using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFFOutPutApp
{
    public static class Logger
    {
        private static NLog.Logger logger;
        static Logger()
        {
            //logger = NLog.LogManager.GetLogger(@"Config\NLog.config");
            logger = NLog.LogManager.GetCurrentClassLogger();
        }

        public static void Debug(string msg)
        {
            logger.Debug(msg);
        }
        public static void Trace(string msg)
        {
            logger.Trace(msg);
        }

        public static void Error(string msg)
        {
            logger.Error(msg);
        }

        public static void Warn(string msg)
        {
            logger.Warn(msg);
        }

        public static void Info(string msg)
        {
            logger.Info(msg);
        }
    }
}
