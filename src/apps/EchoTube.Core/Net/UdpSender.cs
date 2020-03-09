using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EchoTube.Net
{
    #region Class 'Udp'
    public static partial class Udp
    {
        #region Nested Class 'Sender'
        public class Sender
        {
            #region Class Members
            private UdpClient m_client;

            private readonly IPEndPoint receiveEndPoint = new IPEndPoint(IPAddress.Any, 0);
            #endregion

            #region Constructors
            public Sender()
            {
                InitializeSocket();
            }
            #endregion

            #region Initialization
            private void InitializeSocket()
            {
                m_client = new UdpClient();
            }
            #endregion

            #region Sending
            public void Send(byte[] bytes)
            {
                if (Configuration.Network.UdpProtocol == UdpProtocol.COBS)
                {
                    byte[] rawBytes = COBSEncoding.Encode(bytes);
                    byte[] msg = new byte[rawBytes.Length + 1];

                    Array.Copy(rawBytes, 0, msg, 0, rawBytes.Length);
                    msg[rawBytes.Length] = 0;

                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, Configuration.Network.UdpPort);
                    m_client.Send(msg, msg.Length, endPoint);
                }
                else if (Configuration.Network.UdpProtocol == UdpProtocol.Plain)
                {
                    byte[] controlSequence = new byte[] { 255, 255, 255, 255 };
                    byte[] payloadLength = BitConverter.GetBytes((ushort)bytes.Length);

                    byte[] msg = new byte[4 + 2 + bytes.Length];

                    Array.Copy(controlSequence, 0, msg, 0, controlSequence.Length);
                    Array.Copy(payloadLength, 0, msg, 4, payloadLength.Length);
                    Array.Copy(bytes, 0, msg, 4 + 2, bytes.Length);

                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, Configuration.Network.UdpPort);
                    m_client.Send(msg, msg.Length, endPoint);
                }
            }

            public void Send(List<byte[]> bytes)
            {
                List<byte> final = new List<byte>();

                foreach (byte[] packet in bytes)
                {
                    byte[] rawBytes = COBSEncoding.Encode(packet);
                    byte[] finalPacket = new byte[rawBytes.Length + 1];

                    Array.Copy(rawBytes, 0, finalPacket, 0, rawBytes.Length);
                    finalPacket[rawBytes.Length] = 0;

                    final.AddRange(finalPacket);
                }

                byte[] msg = final.ToArray();

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, Configuration.Network.UdpPort);
                m_client.Send(msg, msg.Length, endPoint);
            }
            #endregion
        }
        #endregion
    }
    #endregion
}
