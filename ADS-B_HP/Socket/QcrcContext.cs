using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaFilterTest
{
    public class QcrcContext
    {
        public QcrcContext()
        {
            buffer = IoBuffer.Allocate(2048);
            buffer.AutoExpand = true;
            encoding = Encoding.Default;
        }
        private IoBuffer buffer;
        private Encoding encoding;
        public IoBuffer Buffer { get => buffer; set => buffer = value; }
        public Encoding Encoding { get => encoding; set => encoding = value; }

        public void Append(IoBuffer buffer)
        {
            Buffer.Put(buffer);
        }
        public void Reset()
        {
            encoding = Encoding.Default;
        }

    }
}
