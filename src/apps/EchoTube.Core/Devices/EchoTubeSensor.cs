using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Accord.Audio;
using Accord.Audio.Filters;

using EchoTube.Devices.Sources;
using EchoTube.Processing;

namespace EchoTube.Devices
{
    #region Enumeration 'DeviceState'
    [Flags]
    public enum DeviceState : int
    {
        Startup = 1,
        Running = 2
    }
    #endregion

    #region Class 'EchoTubeSensor'
    public class EchoTubeSensor
    {
        #region Static Fields
        private const int NumberOfCalibrationSamples = 10;

        public const int SampleRate = 312361;

        public const double SpeedOfSound = 343.0;
        #endregion

        #region Class Members
        private IDataSampleProvider m_networkProvider;

        private bool m_running = false;

        private int m_calibrationCounter = 0;

        private List<EchoPoint> m_prevPoints = new List<EchoPoint>();

        private DeviceState m_state = DeviceState.Startup;

        private SignalCalibration m_calibration;

        private SignalProcessor m_signalProcessor;

        private Detector m_detectorType = Detector.New;

        private double[] m_calibrationSamples;
        #endregion

        #region Events
        public event EventHandler DeviceStarted;

        public event EventHandler DeviceStopped;

        public event EventHandler StateChanged;

        public event SensorDataEventHandler DataReceived;

        public event EchoPointEventHandler TouchDown;

        public event EchoPointEventHandler TouchMove;

        public event EchoPointEventHandler TouchUp;
        #endregion

        #region Constructors
        public EchoTubeSensor()
            : this(null)
        { }

        public EchoTubeSensor(string fileName)
        {
            if (fileName != null)
            {
                // load the settings
                LoadSettings(fileName);
            }

            InitializeUdp();
            InitializeSignalProcessor();
        }
        #endregion

        #region Initialization
        private void InitializeUdp()
        {
            m_networkProvider = new NetworkDataProvider();
        }

        private void InitializeSignalProcessor()
        {
            m_calibration = new SignalCalibration(this);
            m_signalProcessor = new SignalProcessor(this);
        }
        #endregion

        #region Properties
        public bool IsRunning
        {
            get { return m_running; }
            private set
            {
                bool changed = m_running != value;

                m_running = value;
                if (changed && m_running)
                {
                    DeviceStarted?.Invoke(this, EventArgs.Empty);
                }
                else if (changed && !m_running)
                {
                    DeviceStopped?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public DeviceState DeviceState
        {
            get { return m_state; }
            set
            {
                bool changed = value != m_state;
                if (changed)
                {
                    if (value == DeviceState.Startup)
                    {
                        if (m_detectorType == Detector.Old)
                        {
                            m_calibrationCounter = 0;
                            m_calibrationSamples = null;
                        }
                        else
                        {
                            m_calibration.Reset();
                            m_calibrationCounter = 0;
                        }
                    }

                    m_state = value;

                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public float EnvelopeAlpha { get; set; } = 0.05f;

        public int StartCutOff { get; set; } = 350;

        public int EndCutOff { get; set; } = 100;

        public float UpperTouchThreshold { get; set; } = 250.0f;

        public float LowerTouchThreshold { get; set; } = 50.0f;

        public int NumberOfTouchPoints { get; set; } = 2;
        #endregion

        #region Start/Stop
        public void Start()
        {
            if (!IsRunning)
            {
                m_calibration.Reset();
                m_calibrationCounter = 0;

                DeviceState = DeviceState.Startup;

                m_networkProvider.RawDataReceived += ProviderDataReceived;
                m_networkProvider.Start();

                IsRunning = true;
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;

                m_networkProvider.RawDataReceived -= ProviderDataReceived;
                m_networkProvider.Stop();
            }
        }
        #endregion

        #region Settings Hanlding
        public void LoadSettings(string fileName)
        {
            EchoTubeSettings settings = SettingsFileHandler.LoadSettings(fileName);
            if (settings != null)
            {
                StartCutOff = settings.StartCutOff;
                EndCutOff = settings.EndCutOff;
                EnvelopeAlpha = settings.Alpha;
                UpperTouchThreshold = settings.UpperTouchThreshold;
                LowerTouchThreshold = settings.LowerTouchThreshold;
                NumberOfTouchPoints = settings.SingleTouch ? 1 : 2;
            }
        }

        public void SaveSettings(string fileName)
        {
            EchoTubeSettings settings = new EchoTubeSettings
            {
                StartCutOff = StartCutOff,
                EndCutOff = EndCutOff,
                Alpha = EnvelopeAlpha,
                UpperTouchThreshold = (int)UpperTouchThreshold,
                LowerTouchThreshold = (int)LowerTouchThreshold,
                SingleTouch = NumberOfTouchPoints == 1
            };

            SettingsFileHandler.SaveSettings(fileName, settings);
        }
        #endregion

        #region Touch Processing
        internal double GetDistance(int location)
        {
            double time = location * 1.0 / SampleRate + 200.0 / 1000.0 / 1000.0;
            double distance = SpeedOfSound * time / 2.0;

            return distance;
        }

        private void ProcessTouches(SensorDataEventArgs eventArgs)
        {
            List<EchoPoint> currPts = new List<EchoPoint>();

            // first: calculate the new points
            for (int i = 0; i < eventArgs.Peaks.Length; i++)
            {
                double distance = GetDistance(eventArgs.Peaks[i]);

                double pressure = eventArgs.Amplitudes[i] / 1000.0;
                pressure = Math.Min(Math.Max(0.0, pressure), 1.0);

                EchoPoint echoPt = new EchoPoint(i, distance, pressure, 
                    eventArgs.Peaks[i], eventArgs.Amplitudes[i]);
                currPts.Add(echoPt);
            }

            // match the points
            if (m_prevPoints.Count == 0)
            {
                // we did not have previous points
                if (currPts.Count != 0)
                {
                    // match those points to new points
                    for (int i = 0; i < currPts.Count; i++)
                    {
                        m_prevPoints.Add(currPts[i]);
                        TouchDown?.Invoke(this, new EchoPointEventArgs(currPts[i]));
                    }
                }
                else
                {
                    // nothing to do here
                }
            }
            else if (m_prevPoints.Count == 1)
            {
                // there was one previous point
                if (currPts.Count == 0)
                {
                    // the point disappeared
                    TouchUp?.Invoke(this, new EchoPointEventArgs(m_prevPoints[0]));
                    m_prevPoints.RemoveAt(0);
                }
                else if (currPts.Count == 1)
                {
                    // this is the same point (double-check distance)
                    m_prevPoints[0].Update(currPts[0]);
                    TouchMove?.Invoke(this, new EchoPointEventArgs(m_prevPoints[0]));
                }
                else if (currPts.Count == 2)
                {
                    // we added a point
                    // find out which one was the original
                    double distance1 = Math.Abs(currPts[0].Distance - m_prevPoints[0].Distance);
                    double distance2 = Math.Abs(currPts[1].Distance - m_prevPoints[0].Distance);
                    int correctId = m_prevPoints[0].Id == 0 ? 1 : 0;

                    if (distance1 > distance2)
                    {
                        // the first point is new
                        currPts[0].Id = correctId;

                        m_prevPoints[0].Update(currPts[1]);
                        TouchMove?.Invoke(this, new EchoPointEventArgs(m_prevPoints[0]));

                        m_prevPoints.Add(currPts[0]);
                        TouchDown?.Invoke(this, new EchoPointEventArgs(currPts[0]));
                    }
                    else
                    {
                        // the other one is new
                        currPts[1].Id = correctId;

                        m_prevPoints[0].Update(currPts[0]);
                        TouchMove?.Invoke(this, new EchoPointEventArgs(m_prevPoints[0]));

                        m_prevPoints.Add(currPts[1]);
                        TouchDown?.Invoke(this, new EchoPointEventArgs(currPts[1]));
                    }
                }
            }
            else if (m_prevPoints.Count == 2)
            {
                // there were two previous points
                if (currPts.Count == 0)
                {
                    // the points disappeared
                    TouchUp?.Invoke(this, new EchoPointEventArgs(m_prevPoints[0]));
                    TouchUp?.Invoke(this, new EchoPointEventArgs(m_prevPoints[1]));

                    m_prevPoints.Clear();
                }
                else if (currPts.Count == 1)
                {
                    // we removed only one point
                    // find out which one
                    double distance1 = Math.Abs(currPts[0].Distance - m_prevPoints[0].Distance);
                    double distance2 = Math.Abs(currPts[0].Distance - m_prevPoints[1].Distance);

                    if (distance1 > distance2)
                    {
                        // the first previous is gone
                        m_prevPoints[1].Update(currPts[0]);
                        TouchMove?.Invoke(this, new EchoPointEventArgs(m_prevPoints[1]));

                        TouchUp?.Invoke(this, new EchoPointEventArgs(m_prevPoints[0]));
                        m_prevPoints.RemoveAt(0);
                    }
                    else
                    {
                        // the second previous is gone
                        m_prevPoints[0].Update(currPts[0]);
                        TouchMove?.Invoke(this, new EchoPointEventArgs(m_prevPoints[0]));

                        TouchUp?.Invoke(this, new EchoPointEventArgs(m_prevPoints[1]));
                        m_prevPoints.RemoveAt(1);
                    }
                }
                else
                {
                    // both are still here -> update them
                    bool prevFlipped = m_prevPoints[0].Distance > m_prevPoints[1].Distance;
                    bool currFlipped = currPts[0].Distance > currPts[1].Distance;

                    if ((!prevFlipped && !currFlipped)
                        || (prevFlipped && currFlipped))
                    {
                        m_prevPoints[0].Update(currPts[0]);
                        m_prevPoints[1].Update(currPts[1]);

                        TouchMove?.Invoke(this, new EchoPointEventArgs(m_prevPoints[0]));
                        TouchMove?.Invoke(this, new EchoPointEventArgs(m_prevPoints[1]));
                    }
                    else if ((!prevFlipped && currFlipped)
                        || (prevFlipped && !currFlipped))
                    {
                        m_prevPoints[0].Update(currPts[1]);
                        m_prevPoints[1].Update(currPts[0]);

                        TouchMove?.Invoke(this, new EchoPointEventArgs(m_prevPoints[0]));
                        TouchMove?.Invoke(this, new EchoPointEventArgs(m_prevPoints[1]));
                    }
                }
            }
        }
        #endregion

        #region Event Handling
        private void ProviderDataReceived(object sender, ushort[] data)
        {
            // check the device state
            if (DeviceState == DeviceState.Startup)
            {
                if (m_detectorType == Detector.Old)
                {
                    if (m_calibrationSamples == null)
                    {
                        m_calibrationSamples = new double[data.Length];
                    }

                    Parallel.For(0, data.Length, i =>
                    {
                        m_calibrationSamples[i] += data[i];
                    });
                    m_calibrationCounter++;

                    if (m_calibrationCounter == NumberOfCalibrationSamples)
                    {
                        Parallel.For(0, data.Length, i =>
                        {
                            m_calibrationSamples[i] /= m_calibrationCounter;
                        });

                        DeviceState = DeviceState.Running;
                    }
                }
                else
                {
                    if (!m_calibration.IsValid)
                    {
                        m_calibration.AddSample(data);
                        m_calibrationCounter++;

                        if (m_calibrationCounter == NumberOfCalibrationSamples)
                        {
                            // now, we can calibrate
                            m_calibration.Calibrate();

                            if (m_calibration.IsValid)
                            {
                                DeviceState = DeviceState.Running;
                            }
                            else
                            {
                                m_calibration.Reset();
                            }
                        }
                    }
                }
            }
            else if (DeviceState == DeviceState.Running)
            {
                // preprocess the signal (get baseline and shift)
                float[] rawData = data.Select(value => (float)value).ToArray();
                ushort[] calibratedData = new ushort[rawData.Length];

                if (m_detectorType == Detector.Old)
                {
                    // calibrate
                    Parallel.For(0, rawData.Length, i =>
                    {
                        calibratedData[i] = (ushort)Math.Abs(rawData[i] - m_calibrationSamples[i]);
                    });
                }
                else if (m_detectorType == Detector.New)
                {
                    Signal rawSignal = Signal.FromArray(rawData, SampleRate, SampleFormat.Format32BitIeeeFloat);
                    LowPassFilter lowPassFilter = new LowPassFilter(100.0, SampleRate)
                    {
                        Alpha = 0.05f
                    };
                    rawSignal = lowPassFilter.Apply(rawSignal);

                    float[] baseline = rawSignal.ToFloat();
                    Parallel.For(0, rawData.Length, i =>
                    {
                        rawData[i] = Math.Abs(rawData[i] - baseline[i]);
                    });
                }
                else if (m_detectorType == Detector.Advanced)
                {

                }

                // good, raw data is shifted now
                SensorDataEventArgs eventArgs = null;
                if (m_detectorType == Detector.Old)
                {
                    eventArgs = m_signalProcessor.ProcessData(calibratedData, true);
                }
                else
                {
                    eventArgs = m_signalProcessor.ProcessData(m_calibration, rawData, true);
                }

                // notify the receviers about the raw data
                DataReceived?.Invoke(this, eventArgs);

                // now, do touch processing
                ProcessTouches(eventArgs);
            }
        }
        #endregion
    }
    #endregion
}
