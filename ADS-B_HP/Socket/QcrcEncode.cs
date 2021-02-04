using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Filter.Codec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaFilterTest
{
    public class QcrcEncode : ProtocolEncoderAdapter
    {
        private Encoding encoding;
        public QcrcEncode(Encoding encoding)
        {
            this.encoding = encoding;
        }
        public override void Encode(IoSession session, object message, IProtocolEncoderOutput output)
        {
            QcrcResponseInfo info = message as QcrcResponseInfo;
            IoBuffer buffer = IoBuffer.Allocate(info.Buffer.Length);
            buffer.Put(info.Buffer);
            buffer.Flip();
            output.Write(buffer);
        }
    }
}
