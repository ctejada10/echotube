using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using MahApps.Metro.Controls;

using WindowsInput;
using WindowsInput.Native;

using AudioSwitcher.AudioApi.CoreAudio;

using EchoTube.Devices;

namespace RobustPlayer
{
    #region Class 'MainWindow'
    public partial class MainWindow : MetroWindow
    {
        #region Const Fields
        public const int MinimumVolume = 0;

        public const int MaximumVolume = 80;

        public const double DistanceDelta = 0.06; // meters
        #endregion

        #region Class Members
        private EchoTubeSensor m_sensor;

        private InputSimulator m_inputSimulator = new InputSimulator();

        private CoreAudioDevice m_audioDevice;

        private Size m_volumeRectSize;

        private List<Tuple<double, VirtualKeyCode, string>> m_discreteEvents;

        private Tuple<double, double> m_sliderRange;
        #endregion

        #region Constructors
        public MainWindow()
        {
            InitializeComponent();
            InitializeControls();
            InitializeSensor();
        }
        #endregion

        #region Initialization
        private void InitializeAudioDevice()
        {
            m_audioDevice = new CoreAudioController().DefaultPlaybackDevice;

            // let's adjust the volume bar
            double width = m_audioDevice.Volume / MaximumVolume;
            VolumeRect.Width = width * m_volumeRectSize.Width;
        }

        private void InitializeControls()
        {
            m_discreteEvents = new List<Tuple<double, VirtualKeyCode, string>>
            {
                new Tuple<double, VirtualKeyCode, string>(0.888, VirtualKeyCode.MEDIA_PLAY_PAUSE, "Play"),
                new Tuple<double, VirtualKeyCode, string>(1.157, VirtualKeyCode.MEDIA_PREV_TRACK, "Previous"),
                new Tuple<double, VirtualKeyCode, string>(1.78, VirtualKeyCode.MEDIA_NEXT_TRACK, "Next")
            };

            m_sliderRange = new Tuple<double, double>(1.35, 1.5);
        }

        private void InitializeSensor()
        {
            m_sensor = new EchoTubeSensor();

            // attach event handlers
            m_sensor.TouchDown += SensorTouchDown;
            m_sensor.TouchMove += SensorTouchMove;
            m_sensor.TouchUp += SensorTouchUp;                                                                                        

            // start the sensor
            m_sensor.Start();
        }
        #endregion

        #region Properties
        public double Volume
        {
            get { return m_audioDevice.Volume; }
            set
            {
                m_audioDevice.Volume = value;

                Dispatcher.Invoke(
                    new Action(
                        delegate ()
                        {
                            // let's adjust the volume bar
                            double width = value / MaximumVolume;
                            VolumeRect.Width = width * m_volumeRectSize.Width;
                        }));
            }
        }
        #endregion

        #region Volume Controls
        private void AdjustVolume(double percentage)
        {
            // check this
            percentage = Math.Min(Math.Max(0.0, percentage), 1.0);
            Volume = percentage * MaximumVolume;
        }
        #endregion

        #region UI Updates
        private void UpdateLabel(string label)
        {
            Dispatcher.Invoke(
                new Action(
                    delegate ()
                    {
                        LastKeyLabel.Content = label.ToUpper();
                        LastKeyLabel.Opacity = 1.0;

                        DoubleAnimation animation = new DoubleAnimation
                        {
                            From = 1.0,
                            To = 0.0,
                            Duration = new Duration(TimeSpan.FromSeconds(2))
                        };

                        LastKeyLabel.BeginAnimation(OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);
                    }));
        }
        #endregion

        #region Checking
        private void PerformDiscreteInput(double distance)
        {
            // check the events
            for (int i = 0; i < m_discreteEvents.Count; i++)
            {
                double lower = m_discreteEvents[i].Item1 - DistanceDelta / 2.0;
                double upper = m_discreteEvents[i].Item1 + DistanceDelta / 2.0;

                if (distance >= lower
                    && distance <= upper)
                {
                    // yay!
                    m_inputSimulator.Keyboard.KeyPress(m_discreteEvents[i].Item2);

                    // change label
                    UpdateLabel(m_discreteEvents[i].Item3);
                    break;
                }
            }
        }
        #endregion

        #region Shutdown
        private void Shutdown()
        {
            if (m_sensor != null)
            {
                // detach event handlers
                m_sensor.TouchDown -= SensorTouchDown;
                m_sensor.TouchMove -= SensorTouchMove;
                m_sensor.TouchUp -= SensorTouchUp;

                // stop the sensor
                m_sensor.Stop();
            }

            if (m_audioDevice != null)
            {
                m_audioDevice.Dispose();
                m_audioDevice = null;
            }

            Thread.Sleep(20);
        }
        #endregion

        #region Event Handling
        #region Window Events
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            m_volumeRectSize = new Size(
                VolumeRectContainer.ActualWidth - 2.0, 
                VolumeRectContainer.ActualHeight - 2.0);

            InitializeAudioDevice();
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            Shutdown();
        }

        private void WindowKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else
            {
                // testing
                if (e.Key == Key.D1)
                {
                    // prev
                    PerformDiscreteInput(0.58);
                }
                else if (e.Key == Key.D2)
                {
                    // next
                    PerformDiscreteInput(0.82);
                }
                else if (e.Key == Key.D3)
                {
                    // play/pause
                    PerformDiscreteInput(0.39);
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
                    }
                }
                else if (e.Key == Key.C)
                {
                    // calbrate the device
                    m_sensor.DeviceState = DeviceState.Startup;
                }
            }
        }
        #endregion

        #region EchoTube Sensor Events
        private void SensorTouchDown(object sender, EchoPointEventArgs e)
        {
            // not much to do here
        }

        private void SensorTouchMove(object sender, EchoPointEventArgs e)
        {
            // is in slider range?
            if (e.Point.Distance >= m_sliderRange.Item1
                && e.Point.Distance <= m_sliderRange.Item2)
            {
                // we are in the slider
                // adjust the values
                double percentage = (e.Point.Distance - m_sliderRange.Item1)
                    / (m_sliderRange.Item2 - m_sliderRange.Item1);
                AdjustVolume(percentage);
            }
        }

        private void SensorTouchUp(object sender, EchoPointEventArgs e)
        {
            PerformDiscreteInput(e.Point.Distance);
        }
        #endregion
        #endregion
    }
    #endregion
}
