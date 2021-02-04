using DevExpress.Utils.Svg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADS_B_HP.Utils
{
    public class CommonUtil
    {
        static public SvgImage LoadSvgImageFromResource(string imageName, object obj)
        {
            return SvgImage.FromResources(string.Format(@"ADS-B_HP.SvgImage.{0}.svg", imageName), obj.GetType().Assembly);
        }

        public static SvgImage LoadSvgImageFromFile(string path)
        {
            return SvgImage.FromFile(path);
        }
    }
}
