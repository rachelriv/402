namespace Instrumovement
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Ventuz.OSC;
    using System;
    using Instrumovement.Drawing;
    using Instrumovement.BodyTracking;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Collection of joint positions with their timestamps kept for calculating velocity
        /// </summary>
        public static JointRecords jointRecords;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
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
        /// Reader for body frames
        /// </summary>
        private BodyDrawer drawer = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        public static HandState lastHandLeftState;
        public static HandState lastHandRightState;

        private static TimeSignature timeSignature;

        public static Body currentBody;


        private static bool currentlyPlayingFastNote = false;
        private static bool currentlyPlayingSlowNote = false;


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {

            jointRecords = new JointRecords();

            timeSignature = new TimeSignature();


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
            double currentTime = bodyFrame.RelativeTime.TotalMilliseconds;

            // Selects the first body that is tracked and use that for our calculations
            Body body = System.Linq.Enumerable.FirstOrDefault(this.bodies, bod => bod.IsTracked);

            if (body != null && body.IsTracked)
            {
                currentBody = body;

                jointRecords.AddRecordForEachJoint(currentTime);

                drawer.Draw();

                Instrument fast = new Instrument("instr1", "127.0.0.1", 22345);
                Instrument slow = new Instrument("instr0", "127.0.0.1", 22345);

                if (!timeSignature.isEstablished)
                {
                    timeSignature.CheckForBeats();
                }
                else
                {
                    double handShoulderRelativeVelocity = VelocityComputer.GetRelativeVelocity(JointType.ShoulderLeft, JointType.HandLeft);

                    if (currentBody.HandLeftState == HandState.Open && currentBody.HandRightState == HandState.Open)
                    {
                        if (currentlyPlayingSlowNote)
                        {
                            OscElement elem = new OscElement("/test", (new Random()).NextDouble());
                            UdpWriter test = new UdpWriter("127.0.0.1", 9001);
                            test.Send(elem);
                        }
                        if (handShoulderRelativeVelocity > 2.0 && !currentlyPlayingFastNote)
                        {
                            if (currentlyPlayingSlowNote)
                            {
                                slow.StopNote();
                                currentlyPlayingSlowNote = false;
                            }
                            fast.PlayNote(60);
                            currentlyPlayingFastNote = true;
                        }
                        else if (handShoulderRelativeVelocity <= 2.0 && !currentlyPlayingSlowNote)
                        {
;
                            if (!currentlyPlayingSlowNote)
                            {
                                if (currentlyPlayingFastNote)
                                {
                                    fast.StopNote();
                                    currentlyPlayingFastNote = false;
                                }
                                slow.PlayNote(60, 127, 500, 1);
                                currentlyPlayingSlowNote = true;

                            }
                        }
                    }


                    else if (currentBody.HandLeftState == HandState.Closed && currentBody.HandRightState == HandState.Closed)
                    {
                        slow.StopNote();
                        fast.StopNote();
                        currentlyPlayingSlowNote = false;
                        currentlyPlayingFastNote = false;
                    }
                }

                lastHandLeftState = currentBody.HandLeftState;
                lastHandRightState = currentBody.HandRightState;
            }

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
