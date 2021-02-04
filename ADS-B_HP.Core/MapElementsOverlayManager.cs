using DevExpress.Utils;
using DevExpress.XtraMap;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ADS_B_HP.Core
{
    public class MapElementsOverlayManager : OverlayManagerBase
    {
        readonly MapControl mapControl;
        MapOverlay imageOverlay;
        MapOverlay infoOverlay;
        MapOverlayImageItem imageItem;
        List<string> keys = new List<string>() { "name", "id", "current_time", "speed", "direction", "altitude", "attribute" };
        Dictionary<string, bool> spacingMask;
        Dictionary<string, string> itemsNames;
        Dictionary<string, MapOverlayTextItem> textItems;

        public MapControl Map { get { return mapControl; } }

        public MapElementsOverlayManager(MapControl mapControl)
        {
            textItems = new Dictionary<string, MapOverlayTextItem>();
            this.mapControl = mapControl;
            itemsNames = CreateNames();
            spacingMask = CreateSpacingMask();
            CreateOverlays();
            SetOverlaysVisibility(false);
        }

        Dictionary<string, string> CreateNames()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add(keys[0], "航班号");
            result.Add(keys[1], "ICAO号");
            result.Add(keys[2], "时标");
            result.Add(keys[3], "航速(节)");
            result.Add(keys[4], "航向(°)");
            result.Add(keys[5], "飞行高度");
            result.Add(keys[6], "飞机属性");
            return result;
        }
        Dictionary<string, bool> CreateSpacingMask()
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>();
            result.Add(keys[0], false);
            result.Add(keys[1], false);
            result.Add(keys[2], false);
            result.Add(keys[3], false);
            result.Add(keys[4], false);
            result.Add(keys[5], false);
            result.Add(keys[6], false);
            return result;
        }
        void CreateImageOverlay()
        {
            imageOverlay = new MapOverlay() { Alignment = ContentAlignment.TopRight, Margin = new Padding(0, 10, 10, 0), Padding = new Padding(10) };
            imageItem = new MapOverlayImageItem();
            imageOverlay.Items.Add(imageItem);
        }
        void CreateInfoOverlay()
        {
            infoOverlay = new MapOverlay() { Alignment = ContentAlignment.TopRight, JoiningOrientation = Orientation.Vertical, Margin = new Padding(0, 0, 10, 0), Padding = new Padding(10) };
            textItems.Clear();
            foreach (string key in keys)
            {
                int bottomPadding = spacingMask[key] ? 13 : 3;
                string itemText = string.Format("{0}:", itemsNames[key]);
                MapOverlayTextItem labelItem = new MapOverlayTextItem() { Alignment = ContentAlignment.TopLeft, JoiningOrientation = Orientation.Vertical, Size = new Size(105, 0), Padding = new Padding(0, 3, 0, bottomPadding), Text = itemText };
                labelItem.TextStyle.Font = FontsCollection["label"];
                MapOverlayTextItem valueItem = new MapOverlayTextItem() { Alignment = ContentAlignment.TopRight, JoiningOrientation = Orientation.Vertical, Size = new Size(120, 0), Padding = new Padding(0, 3, 0, bottomPadding) };
                valueItem.TextStyle.Font = FontsCollection["value"];
                textItems.Add(key, valueItem);
                infoOverlay.Items.AddRange(new MapOverlayItemBase[] { labelItem, valueItem });
            }
        }
        void CreateOverlays()
        {
            CreateImageOverlay();
            CreateInfoOverlay();
        }

        protected override Dictionary<string, Font> CreateFonts()
        {
            Dictionary<string, Font> collection = new Dictionary<string, Font>();
            collection.Add("label", new Font(AppearanceObject.DefaultFont.FontFamily, 8, FontStyle.Regular));
            collection.Add("value", new Font(AppearanceObject.DefaultFont.FontFamily, 8, FontStyle.Bold));
            return collection;
        }

        public MapOverlay[] GetOverlays()
        {
            return new MapOverlay[] { imageOverlay, infoOverlay};
        }
        public void SetTextToItemByKey(string key, string text)
        {
            if (!textItems.ContainsKey(key))
                return;
            textItems[key].Text = text;
        }
        public void SetImage(Image image)
        {
            imageItem.Image = image;
        }
        public void SetOverlaysVisibility(bool isVisible)
        {
            imageOverlay.Visible = isVisible;
            infoOverlay.Visible = isVisible;
        }
    }
}
