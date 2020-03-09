using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using EchoTube.Devices;

namespace BicycleCounter
{
    #region Partial Class 'MainWindow'
    public partial class MainWindow : Window
    {
        #region Class Members
        private EchoTubeSensor m_sensor;

        private int m_bikeCounter = 0;

        private double m_lastSpeed = 0.0;

        private int m_lastDirection = 0;

        private Timer m_trackTimer;

        private Timer m_coolOffTimer;

        private bool m_tracking = false;

        private bool m_enabled = true;

        private EchoPoint[] m_detection = new EchoPoint[2];

        private Stopwatch m_stopwatch = new Stopwatch();

        private readonly object m_syncObj = new object();
        #endregion

        #region Constructors
        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
            InitializeSensor();

            UpdateUI();
        }
        #endregion

        #region Initialization
        private void InitializeTimer()
        {
            m_trackTimer = new Timer
            {
                Interval = 500.0
            };
            m_trackTimer.Elapsed += TrackTimerElapsed;

            m_coolOffTimer = new Timer
            {
                Interval = 1000.0
            };
            m_coolOffTimer.Elapsed += CoolOffTimerElapsed;
        }

        private void InitializeSensor()
        {
            m_sensor = new EchoTubeSensor();

            // attach event handlers
            m_sensor.DataReceived += SensorRawDataReceived;

            m_sensor.TouchDown += SensorTouchDown;
            m_sensor.TouchMove += SensorTouchMove;
            m_sensor.TouchUp += SensorTouchUp;

            // start the sensor
            m_sensor.Start();
        }
        #endregion

        #region Shutdown
        private void Shutdown()
        {
            if (m_sensor != null)
            {
                // detach event handlers
                m_sensor.DataReceived -= SensorRawDataReceived;

                m_sensor.TouchDown -= SensorTouchDown;
                m_sensor.TouchMove -= SensorTouchMove;
                m_sensor.TouchUp -= SensorTouchUp;

                // start the sensor
                m_sensor.Stop();
            }

            System.Threading.Thread.Sleep(20);
        }
        #endregion

        #region UI Updates
        private void UpdateUI()
        {
            Dispatcher.Invoke(
                new Action(
                    delegate ()
                    {
                        if (m_bikeCounter == 0)
                        {
                            RightArrow.Visibility = Visibility.Hidden;
                            LeftArrow.Visibility = Visibility.Hidden;
                            SpeedLabel.Content = "";
                        }
                        else
                        {
                            CounterLabel.Content = m_bikeCounter.ToString();
                            RightArrow.Visibility = m_lastDirection == 0 ? Visibility.Visible : Visibility.Hidden;
                            LeftArrow.Visibility = m_lastDirection == 1 ? Visibility.Visible : Visibility.Hidden;
                            SpeedLabel.Content = m_lastSpeed.ToString("F1") + " KM/H";
                        }
                    }));
        }
        #endregion

        #region Tracking
        private void ResetTracking()
        {
            m_detection[0] = null;
            m_detection[1] = null;
        }

        private void CalculateBike()
        {
            m_enabled = false;
            m_coolOffTimer.Start();

            long time = m_stopwatch.ElapsedMilliseconds;

            EchoPoint first = m_detection[0];
            EchoPoint second = m_detection[1];

            if (first != null
                && second != null)
            {
                m_bikeCounter++;

                // get direction
                m_lastDirection = first.Distance < second.Distance ? 0 : 1;

                // get speed
                double seconds = time / 1000.0;
                double meters = 0.32;

                double metersPerSecond = meters / seconds;

                m_lastSpeed = metersPerSecond * 3.6;
            }

            ResetTracking();
        }
        #endregion

        #region Event Handling
        #region Window Events
        private void WindowKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.S)
            {
                // load a file
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(
                    Assembly.GetEntryAssembly().Location) + "\\Files"))
                {
                    return;
                }

                string fileName = null;

                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "EchoTube Settings (*.ets)|*.ets",
                    InitialDirectory = System.IO.Path.GetDirectoryName(
                        Assembly.GetEntryAssembly().Location) + "\\Files"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    fileName = openFileDialog.FileName;
                }

                if (fileName != null)
                {
                    // open the file
                    m_sensor.LoadSettings(fileName);
                    m_sensor.EndCutOff = 1000;
                }
            }
            else if (e.Key == Key.C)
            {
                // calbrate the device
                m_sensor.DeviceState = DeviceState.Startup;
            }
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            Shutdown();
        }

        private void TrackTimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock (m_syncObj)
            {
                if (m_tracking)
                {
                    m_trackTimer.Stop();
                    m_tracking = false;

                    ResetTracking();
                }
            }
        }

        private void CoolOffTimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock (m_syncObj)
            {
                m_enabled = true;
                m_coolOffTimer.Stop();
            }
        }
        #endregion

        #region EchoTube Sensor Events
        private void SensorRawDataReceived(object sender, SensorDataEventArgs e)
        {
            // let's see whether we want that
        }

        private void SensorTouchDown(object sender, EchoPointEventArgs e)
        {

        }

        private void SensorTouchMove(object sender, EchoPointEventArgs e)
        {
            
        }

        private void SensorTouchUp(object sender, EchoPointEventArgs e)
        {
            lock (m_syncObj)
            {
                if (!m_enabled)
                {
                    return;
                }

                if (!m_tracking)
                {
                    m_detection[0] = e.Point;
                    m_stopwatch.Restart();
                
                    m_trackTimer.Start();
                    m_tracking = true;
                }
                else
                {
                    m_tracking = false;
                    m_trackTimer.Stop();

                    m_detection[1] = e.Point;

                    CalculateBike();
                    UpdateUI();

                    m_stopwatch.Reset();
                }
            }
        }
        #endregion
        #endregion
    }
    #endregion
}
