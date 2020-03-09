using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoTube.Devices
{
    #region Class 'EchoTubeSettings'
    internal class EchoTubeSettings
    {
        #region Properties
        public int StartCutOff { get; internal set; }

        public int EndCutOff { get; internal set; }

        public float Alpha { get; internal set; }

        public int UpperTouchThreshold { get; internal set; }

        public int LowerTouchThreshold { get; internal set; }

        public bool SingleTouch { get; internal set; }
        #endregion
    }
    #endregion
}
