using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoTube.Net
{
    #region Class 'Udp'
    public static partial class Udp
    {
        #region Nested Class 'Receiver'
        public class Receiver
        {
            #region Class Members
            private UdpClient m_client;

            private IPEndPoint m_receiveEndPoint = new IPEndPoint(IPAddress.Any, 0);

            private Thread m_receiveThread;

            private List<byte> m_buffer;

            private bool m_bufferSynchronized = false;
            #endregion

            #region Events
            public event ConnectionDataEventHandler BytesReceived;

            public event ConnectionDataEventHandler PacketReceived;
            #endregion

            #region Constructors
            public Receiver()
            {
                m_buffer = new List<byte>();
                InitializeSocket();
            }
            #endregion

            #region Initialization
            private void InitializeSocket()
            {
                m_client = new UdpClient(Configuration.Network.UdpPort);
            }
            #endregion

            #region Properties
            public bool IsRunning { get; internal set; } = false;
            #endregion

            #region Start/Stop
            public void Start()
            {
                if (!IsRunning)
                {
                    m_bufferSynchronized = false;

                    IsRunning = true;
                    m_receiveThread = new Thread(new ThreadStart(Receive))
                    {
                        IsBackground = true,
                        Name = "UdpConnection#Receive"
                    };
                    m_receiveThread.Start();
                }
            }

            public void Stop()
            {
                if (IsRunning)
                {
                    IsRunning = false;

                    if (!m_receiveThread.Join(100))
                    {
                        m_receiveThread.Abort();
                    }
                }
            }
            #endregion

            #region Receiving
            private List<int> FindControlSequenceIndices(List<byte> bytes)
            {
                List<int> indices = new List<int>();

                int index = -1;
                int start = 0;

                while (start < bytes.Count - 4
                    && (index = bytes.IndexOf(255, start)) != -1)
                {
                    // we found a 255
                    bool complete = false;
                    if (index < bytes.Count - 3)
                    {
                        complete = true;
                        for (int j = 1; j < 4; j++)
                        {
                            complete = complete && (bytes[index + j] == 255);
                        }
                    }

                    if (complete)
                    {
                        indices.Add(index);
                    }
                    start = index + 4;
                }
                return indices;
            }

            private void ProcessBuffer()
            {
                if (Configuration.Network.UdpProtocol == UdpProtocol.COBS)
                {
                    if (!m_bufferSynchronized)
                    {
                        // we have not found a zero yet
                        // is there one in the buffer?
                        int index = m_buffer.IndexOf(0);
                        if (index >= 0)
                        {
                            // we have a zero, trim the buffer
                            if (index > 0)
                            {
                                // only if there's stuff up front
                                m_buffer.RemoveRange(0, index + 1);
                            }

                            // now, the buffer is synchronized
                            m_bufferSynchronized = true;
                        }
                    }

                    if (m_bufferSynchronized)
                    {
                        // find the next zero
                        int index = -1;
                        while ((index = m_buffer.IndexOf(0)) > 0)
                        {
                            // extract the packet
                            byte[] rawBytes = m_buffer.GetRange(0, index).ToArray();
                            m_buffer.RemoveRange(0, index + 1);

                            byte[] bytes = COBSEncoding.Decode(rawBytes);

                            PacketReceived?.Invoke(this, bytes);
                        }
                    }
                }
                else
                {
                    // let's find the second-last four FFs first
                    int firstIndex = -1;
                    List<int> indices = FindControlSequenceIndices(m_buffer);
                    if (indices.Count == 1)
                    {
                        firstIndex = indices[0];
                    }
                    else if (indices.Count > 1)
                    {
                        firstIndex = indices[indices.Count - 2];
                    }

                    if (firstIndex >= 0)
                    {
                        // strip everything before (not needed anymore)
                        m_buffer.RemoveRange(0, firstIndex);

                        // do we have the length?
                        int payloadLength = -1;
                        if (m_buffer.Count >= 6)
                        {
                            payloadLength = BitConverter.ToUInt16(new byte[] { m_buffer[4], m_buffer[5] }, 0);
                        }

                        // do we have enough bytes
                        if (payloadLength > 0
                            && m_buffer.Count >= 2 + 4 + payloadLength)
                        {                            
                            // get the payload
                            List<byte> payload = m_buffer.GetRange(4 + 2, payloadLength);

                            // check that there's no control sequence in there
                            List<int> subIndices = FindControlSequenceIndices(payload);
                            if (subIndices.Count > 0)
                            {
                                m_buffer.RemoveRange(0, 2 + 4 + subIndices.Last());
                            }
                            else
                            {
                                // remove those bytes (not needed anymore)
                                m_buffer.RemoveRange(0, 2 + 4 + payloadLength);

                                // fire the event
                                PacketReceived?.Invoke(this, payload.ToArray());
                            }
                        }
                    }
                }
            }

            private void Receive()
            {
                while (IsRunning)
                {
                    byte[] bytes = m_client.Receive(ref m_receiveEndPoint);
                    BytesReceived?.Invoke(this, bytes);

                    m_buffer.AddRange(bytes);
                    ProcessBuffer();
                }
            }
            #endregion
        }
        #endregion
    }
    #endregion
}
