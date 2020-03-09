using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EchoTube.Net;

namespace EchoTube.Devices.Sources
{
    #region Class 'NetworkDataProvider'
    internal class NetworkDataProvider : IDataSampleProvider
    {
        #region Class Members
        private Udp.Receiver m_conn;
        #endregion

        #region Constructors
        internal NetworkDataProvider()
        {
            InitializeConnection();
        }
        #endregion

        #region Initialization
        private void InitializeConnection()
        {
            m_conn = new Udp.Receiver();
        }
        #endregion

        #region Start/Stop
        public override void Start()
        {
            if (m_conn != null
                && !m_conn.IsRunning)
            {
                m_conn.PacketReceived += ConnectionPacketReceived;
                m_conn.Start();
            }
        }

        public override void Stop()
        {
            if (m_conn != null
                && m_conn.IsRunning)
            {
                m_conn.PacketReceived -= ConnectionPacketReceived;
                m_conn.Stop();
            }
        }
        #endregion

        #region Event Handling
        private void ConnectionPacketReceived(object sender, byte[] bytes)
        {
            if (bytes.Length % 2 == 0)
            {
                ushort[] data = new ushort[bytes.Length / 2];
                Parallel.For(0, data.Length, i =>
                {
                    data[i] = BitConverter.ToUInt16(bytes, 2 * i);
                });

                OnDataReceived(data);
            }
        }
        #endregion
    }
    #endregion
}
