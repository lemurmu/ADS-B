using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADS_B_HP.Mode
{
    public class IFFMessage
    {
        public int PlaneMsgCount { set; get; }
        public int EarthLocationCount { set; get; }
        public int AirLocationCount { set; get; }
        public int AirSpeedCount { set; get; }
        public int PauseAirConditionCount { set; get; }
        public int TargetStateCount { set; get; }
        public int AirConditionCount { set; get; }
        public int OtherMsg { set; get; }
    }

    //public class CrcMsg
    //{
    //    public int CrcSucess { set; get; }//0
    //    public int CrcFailed { set; get; }//-1
    //    public int CrcCorrect { set; get; }//1
    //}
}
