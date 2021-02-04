using DevExpress.XtraMap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADS_B_HP.Core
{
    public class LocalProvider : MapDataProviderBase
    {

        readonly SphericalMercatorProjection projection = new SphericalMercatorProjection();

        public LocalProvider(string mapFormat)
        {
            TileSource = new LocalTileSource(this,mapFormat);
        }

        public override ProjectionBase Projection
        {
            get
            {
                return projection;
            }
        }

        public override MapSize GetMapSizeInPixels(double zoomLevel)
        {
            double imageSize;
            imageSize = LocalTileSource.CalculateTotalImageSize(zoomLevel);
            return new MapSize(imageSize, imageSize);
        }
        protected override Size BaseSizeInPixels
        {
            get { return new Size(Convert.ToInt32(LocalTileSource.tileSize * 2), Convert.ToInt32(LocalTileSource.tileSize * 2)); }
        }
    }

    public class LocalTileSource : MapTileSourceBase
    {
        public const int tileSize = 256;
        public const int maxZoomLevel = 12;
        string directoryPath;
        string MapsFormat = "jpg";
        internal static double CalculateTotalImageSize(double zoomLevel)
        {
            if (zoomLevel < 1.0)
                return zoomLevel * tileSize * 2;
            return Math.Pow(2.0, zoomLevel) * tileSize;
        }

        public LocalTileSource(ICacheOptionsProvider cacheOptionsProvider,string mapFormat) :
            base((int)CalculateTotalImageSize(maxZoomLevel), (int)CalculateTotalImageSize(maxZoomLevel), tileSize, tileSize, cacheOptionsProvider)
        {
            this.MapsFormat = mapFormat;
            DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            directoryPath = dir + "\\maps";
        }
        //重写基类的获取图片的方法，一定要对应按下载地图的文件结构
        public override Uri GetTileByZoomLevel(int zoomLevel, int tilePositionX, int tilePositionY)
        {
            if (zoomLevel <= maxZoomLevel)
            {
                Uri u = new Uri(string.Format("file://" + directoryPath + "\\{0}\\{1}\\{2}." + MapsFormat, zoomLevel, tilePositionX, tilePositionY));
                return u;
            }
            return null;
        }
    }
}
