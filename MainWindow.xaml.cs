namespace Instrumovement
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System;
    using Instrumovement.Drawing;
    using Instrumovement.BodyTracking;
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
        ///  List of instruments
        /// </summary>
        public static Instrument[] instruments;

        /// <summary>
        /// Mapping of steady/moving joint velocity to instrument number
        /// </summary>
        public static Dictionary<Tuple<JointType, JointType>, Dictionary<String, Instrument>> instrumentsForJointPair = new Dictionary<Tuple<JointType, JointType>, Dictionary<String, Instrument>>();

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

        
        private static Dictionary<String, Tuple<JointType, JointType>> steadyMovingVelocityPairs = new Dictionary<string, Tuple<JointType, JointType>>();



        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {

            steadyMovingVelocityPairs.Add("shoulderHandLeft", new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.HandLeft));
            steadyMovingVelocityPairs.Add("shoulderHandRight", new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.HandRight));
            steadyMovingVelocityPairs.Add("hipFootLeft", new Tuple<JointType, JointType>(JointType.HipLeft, JointType.FootLeft));
            steadyMovingVelocityPairs.Add("hipFootRight", new Tuple<JointType, JointType>(JointType.HipRight, JointType.FootRight));
            steadyMovingVelocityPairs.Add("shoulderLeftRight", new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ShoulderRight));


            Dictionary<String, Instrument> instrumentsForShoulderHandLeft = new Dictionary<string, Instrument>();
            instrumentsForShoulderHandLeft.Add("Slow", new Instrument("instr0"));
            instrumentsForShoulderHandLeft.Add("Fast", new Instrument("instr1"));

            instrumentsForJointPair.Add(steadyMovingVelocityPairs["shoulderHandLeft"], instrumentsForShoulderHandLeft);

            Dictionary<String, Instrument> instrumentsForShoulderHandRight = new Dictionary<string, Instrument>();
            instrumentsForShoulderHandRight.Add("Slow", new Instrument("instr2"));
            instrumentsForShoulderHandRight.Add("Fast", new Instrument("instr3"));

            instrumentsForJointPair.Add(steadyMovingVelocityPairs["shoulderHandRight"], instrumentsForShoulderHandRight);

            Dictionary<String, Instrument> instrumentsForHipFootLeft = new Dictionary<string, Instrument>();
            instrumentsForHipFootLeft.Add("Slow", new Instrument("instr4"));
            instrumentsForHipFootLeft.Add("Fast", new Instrument("instr5"));

            instrumentsForJointPair.Add(steadyMovingVelocityPairs["hipFootLeft"], instrumentsForHipFootLeft);

            Dictionary<String, Instrument> instrumentsForHipFootRight = new Dictionary<string, Instrument>();
            instrumentsForHipFootRight.Add("Slow", new Instrument("instr6"));
            instrumentsForHipFootRight.Add("Fast", new Instrument("instr7"));

            instrumentsForJointPair.Add(steadyMovingVelocityPairs["hipFootRight"], instrumentsForHipFootRight);

            Dictionary<String, Instrument> instrumentsForShoudlerLeftRight = new Dictionary<string, Instrument>();
            instrumentsForShoudlerLeftRight.Add("Slow", new Instrument("instr4"));
            instrumentsForShoudlerLeftRight.Add("Fast", new Instrument("instr5"));

            instrumentsForJointPair.Add(steadyMovingVelocityPairs["shoulderLeftRight"], instrumentsForShoudlerLeftRight);



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


                if (!timeSignature.isEstablished)
                {
                    timeSignature.CheckForBeats();
                }
                else
                {

                    double leftShoulderHandRelativeVelocity = VelocityComputer.GetRelativeVelocity(JointType.ShoulderLeft, JointType.HandLeft);

                    if (leftShoulderHandRelativeVelocity > 2.0)
                    {
                        PlayFastNoteForJointPair(steadyMovingVelocityPairs["shoulderHandLeft"]);
                    } else if (leftShoulderHandRelativeVelocity > .5)
                    {
                        PlaySlowNoteForJointPair(steadyMovingVelocityPairs["shoulderHandLeft"]);
                    } else
                    {
                        StopAll();
                    }

              

                    if (currentBody.HandLeftState == HandState.Closed && currentBody.HandRightState == HandState.Closed)
                    {
                        StopAll();
                    }
                }

                lastHandLeftState = currentBody.HandLeftState;
                lastHandRightState = currentBody.HandRightState;
            }

        }

        private void StopAll()
        {
            foreach (Dictionary<String, Instrument> instrumentMapping in instrumentsForJointPair.Values)
            {
                foreach (Instrument i in instrumentMapping.Values)
                {
                    i.StopNote();
                }
            }
        }

        private void PlaySlowNoteForJointPair(Tuple<JointType, JointType> jointPair)
        {
            if (!instrumentsForJointPair[jointPair]["Slow"].isPlaying)
            {
                if (instrumentsForJointPair[jointPair]["Fast"].isPlaying)
                {
                    instrumentsForJointPair[jointPair]["Fast"].StopNote();
                }
                instrumentsForJointPair[jointPair]["Slow"].PlayNote(60, 127, 500, 1);
            }
        }

        private void PlayFastNoteForJointPair(Tuple<JointType, JointType> jointPair)
        {
            if (!instrumentsForJointPair[jointPair]["Fast"].isPlaying)
            {
                if (instrumentsForJointPair[jointPair]["Slow"].isPlaying)
                {
                    instrumentsForJointPair[jointPair]["Slow"].StopNote();
                }
                instrumentsForJointPair[jointPair]["Fast"].PlayNote(60);
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
