using DevExpress.Map;
using DevExpress.XtraMap;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADS_B_HP.PlaneInfo
{
    public class AirportInfo : InfoBase
    {
        static Bitmap icon = new Bitmap("Images\\Airport.png");

        protected override MapItemType Type { get { return MapItemType.Custom; } }
        public override Image Icon { get { return icon; } }
        public string PlaneID { set; get; }
        public AirportInfo(GeoPoint location)
        {
            this.Latitude = location.Latitude;
            this.Longitude = location.Longitude;
        }
    }
    public class PlaneInfo : InfoBase
    {
        //bool isLandedField = false;
        string currentFlightTime;
        double course;
        Image icon;
        readonly Image sourceIcon;
        readonly string planeIDField;
        string nameField;
        //readonly string endPointNameField;//航班起点
        //readonly string startPointNameField;//航班终点
        double speedInKmHField;
        double flightAltitudeField;
        readonly Image imageField;
        PlaneTrajectory trajectoryField;
        public string Attribute { set; get; }
        public double Course
        {
            get { return course; }
            set
            {
                if (course == value)
                    return;
                course = value;
                UpdateIcon();
            }
        }
        protected override MapItemType Type { get { return MapItemType.Custom; } }
        public string CurrentFlightTime
        {
            get { return currentFlightTime; }
            set
            {
                //if (currentFlightTime == value)
                //    return;
                currentFlightTime = value;
                //UpdatePosition(currentFlightTime);
            }
        }
        public string PlaneID { get { return planeIDField; } }
        public string Name { get { return nameField; }set { nameField = value; } }
        //public string EndPointName { get { return endPointNameField; } }
        //public string StartPointName { get { return startPointNameField; } }
        public double SpeedKmH { get { return speedInKmHField; } set { speedInKmHField = value; } }
        public double FlightAltitude { get { return flightAltitudeField; } set { flightAltitudeField = value; } }
        public Image Image { get { return imageField; } }
        // public bool IsLanded { get { return isLandedField; } }
        public double TotalFlightTime { get { return trajectoryField.FlightTime; } }
        public override Image Icon { get { return icon; } }
        public PlaneTrajectory Trajectory { get { return trajectoryField; } set { trajectoryField = value; } }

        public PlaneInfo(string name, string id, double speedInKmH,double cource, double flightAltitude, List<CoordPoint> points, Image sourceIcon)
        {
            this.nameField = name;
            this.planeIDField = id;
            this.sourceIcon = sourceIcon;
            this.course = cource;
            //this.endPointNameField = endPointName;
            //this.startPointNameField = startPointName;
            this.speedInKmHField = speedInKmH;
            this.flightAltitudeField = flightAltitude;
            this.imageField = new Bitmap("Images\\AirbusA318.png");
            trajectoryField = new PlaneTrajectory(points, speedInKmH);
          
            //UpdatePosition(CurrentFlightTime);
        }
        //void UpdatePosition(double flightTime)
        //{
        //isLandedField = flightTime >= trajectoryField.FlightTime;
        // GeoPoint point = trajectoryField.GetPointByCurrentFlightTime(flightTime);
        // Latitude = point.Latitude;
        // Longitude = point.Longitude;
        // Course = trajectoryField.GetCourseByCurrentFlightTime(flightTime);
        //}
        void UpdateIcon()
        {
            lock (FrmMain.ImageLocker)
            {
                if (icon != null)
                {
                    icon.Dispose();
                    icon = null;
                }
                icon = GetRotatedImage(sourceIcon, Course);
            }
        }

        public static Image GetRotatedImage(Image inputImage, double angleDegrees)
        {
            Color backgroundColor = Color.Transparent;
            bool upsizeOk = false;
            bool clipOk = true;
            if (angleDegrees == 0f)
                return (Bitmap)inputImage.Clone();

            int oldWidth = ((Bitmap)inputImage).Width;
            int oldHeight = ((Bitmap)inputImage).Height;
            int newWidth = oldWidth;
            int newHeight = oldHeight;
            float scaleFactor = 1f;

            if (upsizeOk || !clipOk)
            {
                double angleRadians = angleDegrees * Math.PI / 180d;
                double cos = Math.Abs(Math.Cos(angleRadians));
                double sin = Math.Abs(Math.Sin(angleRadians));
                newWidth = (int)Math.Round(oldWidth * cos + oldHeight * sin);
                newHeight = (int)Math.Round(oldWidth * sin + oldHeight * cos);
            }
            if (!upsizeOk && !clipOk)
            {
                scaleFactor = Math.Min((float)oldWidth / newWidth, (float)oldHeight / newHeight);
                newWidth = oldWidth;
                newHeight = oldHeight;
            }
            Bitmap newBitmap = new Bitmap(newWidth, newHeight, backgroundColor == Color.Transparent ?
                                             PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
            newBitmap.SetResolution(inputImage.HorizontalResolution, inputImage.VerticalResolution);
            using (Graphics graphicsObject = Graphics.FromImage(newBitmap))
            {
                graphicsObject.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsObject.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphicsObject.SmoothingMode = SmoothingMode.HighQuality;
                if (backgroundColor != Color.Transparent)
                    graphicsObject.Clear(backgroundColor);
                graphicsObject.TranslateTransform(newWidth / 2f, newHeight / 2f);
                if (scaleFactor != 1f)
                    graphicsObject.ScaleTransform(scaleFactor, scaleFactor);
                graphicsObject.RotateTransform((float)angleDegrees);
                graphicsObject.TranslateTransform(-oldWidth / 2f, -oldHeight / 2f);
                graphicsObject.DrawImage(inputImage, 0, 0);
            }
            return newBitmap;
        }
    }
}
