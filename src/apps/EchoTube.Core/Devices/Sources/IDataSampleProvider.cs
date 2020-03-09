using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoTube.Devices.Sources
{
    #region Abstract Class 'IDataSampleProvider'
    internal abstract class IDataSampleProvider
    {
        #region Class Members

        #endregion

        #region Events
        internal event RawSensorDataEventHandler RawDataReceived;
        #endregion

        #region Constructors
        protected IDataSampleProvider() { }
        #endregion

        #region Properties

        #endregion

        #region Start/Stop
        public abstract void Start();

        public abstract void Stop();
        #endregion

        #region Updates
        protected void OnDataReceived(ushort[] data)
        {
            RawDataReceived?.Invoke(this, data);
        }
        #endregion
    }
    #endregion
}
