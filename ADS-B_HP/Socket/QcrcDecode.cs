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
    public class QcrcDecode : CumulativeProtocolDecoder
    {
        private static readonly string KEY = "context";
        private Encoding encoding;
        private int maxPackLength = 2048;

        public int MaxPackLength
        {
            get => maxPackLength; set
            {
                if (value <= 0)
                    throw new Exception("value is wrong");
                else
                    maxPackLength = value;
            }
        }

        public QcrcDecode(Encoding encoding)
        {
            this.encoding = encoding;
        }
        public QcrcDecode() : this(Encoding.Default)
        {

        }

        private QcrcContext GetContext(IoSession session)
        {
            QcrcContext ctx = (QcrcContext)session.GetAttribute(KEY);
            if (ctx == null)
            {
                ctx = new QcrcContext();
                session.SetAttribute(KEY, ctx);
            }
            return ctx;

        }

        public override void Dispose(IoSession session)
        {
            QcrcContext ctx = (QcrcContext)session.GetAttribute(KEY);
            if (ctx != null)
            {
                session.RemoveAttribute(KEY);
            }
            base.Dispose(session);
        }

        /// <summary>
        /// 读取的数据已经够解码了，那就返回true，否则返回false 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns> 返回true CumulativeProtocolDecoder会再次调用decoder，并把剩余的数据发下来（粘包了，把剩下的数据合并到下次）
        /// ，返回false就不处理剩余的（数据不够，丢包了），当有新的数据包发来的时候再拼接调用Decoder解码</returns>
        protected override bool DoDecode(IoSession session, IoBuffer input, IProtocolDecoderOutput output)
        {
            int packHeaderLength = 128;
            //QcrcContext ctx = this.GetContext(session);
            //ctx.Append(input);
            //IoBuffer buffer = ctx.Buffer;
            //buffer.Flip();//写流之前调用 否则可能导致position-limit之间没有数据
            if (!input.HasRemaining)
                return false;
            input.Mark();//position快照 以便后续Reset可以恢复position的位置
            byte[] bytes = new byte[4];
            input.Get(bytes, 0, bytes.Length);
            uint flag = BitConverter.ToUInt32(bytes, 0);
            if (flag != Globle.HEARTBEAT && flag != Globle.SELFCHECK && flag != Globle.UNSUALBEAT)//未找到头部标识
            {
                if (input.Remaining < maxPackLength - 4)
                {
                    input.Reset();
                    return false;
                }
                else
                {//头部已经跳过了4字节的标识 position+4 但是没找到头部标识，数据不做处理，继续移到下4个字节
                    return false;
                }
            }
            else
            {//找到头部标识
                if (input.Remaining < maxPackLength - 4)//数据不够
                {
                    input.Reset();
                    return false;
                }
                else//找到完整的数据包
                {
                    byte[] header = new byte[packHeaderLength - bytes.Length];
                    byte[] body = new byte[maxPackLength - packHeaderLength];
                    input.Get(header, 0, header.Length);
                    input.Get(body, 0, body.Length);
                    QcrcRequestInfo info = new QcrcRequestInfo(flag, header, body);
                    output.Write(info);
                    if (input.Remaining > 0)//如果读取一个完整的内容后还有数据（粘包），则用父类再次调用
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

