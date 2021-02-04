using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaFilterTest
{
    public class QcrcResponseInfo
    {
        public QcrcResponseInfo(byte[] buffer)
        {
            this.buffer = buffer;
        }
        byte[] buffer;

        public byte[] Buffer { get => buffer; set => buffer = value; }
    }
}
