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
    public static class OverlayUtils
    {
        static MapOverlay bingLogoOverlay;
        static MapOverlay bingCopyrightOverlay;
        static MapOverlay osmCopyrightOverlay;
        static MapOverlay medalsOverlay;

        public static MapOverlay BingLogoOverlay
        {
            get
            {
                if (bingLogoOverlay == null)
                    bingLogoOverlay = CreateBingLogoOverlay();
                return bingLogoOverlay;
            }
        }
        public static MapOverlay BingCopyrightOverlay
        {
            get
            {
                if (bingCopyrightOverlay == null)
                    bingCopyrightOverlay = CreateBingCopyrightOverlay();
                return bingCopyrightOverlay;
            }
        }
        public static MapOverlay OSMCopyrightOverlay
        {
            get
            {
                if (osmCopyrightOverlay == null)
                    osmCopyrightOverlay = CreateOSMCopyrightOverlay();
                return osmCopyrightOverlay;
            }
        }
        public static MapOverlay MedalsOverlay
        {
            get
            {
                if (medalsOverlay == null)
                    medalsOverlay = CreateMedalsOverlay();
                return medalsOverlay;
            }
        }

        static MapOverlay CreateBingLogoOverlay()
        {
            MapOverlay overlay = new MapOverlay() { Alignment = ContentAlignment.BottomLeft, Margin = new Padding(10, 0, 0, 10) };
            MapOverlayImageItem logoItem = new MapOverlayImageItem() { Padding = new Padding(), ImageUri = new Uri("") };
            overlay.Items.Add(logoItem);
            return overlay;
        }
        static MapOverlay CreateBingCopyrightOverlay()
        {
            MapOverlay overlay = new MapOverlay() { Alignment = ContentAlignment.BottomRight, Margin = new Padding(0, 0, 10, 10) };
            MapOverlayTextItem copyrightItem = new MapOverlayTextItem() { Padding = new Padding(5), Text = "Copyright © 2018 Microsoft and its suppliers. All rights reserved." };
            overlay.Items.Add(copyrightItem);
            return overlay;
        }
        static MapOverlay CreateOSMCopyrightOverlay()
        {
            MapOverlay overlay = new MapOverlay() { Alignment = ContentAlignment.BottomRight, Margin = new Padding(0, 0, 10, 10) };
            MapOverlayTextItem copyrightItem = new MapOverlayTextItem() { Padding = new Padding(5), Text = "© OpenStreetMap contributors" };
            overlay.Items.Add(copyrightItem);
            return overlay;
        }
        static MapOverlay CreateMedalsOverlay()
        {
            MapOverlay overlay = new MapOverlay() { Alignment = ContentAlignment.TopCenter, Margin = new Padding(10, 10, 10, 10) };
            MapOverlayTextItem medalsItem = new MapOverlayTextItem() { Padding = new Padding(5), Text = "2016 Summer Olympics Medal Result" };
            medalsItem.TextStyle.Font = new Font(AppearanceObject.DefaultFont.FontFamily, 16, FontStyle.Regular);
            overlay.Items.Add(medalsItem);
            return overlay;
        }
        public static MapOverlay[] GetBingOverlays()
        {
            return new MapOverlay[] { BingLogoOverlay, BingCopyrightOverlay };
        }
        public static MapOverlay[] GetMedalsOverlay()
        {
            return new MapOverlay[] { MedalsOverlay };
        }
        public static MapOverlayItemBase GetClickedOverlayItem(MapHitInfo hitInfo)
        {
            if (hitInfo.InUIElement)
            {
                MapOverlayHitInfo overlayHitInfo = hitInfo.UiHitInfo as MapOverlayHitInfo;
                if (overlayHitInfo != null)
                    return overlayHitInfo.OverlayItem;
            }
            return null;
        }
    }
}
