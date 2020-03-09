using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Accord.Audio;
using Accord.Audio.Filters;

using EchoTube.Devices;

namespace EchoTube.Processing
{
    #region Class 'SignalCalibration'
    public class SignalCalibration
    {
        #region Class Members
        private List<float[]> m_rawSignals;

        private float[] m_envelopedAvg;
        #endregion

        #region Constructors
        public SignalCalibration(EchoTubeSensor sensor)
        {
            Sensor = sensor;
            IsValid = false;

            m_rawSignals = new List<float[]>();
        }
        #endregion

        #region Properties
        public EchoTubeSensor Sensor { get; private set; }

        public bool IsValid { get; private set; }
        #endregion

        #region Sampling
        public void AddSample(ushort[] rawSignal)
        {
            float[] rawData = rawSignal.Select(x => (float)x).ToArray();
            AddSample(rawData);
        }

        public void AddSample(float[] rawSignal)
        {
            m_rawSignals.Add(rawSignal);
        }
        #endregion

        #region Resetting
        public void Reset()
        {
            m_rawSignals.Clear();
            m_envelopedAvg = null;

            IsValid = false;
        }
        #endregion

        #region Applying
        public float[] GetCalibratedSignal(float[] data)
        {
            Signal signal = Signal.FromArray(data, EchoTubeSensor.SampleRate, SampleFormat.Format32BitIeeeFloat);

            EnvelopeFilter filter = new EnvelopeFilter(Sensor.EnvelopeAlpha);
            Signal envelopedSignal = filter.Apply(signal);

            float[] envelopedData = envelopedSignal.ToFloat();
            if (IsValid)
            {
                Parallel.For(0, envelopedData.Length, i =>
                {
                    /* if (Math.Abs(envelopedData[i] - m_envelopedAvg[i]) >= 200)
                        Debug.WriteLine(envelopedData[i] + " :: " + m_envelopedAvg[i] + " :: " + i); */

                    // envelopedData[i] = Math.Abs(envelopedData[i] - m_envelopedAvg[i]);
                    envelopedData[i] = Math.Max(0.0f, envelopedData[i] - m_envelopedAvg[i]);
                    // envelopedData[i] = envelopedData[i] - m_envelopedAvg[i];
                });
            }
            return envelopedData;
        }
        #endregion

        #region Calibration
        public void Calibrate()
        {
            double[] envelopeAverage = null;

            // create the envelope for each signal
            for (int i = 0; i < m_rawSignals.Count; i++)
            {
                Signal signal = Signal.FromArray(m_rawSignals[i], 
                    EchoTubeSensor.SampleRate, SampleFormat.Format32BitIeeeFloat);

                LowPassFilter lowPassFilter = new LowPassFilter(100.0, EchoTubeSensor.SampleRate)
                {
                    Alpha = 0.05f
                };
                Signal filteredSignal = lowPassFilter.Apply(signal);

                // this is the filtered signal (and a single baseline)
                float[] filteredRawSData = filteredSignal.ToFloat();
                float[] processedData = new float[filteredRawSData.Length];

                // now, shift and rectify the raw data
                Parallel.For(0, processedData.Length, j =>
                {
                    // shift & rectify
                    processedData[j] = Math.Abs(m_rawSignals[i][j] - filteredRawSData[j]);
                });

                Signal processedSignal = Signal.FromArray(processedData,
                    EchoTubeSensor.SampleRate, SampleFormat.Format32BitIeeeFloat);

                // now, we can create the calibration envelope
                EnvelopeFilter filter = new EnvelopeFilter(Sensor.EnvelopeAlpha);
                Signal envelopedSignal = filter.Apply(processedSignal);

                float[] envelopedData = envelopedSignal.ToFloat();
                if (envelopeAverage == null)
                {
                    envelopeAverage = new double[envelopedData.Length];
                }

                Parallel.For(0, envelopedData.Length, j =>
                {
                    envelopeAverage[j] += envelopedData[j];
                });
            }

            // now, we need to average over the envelope
            if (m_rawSignals.Count > 0)
            {
                Parallel.For(0, envelopeAverage.Length, i =>
                {
                    envelopeAverage[i] /= m_rawSignals.Count;
                });

                m_envelopedAvg = envelopeAverage.Select(x => (float)x).ToArray();
                IsValid = true;
            }
        }
        #endregion
    }
    #endregion
}
