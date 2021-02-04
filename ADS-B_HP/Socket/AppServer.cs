using Mina.Core.Service;
using Mina.Filter.Codec;
using Mina.Filter.Logging;
using Mina.Transport.Socket;
using MinaFilterTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ADS_B_HP.Socket
{
    public class AppServer
    {
        public static AppServer DefaultServer = new AppServer();

        private IoAcceptor acceptor;
        public IoAcceptor Acceptor { get => acceptor; set => acceptor = value; }
        public void Start(int port)
        {
            // Create the acceptor
            acceptor = new AsyncSocketAcceptor();

            // Add two filters : a logger and a codec
            acceptor.FilterChain.AddLast("logger", new LoggingFilter());
            acceptor.FilterChain.AddLast("codec", new ProtocolCodecFilter(new QcrcPtotocolFactory(Encoding.UTF8)));

            // Attach the business logic to the server
            acceptor.Handler = new QcrcHandler();

            // Configurate the buffer size and the iddle time
            acceptor.SessionConfig.ReadBufferSize = 2048;
            //acceptor.SessionConfig.SetIdleTime(IdleStatus.BothIdle, 1);

            // And bind !
            acceptor.Bind(new IPEndPoint(IPAddress.Any, port));

        }

        public void Stop()
        {
            acceptor?.Unbind();
        }
    }
}
