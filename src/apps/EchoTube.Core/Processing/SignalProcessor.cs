using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EchoTube.Devices;

using Accord.Audio;
using Accord.Audio.Filters;

namespace EchoTube.Processing
{
    #region Enumeration 'Detector'
    [Flags]
    public enum Detector : int
    {
        Old = 1,
        New = 2,
        Advanced = 3
    }
    #endregion

    #region Class 'SignalProcessor'
    public class SignalProcessor
    {
        #region Constant Fields
        public const double ErrorDetectionThreshold = 25.0;
        #endregion

        #region Properties
        private float[] m_previousData;
        #endregion

        #region Constructors
        public SignalProcessor(EchoTubeSensor sensor)
        {
            Sensor = sensor;
        }
        #endregion

        #region Properties
        public EchoTubeSensor Sensor { get; private set; }
        #endregion

        #region Signal Processing
        private float[] PreprocessSignal(SignalCalibration calibration, float[] rawData, bool amplify)
        {
            if (amplify && false)
            {
                for (int i = 0; i < rawData.Length; i++)
                {
                    if (i >= Sensor.StartCutOff && i <= rawData.Length - Sensor.EndCutOff)
                    {
                        // amplify the data
                        // double factor = 2.0 * Math.Pow(Sensor.GetDistance(i), 3.0) + 1.0;
                        // double factor = 4.0 * Math.Pow(Sensor.GetDistance(i), 2.0) + 1.0;
                        double factor = 1.0 * Math.Pow(Sensor.GetDistance(i), 2.0) + 1.0;
                        factor = 1.0;
                        // rawData[i] *= 4.0f;
                        // factor *= 2.0;

                        // SMALL
                        // double factor = 8.0 * Math.Pow(Sensor.GetDistance(i), 4.0) + 1.0;

                        rawData[i] *= (float)factor;
                    }
                }
            }

            // let the calibration do its job
            float[] calibratedEnvelope = calibration.GetCalibratedSignal(rawData);

            if (amplify)
            {
                for (int i = 0; i < calibratedEnvelope.Length; i++)
                {
                    if (i >= Sensor.StartCutOff && i <= rawData.Length - Sensor.EndCutOff)
                    {
                        // amplify the data
                        // double factor = 2.0 * Math.Pow(Sensor.GetDistance(i), 3.0) + 1.0;

                        // SMALL
                        double factor = 8.0 * Math.Pow(Sensor.GetDistance(i), 2.0) + 1.0;
                        factor *= 2.0;

                        // LARGE
                        /* factor = 2.0 * Math.Pow(Sensor.GetDistance(i), 3.0) + 1.0;
                        factor *= 2.0; */

                        calibratedEnvelope[i] *= (float)factor;
                    }
                }
            }

            return calibratedEnvelope;
        }
        
        private float[] PreprocessSignal(float[] rawData, bool amplify)
        {
            // preprocess the peaks
            for (int i = 0; i < rawData.Length; i++)
            {
                if (i <= Sensor.StartCutOff)
                {
                    rawData[i] *= i / (float)Sensor.StartCutOff;
                }
                else if (i >= rawData.Length - Sensor.EndCutOff)
                {
                    rawData[i] *= (rawData.Length - i + 1) / (float)Sensor.EndCutOff;
                }

                // amplify the data
                if (amplify)
                {
                    // double factor = 2.0 * Math.Pow(Sensor.GetDistance(i), 3.0) + 1.0;
                    double factor = 4.0 * Math.Pow(Sensor.GetDistance(i), 2.0) + 1.0;
                    // rawData[i] *= 4.0f;
                    // factor *= 4.0;
                    rawData[i] *= (float)factor;
                }
            }

            Signal signal = Signal.FromArray(rawData, EchoTubeSensor.SampleRate, SampleFormat.Format32BitIeeeFloat);

            EnvelopeFilter filter = new EnvelopeFilter(Sensor.EnvelopeAlpha);
            Signal envelopedSignal = filter.Apply(signal);

            float[] currData = envelopedSignal.ToFloat();

            /* float avg = currData.Average();
            rawData = rawData.Select(x => x - avg).ToArray();
            currData = currData.Select(x => x - avg).ToArray(); */

            /* if (m_previousData == null)
            {
                m_previousData = new float[currData.Length];
            }
            else
            {
                if (checkPrevious)
                {
                    double absDifference = 0.0;
                    for (int i = 0; i < currData.Length; i++)
                    {
                        absDifference += Math.Abs(currData[i] - m_previousData[i]);
                    }
                    absDifference /= currData.Length;

                    // Debug.WriteLine(absDifference);
                    if (absDifference >= ErrorDetectionThreshold && false)
                    {
                        // this is a wrong signal
                        // throw it away
                        return null;
                    }
                    else
                    {
                        // signal was good, store the current as previous
                        Array.Copy(currData, m_previousData, currData.Length);
                    }
                }
            } */

            return currData;
        }

        private Tuple<int, float> GetFirstPeak(float[] envelopedData)
        {
            int index = -1;
            float max = Sensor.UpperTouchThreshold;

            for (int i = Sensor.StartCutOff; i < envelopedData.Length - Sensor.EndCutOff; i++)
            {
                if (envelopedData[i] >= max)
                {
                    max = envelopedData[i];
                    index = i;
                }
            }

            return new Tuple<int, float>(index, max);
        }

        private List<Tuple<int, int>> GetRawAreas(float[] envelopedData, double threshold)
        {
            int offset = 1;
            List<int> intersects = new List<int>();
            List<Tuple<int, int>> areas = new List<Tuple<int, int>>();

            // we get all intersects of the threshold with the signal
            for (int i = Sensor.StartCutOff + offset; i < envelopedData.Length - Sensor.EndCutOff - offset; i++)
            {
                if (intersects.Count == 0)
                {
                    if (envelopedData[i - offset] < threshold
                        && envelopedData[i + offset] >= threshold)
                    {
                        intersects.Add(i);
                    }
                    continue;
                }

                if (Math.Sign(envelopedData[i - offset] - threshold)
                    != Math.Sign(envelopedData[i + offset] - threshold))
                {
                    intersects.Add(i);
                }
            }

            // this should actually not happen!
            if (intersects.Count % 2 != 0)
            {
                intersects.RemoveAt(intersects.Count - 1);
            }

            // now, we can check the areas:
            //  - remove if not wide enough
            //  - remove area is too small
            for (int i = 1; i < intersects.Count; i++)
            {
                double area = 0.0;
                double localMax = threshold;
                int localIndex = -1;

                for (int j = intersects[i - 1]; j <= intersects[i]; j++)
                {
                    area += envelopedData[j] - threshold;
                    if (envelopedData[j] > localMax)
                    {
                        localMax = envelopedData[j];
                        localIndex = j;
                    }
                }

                if (area > 0
                    && localIndex != -1
                    && (intersects[i] - intersects[i - 1] >= 10))
                {
                    areas.Add(new Tuple<int, int>(intersects[i - 1], intersects[i]));

                    // find the peak in that area
                    // peaks.Add(localIndex);
                }
            }

            return areas;
        }

        private List<Tuple<int, int>> FilterAreas(List<Tuple<int, int>> allAreas, float[] envelopedData)
        {
            List<Tuple<int, int>> filteredAreas = new List<Tuple<int, int>>(allAreas);
            bool nothingChanged = true;

            do
            {
                nothingChanged = true;
                for (int i = 1; i < filteredAreas.Count; i++)
                {
                    Tuple<int, int> first = filteredAreas[i - 1];
                    Tuple<int, int> second = filteredAreas[i];

                    // do we need to flip them?
                    if (first.Item2 > second.Item1)
                    {
                        Tuple<int, int> temp = first;
                        first = second;
                        second = temp;
                    }

                    // first, we check the enveloped data between
                    int start = first.Item2;
                    int end = second.Item1;

                    float min = float.MaxValue;
                    for (int j = start; j <= end; j++)
                    {
                        if (envelopedData[j] <= min)
                        {
                            min = envelopedData[j];
                        }
                    }

                    if (min >= Sensor.LowerTouchThreshold)
                    {
                        // these two are not separate peaks, there's no valley in between
                        // merge them
                        Tuple<int, int> mergedArea = new Tuple<int, int>(first.Item1, second.Item2);

                        filteredAreas.RemoveRange(i - 1, 2);
                        filteredAreas.Add(mergedArea);

                        i--;
                        nothingChanged = false;
                    }
                }
            } while (!nothingChanged);

            return filteredAreas;
        }

        private List<Tuple<int, int, int, float>> GetPeaks(List<Tuple<int, int>> areas, float[] envelopedData)
        {
            List<Tuple<int, int, int, float>> peaks = new List<Tuple<int, int, int, float>>();

            // first, construct all peaks
            foreach (Tuple<int, int> area in areas)
            {
                int start = area.Item1;
                int end = area.Item2;

                float max = float.MinValue;
                int index = -1;

                for (int i = start; i <= end; i++)
                {
                    if (envelopedData[i] > max)
                    {
                        max = envelopedData[i];
                        index = i;
                    }
                }

                if (index >= start
                    && index <= end)
                {
                    peaks.Add(new Tuple<int, int, int, float>(start, end, index, max));
                }
            }

            // new, we filter out the two best peaks
            if (peaks.Count > Sensor.NumberOfTouchPoints)
            {
                peaks = peaks.OrderByDescending(x => x.Item4).ToList();
                peaks.RemoveRange(Sensor.NumberOfTouchPoints, peaks.Count - Sensor.NumberOfTouchPoints);
            }

            return peaks;
        }

        public SensorDataEventArgs ProcessData(SignalCalibration calibration, float[] rawData, bool amplify)
        {
            // data setup
            double threshold = 0.0;

            // preprocess the data
            float[] envelopedData = PreprocessSignal(calibration, rawData, amplify);
            if (envelopedData == null)
            {
                // this is a faulty signal, return 'null'
                return null;
            }

            SensorDataEventArgs eventArgs = new SensorDataEventArgs(Sensor, rawData, envelopedData)
            {
                CutOffs = new Tuple<int, int>(Sensor.StartCutOff, Sensor.EndCutOff)
            };

            // get the first/highest peak (if any exists)
            Tuple<int, float> firstPeak = GetFirstPeak(envelopedData);

            // check whether we found a peak
            if (firstPeak.Item1 != -1)
            {
                // we in fact did!
                // now, let's get the areas (all of them)
                threshold = 1.0 * firstPeak.Item2 / 1.5;
                threshold = Sensor.UpperTouchThreshold;
                Sensor.LowerTouchThreshold = (float)threshold;

                // Debug.WriteLine(Sensor.LowerTouchThreshold);

                // threshold = Sensor.UpperTouchThreshold;
                // Sensor.LowerTouchThreshold = (float)threshold;

                List<Tuple<int, int>> areas = GetRawAreas(envelopedData, threshold);

                // filter them (by merging)
                List<Tuple<int, int>> filteredAreas = FilterAreas(areas, envelopedData);

                // get the raw peaks from those areas
                List<Tuple<int, int, int, float>> peaks = GetPeaks(filteredAreas, envelopedData);

                // add the values to the result
                eventArgs.RawPeaks = peaks.ToArray();
                eventArgs.Threshold = threshold;
            }

            return eventArgs;
        }

        public SensorDataEventArgs ProcessData(ushort[] rawData, bool amplify)
        {
            // data setup
            double threshold = 0.0;
            float[] calibratedData = rawData.Select(x => (float)x).ToArray();

            // preprocess the data
            float[] envelopedData = PreprocessSignal(calibratedData, amplify);
            if (envelopedData == null)
            {
                // this is a faulty signal, return 'null'
                return null;
            }

            SensorDataEventArgs eventArgs = new SensorDataEventArgs(Sensor, calibratedData, envelopedData)
            {
                CutOffs = new Tuple<int, int>(Sensor.StartCutOff, Sensor.EndCutOff)
            };

            // get the first/highest peak (if any exists)
            Tuple<int, float> firstPeak = GetFirstPeak(envelopedData);

            // check whether we found a peak
            if (firstPeak.Item1 != -1)
            {
                // we in fact did!
                // now, let's get the areas (all of them)
                threshold = 1.0 * firstPeak.Item2 / 2.5;
                // threshold = Sensor.UpperTouchThreshold;
                // Sensor.LowerTouchThreshold = (float)threshold;

                // Debug.WriteLine(Sensor.LowerTouchThreshold);

                List<Tuple<int, int>> areas = GetRawAreas(envelopedData, threshold);

                // filter them (by merging)
                List<Tuple<int, int>> filteredAreas = FilterAreas(areas, envelopedData);

                // get the raw peaks from those areas
                List<Tuple<int, int, int, float>> peaks = GetPeaks(filteredAreas, envelopedData);

                // add the values to the result
                eventArgs.RawPeaks = peaks.ToArray();
                eventArgs.Threshold = threshold;
            }

            return eventArgs;
        }
        #endregion
    }
    #endregion
}
