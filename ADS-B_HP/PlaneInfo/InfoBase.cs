using DevExpress.XtraMap;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADS_B_HP.PlaneInfo
{
    public abstract class InfoBase
    {
        protected abstract MapItemType Type { get; }
        public int ItemType { get { return (int)Type; } }
        public virtual Image Icon { get { return null; } }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
