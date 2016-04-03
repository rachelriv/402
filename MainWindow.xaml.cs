//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Ventuz.OSC;
    using System.Timers;
    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        BodyDrawer drawer = null;

        private ImageSource imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        // =========  OSC ===========
        /// <summary>
        /// The host ip address (the computer with Ableton + Max for Live on it).
        /// </summary>
        private String oscHost = "127.0.0.1";


        /// <summary>
        /// The port Ableton + Max for Live is listening on for UDP messages.
        /// </summary>
        private static int oscPort = 22345;


        private static Point lastWristLocation;
        private static HandState lastHandState;
        private static int tempoCounter = 0;
        private static double[] tempos = new double[5];
        private static bool isPlaying = false;
        private static Timer beatTimer;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private UdpWriter osc;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // Set up OSC
            osc = new UdpWriter(oscHost, oscPort);

            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            drawer = new BodyDrawer(this.kinectSensor.CoordinateMapper,
                                    this.kinectSensor.DepthFrameSource.FrameDescription,
                                    new DrawingGroup());

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();


            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            this.imageSource = new DrawingImage(drawer.drawingGroup);

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;


            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }



        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    // Run the core body processing routine
                    Process(bodyFrame);
                }
            }
        }


        private void Process(BodyFrame bodyFrame)
        {
            TimeSpan relativeTime = new TimeSpan();
            // Selects the first body that is tracked and use that for our calculations
            Body b = System.Linq.Enumerable.FirstOrDefault(this.bodies, bod => bod.IsTracked);
            if (b != null && b.IsTracked)
            {


                drawer.Draw(b);

                if (b.HandLeftState == HandState.Open && lastHandState == HandState.Closed && tempoCounter < 5)
                {

                    tempos[tempoCounter] = relativeTime.TotalMilliseconds;

                    tempoCounter++;

                    if (tempoCounter == 5)
                    {
                        double[] tempoDifferences = new double[4];
                        for (int i = 0; i < 4; i++)
                        {
                            tempoDifferences[i] = tempos[i + 1] - tempos[i];
                        }
                        double sum = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            sum += tempoDifferences[i];
                        }
                        double averageTempoDiff = sum / 4.0;
                        Console.WriteLine("AVERAGE TEMPO DIFF: " + averageTempoDiff);
                        beatTimer = new Timer(averageTempoDiff);
                        beatTimer.Elapsed += OnTimedEvent;
                        beatTimer.AutoReset = true;
                        beatTimer.Enabled = true;


                    }

                }
                /**         if (b.HandRightState == HandState.Open)
                         {
                             Console.WriteLine("HEIGHT: " + jointPoints[JointType.HandRight].Y);
                             int pitch = (int)Math.Round(127 - jointPoints[JointType.HandRight].Y / 4);
                             Console.WriteLine("PITCH: " + pitch);
                             OscElement p = new OscElement("/pitch3", 30);
                             osc.Send(p);
                             if (!isPlaying)
                             {
                                 OscElement elem = new OscElement("/instr3", pitch, 127, 10, 1, 1);
                                 osc.Send(elem);
                                 isPlaying = true;
                             }

                             if (b.HandRightState == HandState.Closed)
                             {
                                 Console.WriteLine("HEIGHT: " + jointPoints[JointType.HandRight].Y);
                             //    int pitch = 100 - (int)Math.Round((jointPoints[JointType.HandRight]).Y) / 4;
                                 OscElement elem = new OscElement("/sustain3", 0);
                                 osc.Send(elem);
                             }

                             lastHandState = b.HandLeftState;

                             if (lastWristLocation == null)
                             {
                                 lastWristLocation = jointPoints[JointType.WristLeft];
                             }

                             else
                             {
                                 Point newPosition = jointPoints[JointType.WristLeft];
                                 double deltaX = lastWristLocation.X - newPosition.X;
                                 double deltaY = lastWristLocation.Y - newPosition.Y;
                                 double totalDisplacement = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
                                 if (false)
                                 {
                                     OscElement elem1 = new OscElement("/sustain3", 0);
                                     osc.Send(elem1);
                                     osc.Send(new OscElement("/pitch0", 10));
                                     Console.WriteLine("SENDING instr 0");
                                     OscElement elem2 = new OscElement("/instr0", 50, 127, 500, 1, 1);
                                     osc.Send(elem2);
                                     osc.Send(new OscElement("/sustain0", 0));


                                 }
                                 lastWristLocation = newPosition;

                             }*/

            }

        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            OscElement pitch1 = new OscElement("/instr4", 20, 1000, 10, 1);
            osc.Send(pitch1);
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
