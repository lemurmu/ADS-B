using DevExpress.Map;
using DevExpress.XtraMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADS_B_HP.PlaneInfo
{
    /// <summary>
    /// 航迹
    /// </summary>
    public class PlaneTrajectory : InfoBase
    {
        class TrajectoryPart
        {
            readonly GeoPoint startPointField;
            readonly GeoPoint endPointField;
            readonly double flightTimeField;
            readonly double courseField;

            public GeoPoint StartPoint { get { return startPointField; } }
            public GeoPoint EndPoint { get { return endPointField; } }
            public double FlightTime { get { return flightTimeField; } }
            public double Course { get { return courseField; } }

            public TrajectoryPart(ProjectionBase projection, GeoPoint startPoint, GeoPoint endPoint, double speedInKmH)
            {
                this.startPointField = startPoint;
                this.endPointField = endPoint;
                MapSize sizeInKm = projection.GeoToKilometersSize(startPoint, new MapSize(Math.Abs(startPoint.Longitude - endPoint.Longitude), Math.Abs(startPoint.Latitude - endPoint.Latitude)));
                double partlength = Math.Sqrt(sizeInKm.Width * sizeInKm.Width + sizeInKm.Height * sizeInKm.Height);
                flightTimeField = partlength / speedInKmH;
                courseField = Math.Atan2((endPoint.Longitude - startPoint.Longitude), (endPoint.Latitude - startPoint.Latitude)) * 180 / Math.PI;
            }
            public GeoPoint GetPointByCurrentFlightTime(double currentFlightTime)
            {
                if (currentFlightTime > FlightTime)
                    return endPointField;
                double ratio = currentFlightTime / FlightTime;
                return new GeoPoint(startPointField.Latitude + ratio * (endPointField.Latitude - startPointField.Latitude), startPointField.Longitude + ratio * (endPointField.Longitude - startPointField.Longitude));
            }
        }

        readonly SphericalMercatorProjection projection = new SphericalMercatorProjection();
        readonly List<TrajectoryPart> trajectory = new List<TrajectoryPart>();
        readonly AirportInfo startPoint;
        readonly AirportInfo endPoint;
        readonly double speedInKmH;

        protected override MapItemType Type { get { return MapItemType.Polyline; } }
        public AirportInfo StartPoint { get { return startPoint; } }
        public AirportInfo EndPoint { get { return endPoint; } }
        public string PlaneID { set; get; }
        public double FlightTime
        {
            get
            {
                double result = 0.0;
                foreach (TrajectoryPart part in trajectory)
                    result += part.FlightTime;
                return result;
            }
        }

        public PlaneTrajectory(List<CoordPoint> points, double speedInKmH)
        {
            this.speedInKmH = speedInKmH;
            UpdateTrajectory(points);
            startPoint = new AirportInfo((trajectory.Count > 0) ? trajectory[0].StartPoint : new GeoPoint(0, 0));
            endPoint = new AirportInfo((trajectory.Count > 0) ? trajectory[trajectory.Count - 1].EndPoint : new GeoPoint(0, 0));
        }
        public GeoPoint GetPointByCurrentFlightTime(double currentFlightTime)
        {
            double time = 0.0;
            for (int i = 0; i < trajectory.Count - 1; i++)
            {
                if (trajectory[i].FlightTime > currentFlightTime - time)
                    return trajectory[i].GetPointByCurrentFlightTime(currentFlightTime - time);
                time += trajectory[i].FlightTime;
            }
            return trajectory[trajectory.Count - 1].GetPointByCurrentFlightTime(currentFlightTime - time);
        }
        public CoordPointCollection GetAirPath()
        {
            CoordPointCollection result = new CoordPointCollection();
            foreach (TrajectoryPart trajectoryPart in trajectory)
                result.Add(trajectoryPart.StartPoint);
            if (trajectory.Count > 0)
                result.Add(trajectory[trajectory.Count - 1].EndPoint);
            return result;
        }
        public double GetCourseByCurrentFlightTime(double currentFlightTime)
        {
            double time = 0.0;
            for (int i = 0; i < trajectory.Count - 1; i++)
            {
                if (trajectory[i].FlightTime > currentFlightTime - time)
                    return trajectory[i].Course;
                time += trajectory[i].FlightTime;
            }
            return trajectory[trajectory.Count - 1].Course;
        }
        public void UpdateTrajectory(List<CoordPoint> points)
        {
            trajectory.Clear();
            for (int i = 0; i < points.Count - 1; i++)
                trajectory.Add(new TrajectoryPart(projection, (GeoPoint)points[i], (GeoPoint)points[i + 1], speedInKmH));
        }
    }
}
