using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoTube.Net
{
    #region Enumeration 'UdpProtocol
    [Flags]
    public enum UdpProtocol : int
    {
        COBS = 1,
        Plain = 2
    }
    #endregion

    #region Delegates
    public delegate void ConnectionDataEventHandler(object sender, byte[] bytes);
    #endregion
}
