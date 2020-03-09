using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoTube.Devices
{
    #region Class 'EchoPoint'
    public class EchoPoint
    {
        #region Constructors
        internal EchoPoint(int id, double distance, 
            double pressure, int rawDistance, double rawValue)
        {
            Id = id;
            Distance = distance;
            Pressure = pressure;

            RawDistance = rawDistance;
            RawValue = rawValue;
        }
        #endregion

        #region Properties
        public int Id { get; internal set; }

        public double Distance { get; internal set; }

        public int RawDistance { get; internal set; }

        public double Pressure { get; internal set; }

        public double RawValue { get; internal set; }
        #endregion

        #region Updates
        internal void Update(EchoPoint point)
        {
            Distance = point.Distance;
            Pressure = point.Pressure;

            RawDistance = point.RawDistance;
            RawValue = point.RawValue;
        }
        #endregion
    }
    #endregion
}
