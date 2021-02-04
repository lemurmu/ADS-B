using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaFilterTest
{
    public class QcrcRequestInfo
    {
        public QcrcRequestInfo(uint key, byte[] header, byte[] body)
        {
            this.key = key;
            this.header = header;
            this.body = body;
        }

        uint key;
        public uint Key { get { return key; } }

        byte[] header;
        public byte[] Header { get { return header; } }

        byte[] body;
        public byte[] Body { get { return body; } }

    }
}
