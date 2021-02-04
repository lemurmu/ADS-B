using ADS_B_HP.Core;
using ADS_B_HP.Mode;
using ADS_B_HP.PlaneInfo;
using ADS_B_HP.Socket;
using ADS_B_HP.Utils;
using ADS_B_HP.ViewModel;
using DevExpress.Customization;
using DevExpress.Skins;
using DevExpress.Sparkline;
using DevExpress.Utils;
using DevExpress.XtraBars;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using DevExpress.XtraMap;
using IFFOutPutApp;
using MinaFilterTest;
using ReceiveDataProcess;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ADS_B_HP
{
    public partial class FrmMain : DevExpress.XtraEditors.XtraForm
    {
        public FrmMain()
        {
            InitializeComponent();
            Init();
        }

        static object imageLocker = new object();
        public static object ImageLocker { get { return imageLocker; } }
        MapElementsOverlayManager overlayManager;
        FlightMapDataGenerator dataGenerator;

        MapElementsOverlayManager OverlayManager
        {
            get
            {
                if (overlayManager == null)
                    overlayManager = new MapElementsOverlayManager(mapControl);
                return overlayManager;
            }
        }
        protected MapOverlay[] Overlays { get { return OverlayManager.GetOverlays(); } }
        protected BingMapKind MiniMapBingKind { get { return BingMapKind.Road; } }
        protected MiniMapAlignment MiniMapAlignment { get { return MiniMapAlignment.TopLeft; } }
        public MapControl MapControl { get { return mapControl; } }

        bool enableStartServer = true;
        private Color red;
        private Color green;
        ImageLayer imageTilesLayer;
        QcrcHandler handler;
        void Init()
        {
            //this.DoubleBuffered = true;
            msg_txt.BackColor = layoutControl1.BackColor;
            GlobalParamInfo.Longitide = 103.660441;
            GlobalParamInfo.Latitude = 31.0327;
            GlobalParamInfo.LoadIcaoMappingConfig();
            this.adsb_gridview.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.adsb_gridview.DetailHeight = 377;
            this.adsb_gridview.GridControl = this.adsb_grid;
            this.adsb_gridview.Name = "adsb_gridview";
            this.adsb_gridview.OptionsBehavior.AllowAddRows = DevExpress.Utils.DefaultBoolean.False;
            this.adsb_gridview.OptionsBehavior.AllowDeleteRows = DevExpress.Utils.DefaultBoolean.False;
            this.adsb_gridview.OptionsBehavior.AllowPixelScrolling = DevExpress.Utils.DefaultBoolean.True;
            this.adsb_gridview.OptionsBehavior.Editable = false;
            this.adsb_gridview.OptionsBehavior.ReadOnly = true;
            this.adsb_gridview.OptionsCustomization.AllowGroup = false;
            this.adsb_gridview.OptionsDetail.EnableMasterViewMode = false;
            this.adsb_gridview.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.adsb_gridview.OptionsView.ShowGroupPanel = false;
            this.adsb_gridview.RowHeight = 32;
            this.adsb_gridview.IndicatorWidth = 40;
            this.adsb_gridview.OptionsView.RowAutoHeight = true;
            this.adsb_gridview.OptionsView.ColumnAutoWidth = true;
            GridColumn col = adsb_gridview.Columns["Height"];
            col.ColumnEdit = new ArrowButtonRepositoryItem();
            col.DisplayFormat.FormatType = FormatType.Numeric;
            col.Visible = true;
            RepositoryItemSparklineEdit rise = new RepositoryItemSparklineEdit();
            this.adsb_gridview.Columns["Tendency"].ColumnEdit = rise;
            this.adsb_gridview.Columns["Tendency"].UnboundType = DevExpress.Data.UnboundColumnType.Object;
            AreaSparklineView view = new AreaSparklineView();
            view.HighlightMaxPoint = true;
            view.HighlightMinPoint = true;
            rise.View = view;

            mapControl.MinZoomLevel = 1;
            mapControl.MaxZoomLevel = 12;
            mapControl.ZoomLevel = 3;
            mapControl.CenterPoint = new GeoPoint(GlobalParamInfo.Latitude, GlobalParamInfo.Longitide);
            mapControl.SelectionMode = DevExpress.XtraMap.ElementSelectionMode.Single;
            mapControl.Overlays.AddRange(Overlays);
            imageTilesLayer = new ImageLayer();
            imageTilesLayer.MaxZoomLevel = 12;
            imageTilesLayer.MinZoomLevel = 1;
            imageTilesLayer.DataProvider = new LocalProvider("jpg");
            mapControl.Layers.Add(imageTilesLayer);

            dataGenerator = new FlightMapDataGenerator(OverlayManager);
            dataGenerator.DataChanged += OnDataChanged;
            PlanesDataAdapter.DataSource = dataGenerator.Planes;
            PathsDataAdapter.DataSource = dataGenerator.AirPaths;
            FlightMapFactory factory = new FlightMapFactory();
            factory.DataGenerator = dataGenerator;
            mapControl.SetMapItemFactory(factory);
           

            this.adsb_gridview.BestFitColumns();
            this.adsb_gridview.OptionsView.ShowIndicator = true;
            this.adsb_grid.DataSource = dataGenerator.AdsList;

            refreshTimer.Interval = 3000;
            XYDiagram diagram = this.real_msgChart.Diagram as XYDiagram;
            diagram.AxisY.WholeRange.SetMinMaxValues(0, 7);
        }

        private void SimpleButton1_Click(object sender, EventArgs e)
        {
            if (enableStartServer)
            {
                int port = (int)ip_txt.Value;
                AppServer.DefaultServer.Start(port);
                handler = AppServer.DefaultServer.Acceptor.Handler as QcrcHandler;
                handler.Action = new Action<ClassificationResult>(ADS_BDataCallBack);
                Logger.Info($"Server start at port:{port}......");
                enableStartServer = false;
                start_btn.Text = "停止服务";
            }
            else
            {
                AppServer.DefaultServer.Stop();
                Logger.Info($"Server stop ......");
                enableStartServer = true;
                start_btn.Text = "启动服务";
            }

        }

        #region Map
        DateTime oldDataTime;
        DateTime currentDateTime;
        void OnDataChanged(object sender, EventArgs e)
        {
            oldDataTime = DateTime.Now;
            if (!refreshTimer.Enabled)
            {
                oldDataTime = DateTime.Now;
                refreshTimer.Enabled = true;
            }
            foreach (PlaneInfo.PlaneInfo info in dataGenerator.Planes)
            {
                MapCustomElement item = PlanesLayer.GetMapItemBySourceObject(info) as MapCustomElement;
                if (item != null) item.Location = new GeoPoint(info.Latitude, info.Longitude);
            }
            //PlanesDataAdapter.Load();
            //PathsDataAdapter.Load();
            //dockPanel4.CustomHeaderButtons[0].Properties.Caption = $"共计{dataGenerator.Planes.Count}个目标";
        }

        private void PathsLayer_DataLoaded(object sender, DataLoadedEventArgs e)
        {
            if (dataGenerator.Planes.Count == 0)
                return;
            if (dataGenerator.SelectedPlane == null)
                dataGenerator.SelectedPlane = dataGenerator.Planes[0];
            OnActivePlaneChanged();
        }

        void OnActivePlaneChanged()
        {
            MapItemCollection items = (MapItemCollection)PathsLayer.Data.Items;
            items.BeginUpdate();
            HideLayerItems(PathsLayer);
            List<InfoBase> airPath = dataGenerator.FindAirPath(dataGenerator.SelectedPlane);
            foreach (InfoBase airPathElement in airPath)
            {
                MapItem item = PathsLayer.GetMapItemBySourceObject(airPathElement);
                if (item != null)
                {
                    //if (!item.Visible)
                    item.Visible = true;
                }

            }
            items.EndUpdate();
        }

        void HideLayerItems(VectorItemsLayer layer)
        {
            foreach (MapItem item in ((IMapDataAdapter)layer.Data).Items)
            {
                InfoBase airPathElement = PathsLayer.GetItemSourceObject(item) as InfoBase;
                if (airPathElement is PlaneTrajectory)
                {
                    PlaneTrajectory planeTrajectory = airPathElement as PlaneTrajectory;
                    if (planeTrajectory.PlaneID != dataGenerator.SelectedPlane.PlaneID)
                        item.Visible = false;
                    else
                        item.Visible = true;
                }
                else if (airPathElement is AirportInfo)
                {
                    item.Visible = false;
                    //AirportInfo airportInfo = airPathElement as AirportInfo;
                    //if (airportInfo.PlaneID != dataGenerator.SelectedPlane.PlaneID)
                    //    item.Visible = false;
                    //else
                    //    item.Visible = true;
                }
                //item.Visible = false;
            }

        }

        private void MapControl_DrawMapItem(object sender, DrawMapItemEventArgs e)
        {
            DrawMapPointerEventArgs args = e as DrawMapPointerEventArgs;
            if (args != null)
            {
                args.DisposeImage = true;
                MapItem item = e.Item;
                InfoBase info = ((VectorItemsLayer)e.Item.Layer).GetItemSourceObject(item) as InfoBase;
                if (info != null)
                {
                    lock (ImageLocker)
                    {
                        args.Image = info.Icon != null ? (Image)info.Icon.Clone() : null;
                    }
                }
            }
        }

        private void MapControl_SelectionChanged(object sender, MapSelectionChangedEventArgs e)
        {
            var plane = e.Selection.Count > 0 ? (PlaneInfo.PlaneInfo)e.Selection[0] : null;
            if (plane == null)
            {
                return;
            }
            dataGenerator.SelectedPlane = plane;
            OnActivePlaneChanged();
        }

        private void MapControl_SelectionChanging(object sender, MapSelectionChangingEventArgs e)
        {
            PlaneInfo.PlaneInfo plainInfo = e.Selection.Count > 0 ? e.Selection[0] as PlaneInfo.PlaneInfo : null;
            e.Cancel = plainInfo == null;
        }
        #endregion

        private void ADS_BDataCallBack(ClassificationResult results)
        {
            if (adsb_grid.InvokeRequired)
            {
                adsb_grid.BeginInvoke(new Action<ClassificationResult>(ADS_BDataCallBack), results);
            }
            else
            {
                dataGenerator.DealAdsbInfo(results);
                adsb_gridview.RefreshData();
                RefreshFocusRowDataToChart();
            }

        }

        void RefreshFocusRowDataToChart()
        {
            int rowHandle = adsb_gridview.FocusedRowHandle;
            ADS_info info = adsb_gridview.GetRow(rowHandle) as ADS_info;
            if (info != null)
            {
                AddCrcData(info);
                AddMsgCount(info);
                AddRealTimeMsgData(info);
            }
        }

        private void Logs(string msg)
        {
            if (msg_txt.InvokeRequired)
            {
                msg_txt.BeginInvoke(new Action<string>(Logs), msg);
            }
            else
            {
                msg_txt.AppendText(msg);
            }
        }

        void PanelsCheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            BarCheckItem item = e.Item as BarCheckItem;
            if (item == null) return;
            SetDockPanelVisibility(item);
        }
        void SetDockPanelVisibility(BarCheckItem item)
        {
            string tag = item.Tag as string;
            switch (tag)
            {
                case "crc":
                    if (!item.Checked)
                        dockPanel3.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Hidden;
                    else
                        dockPanel3.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible;
                    break;
                case "msgCount":
                    if (!item.Checked)
                        dockPanel1.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Hidden;
                    else
                        dockPanel1.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible;
                    break;
                case "msgRate":
                    if (!item.Checked)
                        dockPanel2.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Hidden;
                    else
                        dockPanel2.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible;
                    break;
                case "rail":
                    if (!item.Checked)
                        dockPanel4.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Hidden;
                    else
                        dockPanel4.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible;
                    break;
                case "realMsg":
                    if (!item.Checked)
                        dockPanel5.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Hidden;
                    else
                        dockPanel5.Visibility = DevExpress.XtraBars.Docking.DockVisibility.Visible;
                    break;
            }
        }

        private void Swatches_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (SvgSkinPaletteSelector svgSkinPaletteSelector = new SvgSkinPaletteSelector(this.FindForm()))
            {
                svgSkinPaletteSelector.ShowDialog();
            }
            UpdateCustomColors();
        }

        void UpdateCustomColors()
        {
            red = CommonColors.GetCriticalColor(DevExpress.LookAndFeel.UserLookAndFeel.Default);
            green = CommonColors.GetInformationColor(DevExpress.LookAndFeel.UserLookAndFeel.Default);
            msg_txt.BackColor = layoutControl1.BackColor;
        }

        private void FrmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            AppServer.DefaultServer.Stop();
        }

        private void Adsb_gridview_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            try
            {
                int rowHandle = e.FocusedRowHandle;
                ADS_info info = adsb_gridview.GetRow(rowHandle) as ADS_info;
                if (info != null)
                {
                    AddCrcData(info);
                    AddMsgCount(info);
                    AddRealTimeMsgData(info);
                    var selectPlane = dataGenerator.Planes.FirstOrDefault(p => p.PlaneID == info.ICAO);
                    if (selectPlane != null)
                    {
                        dataGenerator.SelectedPlane = selectPlane;
                        OnActivePlaneChanged();
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }

        }

        void AddCrcData(ADS_info info)
        {
            if (crcChart.InvokeRequired)
            {
                crcChart.BeginInvoke(new Action<ADS_info>(AddCrcData), info);
            }
            else
            {
                crcChart.Series[0].Points[0].Values[0] = info.CrcMsgCount[0];
                crcChart.Series[0].Points[1].Values[0] = info.CrcMsgCount[1];
                crcChart.Series[0].Points[2].Values[0] = info.CrcMsgCount[2];
                crcChart.RefreshData();
            }

        }

        void AddMsgCount(ADS_info info)
        {
            if (msgCountChart.InvokeRequired)
            {
                msgCountChart.BeginInvoke(new Action<ADS_info>(AddMsgCount), info);
            }
            else
            {
                msgCountChart.Series[0].Points[0].Values[0] = info.MsgCount[1];
                msgCountChart.Series[0].Points[1].Values[0] = info.MsgCount[2];
                msgCountChart.Series[0].Points[2].Values[0] = info.MsgCount[3];
                msgCountChart.Series[0].Points[3].Values[0] = info.MsgCount[4];
                msgCountChart.Series[0].Points[4].Values[0] = info.MsgCount[5];
                msgCountChart.Series[0].Points[5].Values[0] = info.MsgCount[6];
                msgCountChart.Series[0].Points[6].Values[0] = info.MsgCount[7];
                msgCountChart.Series[0].Points[7].Values[0] = info.MsgCount[0];
                msgCountChart.RefreshData();

                int sum = info.MsgCount.Sum(t => t);
                msgCountPie.Series[0].Points[0].Values[0] = double.Parse((info.MsgCount[1] * 1.0 / sum).ToString("0.00")) * 100;
                msgCountPie.Series[0].Points[1].Values[0] = double.Parse((info.MsgCount[2] * 1.0 / sum).ToString("0.00")) * 100;
                msgCountPie.Series[0].Points[2].Values[0] = double.Parse((info.MsgCount[3] * 1.0 / sum).ToString("0.00")) * 100;
                msgCountPie.Series[0].Points[3].Values[0] = double.Parse((info.MsgCount[4] * 1.0 / sum).ToString("0.00")) * 100;
                msgCountPie.Series[0].Points[4].Values[0] = double.Parse((info.MsgCount[5] * 1.0 / sum).ToString("0.00")) * 100;
                msgCountPie.Series[0].Points[5].Values[0] = double.Parse((info.MsgCount[6] * 1.0 / sum).ToString("0.00")) * 100;
                msgCountPie.Series[0].Points[6].Values[0] = double.Parse((info.MsgCount[7] * 1.0 / sum).ToString("0.00")) * 100;
                msgCountPie.Series[0].Points[7].Values[0] = double.Parse((info.MsgCount[0] * 1.0 / sum).ToString("0.00")) * 100;
                msgCountPie.RefreshData();
            }

        }

        void AddRealTimeMsgData(ADS_info info)
        {
            if (real_msgChart.InvokeRequired)
            {
                real_msgChart.BeginInvoke(new Action<ADS_info>(AddRealTimeMsgData), info);
            }
            else
            {
                int year = int.Parse(info.TimeMark.Substring(0, 4));
                int month = int.Parse(info.TimeMark.Substring(4, 2));
                int day = int.Parse(info.TimeMark.Substring(6, 2));
                int hour = int.Parse(info.TimeMark.Substring(8, 2));
                int minute = int.Parse(info.TimeMark.Substring(10, 2));
                int second = int.Parse(info.TimeMark.Substring(12, 2));
                DateTime dt = new DateTime(year, month, day, hour, minute, second);
                real_msgChart.Series[0].Points.Add(new DevExpress.XtraCharts.SeriesPoint(dt, info.MsgTypeCode));
            }

        }

        private void Adsb_gridview_CustomDrawRowIndicator(object sender, DevExpress.XtraGrid.Views.Grid.RowIndicatorCustomDrawEventArgs e)
        {
            if (e.Info.IsRowIndicator && e.RowHandle >= 0)
            {
                e.Info.DisplayText = (e.RowHandle + 1).ToString();
                adsb_gridview.InvalidateRowIndicator(e.RowHandle + 1);
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            currentDateTime = DateTime.Now;
            if (currentDateTime.Subtract(oldDataTime).Duration().Seconds >= 5)//超时判断
            {
                refreshTimer.Enabled = false;
                return;
            }
            PlanesDataAdapter.Load();
            PathsDataAdapter.Load();
            dockPanel4.CustomHeaderButtons[1].Properties.Caption = $"共计{dataGenerator.Planes.Count}个目标";
        }

        private void Adsb_gridview_CustomDrawCell(object sender, DevExpress.XtraGrid.Views.Base.RowCellCustomDrawEventArgs e)
        {
            if (dataGenerator == null) return;
            GridCellInfo info = e.Cell as GridCellInfo;

            if (info == null) return;
            ADS_info rowData = adsb_gridview.GetRow(e.RowHandle) as ADS_info;
            ArrowButtonRepositoryItem arrowb = info.Editor as ArrowButtonRepositoryItem;
            if (info.Column.FieldName == "Height" && arrowb != null)
            {
                //int contextImageSize = e.Bounds.Height - 6;
                //arrowb.ContextImageOptions.SvgImageSize = new Size(contextImageSize, contextImageSize);
                switch (rowData.Raise)
                {
                    case 0:
                        arrowb.ContextImageOptions.SvgImage = CommonUtil.LoadSvgImageFromFile("Images\\Up.svg");
                        break;
                    case 1:
                        arrowb.ContextImageOptions.SvgImage = CommonUtil.LoadSvgImageFromFile("Images\\Down.svg");
                        break;
                    default:
                        arrowb.ContextImageOptions.SvgImage = null;
                        break;
                }
                info.ViewInfo.DetailLevel = DevExpress.XtraEditors.Controls.DetailLevel.Full;
                info.ViewInfo.CalcViewInfo();
            }
        }

        private void Adsb_gridview_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.IsGetData)
            {
                ADS_info model = e.Row as ADS_info;
                if (model != null && model.Tendency != null)
                {
                    e.Value = model.Tendency;
                }
            }
        }

        private void BarButtonItem1_ItemClick(object sender, ItemClickEventArgs e)
        {
            dataGenerator.Planes.Clear();
            dataGenerator.AirPaths.Clear();
            dataGenerator.AdsList.Clear();
            adsb_gridview.RefreshData();
            PlanesDataAdapter.Load();
            PathsDataAdapter.Load();

        }

        private void Adsb_grid_MouseDown(object sender, MouseEventArgs e)
        {
            GridControl control = sender as GridControl;
            if (e.Button == MouseButtons.Right)
            {
                clear_Popule.ShowPopup(MousePosition, control);
            }
        }

        private void DockPanel4_CustomButtonClick(object sender, DevExpress.XtraBars.Docking2010.ButtonEventArgs e)
        {
            foreach (MapItem item in ((IMapDataAdapter)PathsLayer.Data).Items)
            {
                item.Visible = true;
            }
        }
    }
}
