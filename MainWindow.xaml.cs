namespace Instrumovement
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System;
    using Drawing;
    using BodyTracking;
    using Config;
    using Music;
    using System.Collections.Generic;

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
        /// Mapping of steady/moving joint velocity to instrument numbers
        /// </summary>
        public static Dictionary<Tuple<JointType, JointType>, Dictionary<String, Instrument>> instrumentsForJointPair = null;

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

        /// <summary>
        /// Body of current frame 
        /// </summary>
        public static Body currentBody;

        /// <summary>
        /// Last states of hands
        /// </summary>
        public static HandState lastHandLeftState;
        public static HandState lastHandRightState;

        /// <summary>
        /// Time signature object that establishes the rhythm of the music
        /// </summary>
        private static TimeSignature timeSignature;

        /// <summary>
        /// Pairs of joints used for calculating relative velocity
        /// </summary>
        public static Dictionary<String, Tuple<JointType, JointType>> steadyMovingJointPairs = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // initialize the joint pairs (one steady and one moving per pair)
            JointPairMappings.Initialize();

            // initialize the instruments used for each joint pair
            InstrumentMappings.Initialize();

            // records of previous joint positions
            jointRecords = new JointRecords();

            // initialize time signature
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

                drawer.Draw();

                // add all joint positions to the records
                jointRecords.AddRecordForEachJoint(currentTime);

                // Main sound mapping logic
                // First, establish the time signature (with specific gestures)
                // Then, do general sound-movement mappings
                if (!timeSignature.isEstablished)
                {
                    timeSignature.CheckForBeats();
                }
                else
                {
                    SoundCreator.Create();
                }

                // update last hand state
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
