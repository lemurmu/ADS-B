using DevExpress.Map;
using ReceiveDataProcess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADS_B_HP.Mode
{
    public class ADS_info
    {
        public ADS_info()
        {
            Tendency = new List<double>();
            MsgCount = new UInt16[8];
            Points = new List<CoordPoint>();
            CrcMsgCount = new byte[3];
        }
        public string ArriveTime { set; get; }
        public string ModeName { set; get; }
        public long Freq { set; get; }
        public string TimeMark { set; get; }
        public string Recoding { set; get; }
        public string ICAO { set; get; }
        public string TailNumber { set; get; }
        public string Country { set; get; }
        public string FightNumber { set; get; }
        public string PlaneProperty { set; get; }
        public int Height { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public double AirDirection { get; set; }
        public double AirSpeed { get; set; }
        public string ModeData { set; get; }
        public List<double> Tendency { set; get; }
        public CrcMsg CrcMsg { set; get; }
        public byte[] CrcMsgCount { set; get; }
        public UInt16[] MsgCount { set; get; }
        public List<CoordPoint> Points { set; get; }
        public byte Raise { set; get; }//0 up 1 dowmn
        public int MsgTypeCode { set; get; }
    }

    public class Crc
    {
        public string Argument { set; get; }
        public double Crcmsg { set; get; }
    }
}
