using ADS_B_HP.Core;
using ADS_B_HP.Mode;
using ADS_B_HP.PlaneInfo;
using ADS_B_HP.Utils;
using DevExpress.Map;
using DevExpress.XtraMap;
using ReceiveDataProcess;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ADS_B_HP.ViewModel
{
    public class FlightMapDataGenerator : IDisposable
    {      
        PlaneInfo.PlaneInfo selectedPlane;
       // DateTime lastTime;
        bool isDisposed;
        List<PlaneInfo.PlaneInfo> planes = new List<PlaneInfo.PlaneInfo>();
        List<InfoBase> airPaths = new List<InfoBase>();
        LockableCollection<ADS_info> adsList = new LockableCollection<ADS_info>();
        readonly Image sourcePlaneIcon;
        readonly PlaneInfoVisualizer visualizer;


        public List<PlaneInfo.PlaneInfo> Planes { get { return planes; } set { planes = value; } }
        public List<InfoBase> AirPaths { get { return airPaths; } set { airPaths = value; } }
        public LockableCollection<ADS_info> AdsList { get => adsList; set => adsList = value; }
        public PlaneInfo.PlaneInfo SelectedPlane
        {
            get { return selectedPlane; }
            set
            {
                if (selectedPlane == value)
                    return;
                selectedPlane = value;
                UpdatePlaneInfo(selectedPlane);
            }
        }

        public FlightMapDataGenerator(MapElementsOverlayManager overlayManager)
        {
            visualizer = new PlaneInfoVisualizer(overlayManager);
            sourcePlaneIcon = new Bitmap("Images\\Plane.png");          
        }

        public void DealAdsbInfo(ClassificationResult results)
        {
            Deal(results);
        }

        void Deal(ClassificationResult rs)
        {
            if (adsList.Count >= 200)
                adsList.Clear();
            string iCAO = rs.info.ICAO == 0 ? string.Empty : rs.info.ICAO.ToString("X2");
            var matchResult = adsList.FirstOrDefault(t => t.ICAO == iCAO);
            if (matchResult != null)
            {
                //静态信息
                if (!rs.PlaneProperty.IsNullorEmpty())
                {
                    if (matchResult.PlaneProperty.IsNullorEmpty())
                        matchResult.PlaneProperty = rs.PlaneProperty;
                }

                if (!rs.Country.IsNullorEmpty())
                {
                    if (matchResult.Country.IsNullorEmpty())
                        matchResult.Country = rs.Country;
                }

                if (!rs.FightNumber.IsNullorEmpty())
                {
                    if (matchResult.FightNumber.IsNullorEmpty())
                        matchResult.FightNumber = rs.FightNumber;
                }

                if (!rs.TailNumber.IsNullorEmpty())
                {
                    if (matchResult.TailNumber.IsNullorEmpty())
                        matchResult.TailNumber = rs.TailNumber;
                }

                //动态信息
                if (!rs.info.longitude.IsNullorEmpty())
                    matchResult.Longitude = rs.info.longitude;
                if (!rs.info.latitude.IsNullorEmpty())
                    matchResult.Latitude = rs.info.latitude;
                if (rs.info.Height != 0)
                {
                    if (matchResult.Height < rs.info.Height)
                        matchResult.Raise = 0;
                    else if (matchResult.Height > rs.info.Height)
                        matchResult.Raise = 1;
                    else
                        matchResult.Raise = 2;
                    matchResult.Height = rs.info.Height;
                    matchResult.Tendency.Add(rs.info.Height);
                }
                if (rs.info.AirSpeed != 0)
                    matchResult.AirSpeed = Convert.ToDouble(rs.info.AirSpeed.ToString("F2")); ;
                if (rs.info.AirDirection != 0)
                    matchResult.AirDirection = rs.info.AirDirection;

                //其他信息
                matchResult.TimeMark = rs.TimeMark;
                matchResult.MsgCount = rs.MsgCount;
                matchResult.CrcMsg = rs.CrcInfo;
                matchResult.MsgTypeCode = rs.MsgTypeCode;
                if (rs.DemoData != null)
                    matchResult.ModeData = BitConverter.ToString(rs.DemoData);
                matchResult.CrcMsgCount[GetCrcType(matchResult.CrcMsg)]++;

                if (!rs.info.latitude.IsNullorEmpty() && !rs.info.longitude.IsNullorEmpty())
                {
                    double longtitude = double.Parse(rs.info.longitude);
                    double latitude = double.Parse(rs.info.latitude);
                    matchResult.Points.Add(new GeoPoint(latitude, longtitude));
                }
                DealPlaneAndAirPath(matchResult);
            }
            else
            {
                ADS_info info = new ADS_info();
                info.TimeMark = rs.TimeMark;
                info.ICAO = rs.info.ICAO.ToString("X2");
                info.AirDirection = rs.info.AirDirection;
                info.AirSpeed = Convert.ToDouble(rs.info.AirSpeed.ToString("F2"));
                info.Country = rs.Country;
                info.TailNumber = rs.TailNumber;
                info.FightNumber = rs.FightNumber;
                info.PlaneProperty = rs.PlaneProperty;
                info.ModeData = BitConverter.ToString(rs.DemoData);
                info.Height = rs.info.Height;
                info.Longitude = rs.info.longitude;
                info.Latitude = rs.info.latitude;
                info.CrcMsg = rs.CrcInfo;
                info.MsgCount = rs.MsgCount;
                info.Raise = 2;
                info.CrcMsgCount[GetCrcType(info.CrcMsg)]++;
                info.MsgTypeCode = rs.MsgTypeCode;
                info.Tendency.Add(rs.info.Height);
                if (!rs.info.latitude.IsNullorEmpty() && !rs.info.longitude.IsNullorEmpty())
                {
                    double longtitude = double.Parse(rs.info.longitude);
                    double latitude = double.Parse(rs.info.latitude);
                    info.Points.Add(new GeoPoint(latitude, longtitude));
                }
                adsList.Add(info);
                DealPlaneAndAirPath(info);
            }
        }

        int GetCrcType(CrcMsg msg)
        {
            int crcType = 1;
            switch (msg)
            {
                case CrcMsg.CrcSucess:
                    crcType = 0;
                    break;
                case CrcMsg.CrcFailed:
                    crcType = 1;
                    break;
                case CrcMsg.CrcCorrect:
                    crcType = 2;
                    break;
                default:
                    break;
            }
            return crcType;
        }

        void DealPlaneAndAirPath(ADS_info aDS_Info)
        {
            double latitude = string.IsNullOrEmpty(aDS_Info.Latitude) ? 0 : double.Parse(aDS_Info.Latitude);
            double longtitude = string.IsNullOrEmpty(aDS_Info.Longitude) ? 0 : double.Parse(aDS_Info.Longitude);
            var plane = planes.FirstOrDefault(t => t.PlaneID == aDS_Info.ICAO);
            if (plane == null)
            {
                PlaneInfo.PlaneInfo info = new PlaneInfo.PlaneInfo(aDS_Info.FightNumber, aDS_Info.ICAO, aDS_Info.AirSpeed, aDS_Info.AirDirection, aDS_Info.Height, aDS_Info.Points, sourcePlaneIcon);
                info.CurrentFlightTime = aDS_Info.TimeMark;
                info.Latitude = latitude;
                info.Longitude = longtitude;
                info.Trajectory.Latitude = latitude;
                info.Trajectory.Longitude = longtitude;
                info.Trajectory.PlaneID = aDS_Info.ICAO;
                info.Attribute = aDS_Info.PlaneProperty;
                planes.Add(info);
                airPaths.Add(info.Trajectory);
                airPaths.Add(info.Trajectory.StartPoint);
                airPaths.Add(info.Trajectory.EndPoint);
                info.Trajectory.StartPoint.PlaneID = aDS_Info.ICAO;
                info.Trajectory.EndPoint.PlaneID = aDS_Info.ICAO;
                if (latitude != 0 && longtitude != 0)
                    RaiseDataChanged();
            }
            else
            {
                plane.SpeedKmH = aDS_Info.AirSpeed;
                plane.FlightAltitude = aDS_Info.Height;
                plane.Course = aDS_Info.AirDirection;
                plane.CurrentFlightTime = aDS_Info.TimeMark;
                plane.Attribute = aDS_Info.PlaneProperty;
                plane.Name = aDS_Info.FightNumber;

                if (plane.Longitude != longtitude && plane.Latitude != latitude)
                {
                    plane.Longitude = longtitude;
                    plane.Latitude = latitude;
                    plane.Trajectory = new PlaneTrajectory(aDS_Info.Points, aDS_Info.AirSpeed);
                    plane.Trajectory.Longitude = longtitude;
                    plane.Trajectory.Latitude = latitude;
                    airPaths.Add(plane.Trajectory);
                    airPaths.Add(plane.Trajectory.StartPoint);
                    airPaths.Add(plane.Trajectory.EndPoint);
                    RaiseDataChanged();
                }
                plane.Trajectory.PlaneID = aDS_Info.ICAO;
                plane.Trajectory.StartPoint.PlaneID = aDS_Info.ICAO;
                plane.Trajectory.EndPoint.PlaneID = aDS_Info.ICAO;
            }
        }

        ~FlightMapDataGenerator()
        {
            Dispose(false);
        }

        public event EventHandler DataChanged;

        void RaiseDataChanged()
        {
            visualizer.Update();
            if (DataChanged != null) DataChanged(this, EventArgs.Empty);
        }
      
        void UpdatePlaneInfo(PlaneInfo.PlaneInfo planeInfo)
        {
            visualizer.SelectedPlane = planeInfo;
        }
    
        void Dispose(bool disposing)
        {
            if (disposing && !isDisposed)
            {
                isDisposed = true;               
                if (sourcePlaneIcon != null)
                    sourcePlaneIcon.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }     
        public List<InfoBase> FindAirPath(PlaneInfo.PlaneInfo plane)
        {
            return airPaths.FindAll(airPath => plane.Trajectory == airPath || plane.Trajectory.StartPoint == airPath || plane.Trajectory.EndPoint == airPath);
        }
    }

    public class PlaneInfoVisualizer
    {
        MapElementsOverlayManager overlayManager;
        PlaneInfo.PlaneInfo selectedPlane;


        public PlaneInfo.PlaneInfo SelectedPlane
        {
            get { return selectedPlane; }
            set
            {
                if (selectedPlane == value)
                    return;
                selectedPlane = value;
                Update();
            }
        }

        public PlaneInfoVisualizer(MapElementsOverlayManager overlayManager)
        {
            this.overlayManager = overlayManager;
        }

        void UpdateInternal()
        {
            if (selectedPlane == null)
            {
                overlayManager.SetOverlaysVisibility(false);
                return;
            }
            overlayManager.SetOverlaysVisibility(true);
            overlayManager.SetTextToItemByKey("name", string.IsNullOrEmpty(selectedPlane.Name) ? "无" : selectedPlane.Name);
            overlayManager.SetTextToItemByKey("id", selectedPlane.PlaneID);
            //overlayManager.SetTextToItemByKey("from", selectedPlane.StartPointName);
            //overlayManager.SetTextToItemByKey("to", selectedPlane.EndPointName);
            overlayManager.SetTextToItemByKey("current_time", selectedPlane.CurrentFlightTime);
            //overlayManager.SetTextToItemByKey("flight_time", new TimeSpan(0, 0, (int)Math.Ceiling(selectedPlane.TotalFlightTime * 3600)).ToString());
            overlayManager.SetTextToItemByKey("speed", selectedPlane.SpeedKmH.ToString("0.00"));
            overlayManager.SetTextToItemByKey("direction", selectedPlane.Course.ToString("0.00"));
            overlayManager.SetTextToItemByKey("altitude", selectedPlane.FlightAltitude.ToString("0.00"));
            overlayManager.SetTextToItemByKey("attribute", string.IsNullOrEmpty(selectedPlane.Attribute) ? string.Empty : selectedPlane.Attribute);
            overlayManager.SetImage(selectedPlane.Image);
        }

        public void Update()
        {
            try
            {
                overlayManager.Map.SuspendRender();
                UpdateInternal();
                overlayManager.Map.ResumeRender();
            }
            catch (Exception)
            {

            }

        }
    }
    public class FlightMapFactory : DefaultMapItemFactory
    {
        public FlightMapDataGenerator DataGenerator { set; get; }
        protected override void InitializeItem(MapItem item, object obj)
        {
            base.InitializeItem(item, obj);
            item.Visible = obj is PlaneInfo.PlaneInfo;

            MapPolyline polyLine = item as MapPolyline;
            PlaneTrajectory trajectory = obj as PlaneTrajectory;
            if (polyLine != null && trajectory != null)
            {
                polyLine.IsGeodesic = true;
                polyLine.Points = trajectory.GetAirPath();
                polyLine.Fill = Color.Empty;
                polyLine.Stroke = Color.FromArgb(127, 255, 0, 199);
                polyLine.StrokeWidth = 4;
                //trajectory.UpdateTrajectory(polyLine.ActualPoints.ToList());
                //if (DataGenerator.SelectedPlane != null)
                //{
                //    PlaneInfo.PlaneInfo plane = DataGenerator.SelectedPlane;
                //}
            }
            MapCustomElement customElement = item as MapCustomElement;
            if (customElement != null)
            {
                customElement.UseAnimation = false;
                customElement.BackgroundDrawingMode = ElementState.None;
            }
           
           
        }


    }
}
