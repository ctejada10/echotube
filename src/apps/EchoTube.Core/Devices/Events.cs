using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Accord.Audio;
using Accord.Audio.Filters;
using Accord.Math;

namespace EchoTube.Devices
{
    #region Delegates
    public delegate void SensorDataEventHandler(object sender, SensorDataEventArgs e);

    internal delegate void RawSensorDataEventHandler(object sender, ushort[] data);

    public delegate void EchoPointEventHandler(object sender, EchoPointEventArgs e);
    #endregion

    #region Class 'SensorDataEventArgs'
    public class SensorDataEventArgs : EventArgs
    {
        #region Constructors
        internal SensorDataEventArgs() { }

        internal SensorDataEventArgs(EchoTubeSensor sensor, 
            float[] rawData, float[] filteredData)
        {
            Sensor = sensor;

            RawData = rawData;
            FilteredData = filteredData;
        }
        #endregion

        #region Properties
        public EchoTubeSensor Sensor { get; private set; }

        public float[] RawData { get; internal set; }

        public float[] FilteredData { get; internal set; }

        public int[] Peaks { get; internal set; } = new int[] { };

        public Tuple<int, int>[] Areas { get; internal set; } = new Tuple<int, int>[] { };

        public float[] Amplitudes { get; internal set; } = new float[] { };

        public double Threshold { get; internal set; } = 0.0;

        internal Tuple<int, int, int, float>[] RawPeaks
        {
            set
            {
                int numberOfPeaks = value.Length;

                int[] peaks = new int[numberOfPeaks];
                Tuple<int, int>[] areas = new Tuple<int, int>[numberOfPeaks];
                float[] amplitudes = new float[numberOfPeaks];

                for (int i = 0; i < numberOfPeaks; i++)
                {
                    areas[i] = new Tuple<int, int>(value[i].Item1, value[i].Item2);
                    peaks[i] = value[i].Item3;
                    amplitudes[i] = value[i].Item4;
                }

                Peaks = peaks;
                Areas = areas;
                Amplitudes = amplitudes;
            }
        }

        public Tuple<int, int> CutOffs { get; set; } = new Tuple<int, int>(0, 0);
        #endregion
    }
    #endregion

    #region Class 'EchoPointEventArgs'
    public class EchoPointEventArgs : EventArgs
    {
        #region Constructors
        internal EchoPointEventArgs(EchoPoint point)
        {
            Point = point;
        }
        #endregion

        #region Properties
        public EchoPoint Point { get; private set; }
        #endregion
    }
    #endregion
}
