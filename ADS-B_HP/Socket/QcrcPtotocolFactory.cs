using Mina.Core.Session;
using Mina.Filter.Codec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinaFilterTest
{
    public class QcrcPtotocolFactory : IProtocolCodecFactory
    {
        private readonly QcrcDecode decode;
        private readonly QcrcEncode encode;

        public QcrcPtotocolFactory(Encoding encoding)
        {
            decode = new QcrcDecode(encoding);
            encode = new QcrcEncode(encoding);
        }
        public IProtocolDecoder GetDecoder(IoSession session)
        {
            return decode;
        }

        public IProtocolEncoder GetEncoder(IoSession session)
        {
            return encode;
        }
    }
}
