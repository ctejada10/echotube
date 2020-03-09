using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EchoTube.Net;

namespace EchoTube
{
    #region Static Class 'Configuration'
    public static class Configuration
    {
        #region Nested Static Class 'Network'
        public static class Network
        {
            #region Adjustable Members
            public static int Timeout = 1000;

            public static UdpProtocol UdpProtocol = UdpProtocol.Plain;
            #endregion

            #region Fixed Members
            internal const int UdpPort = 1966;
            #endregion
        }
        #endregion
    }
    #endregion
}
