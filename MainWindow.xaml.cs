//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Instrumovement
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Ventuz.OSC;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        BodyDrawer drawer = null;

        public static JointPositions jointPositions;

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


        public static HandState lastHandLeftState;
        public static HandState lastHandRightState;


        private static TimeSignature timeSignature;

        public static Body currentBody;

        private static bool currentlyPlayingNote = false;

        private static bool currentlyPlayingFastNote = false;
        private static bool currentlyPlayingSlowNote = false;

        /// <summary>
        /// Current status text to display
        /// </summary>
        public static UdpWriter osc;

        public static UdpWriter oscBeat;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {

            jointPositions = new JointPositions();

            timeSignature = new TimeSignature();

            // Set up OSC
            osc = new UdpWriter(oscHost, oscPort);

            oscBeat = new UdpWriter(oscHost, 8000);

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
                    // Console.WriteLine("BODY LENGTH: " + this.bodies.Length);


                    //                    bodyFrames.AddFrame(bodyFrame);

                    // Run the core body processing routine
                    Process(bodyFrame);
                }
            }
        }

        public static CameraSpacePoint CopyPosition(CameraSpacePoint position)
        {
            CameraSpacePoint result = new CameraSpacePoint();
            result.X = position.X;
            result.Y = position.Y;
            result.Z = position.Z;
            return result;
        }



        private void Process(BodyFrame bodyFrame)
        {
            double currentTime = bodyFrame.RelativeTime.TotalMilliseconds;

            // Selects the first body that is tracked and use that for our calculations
            Body body = System.Linq.Enumerable.FirstOrDefault(this.bodies, bod => bod.IsTracked);

            if (body != null && body.IsTracked)
            {
                TimedPosition timedPositionOfHandLeft = new TimedPosition(currentTime, CopyPosition(body.Joints[JointType.HandLeft].Position));
                jointPositions.AddPosition(JointType.HandLeft, timedPositionOfHandLeft);

                TimedPosition timedPositionOfShoulderLeft = new TimedPosition(currentTime, CopyPosition(body.Joints[JointType.ShoulderLeft].Position));
                jointPositions.AddPosition(JointType.ShoulderLeft, timedPositionOfShoulderLeft);


                currentBody = body;
                drawer.Draw();
                Instrument i = new Instrument("suhdude");
                Instrument fast = new Instrument("instr1");
                Instrument slow = new Instrument("instr0");


                /*    if (currentBody.HandLeftState == HandState.Lasso && !currentlyPlayingNote)
                    {
                        i.PlayNote(60, 127, 500, 1, 0);
                        currentlyPlayingNote = true;
                    }


                    if (currentBody.HandLeftState == HandState.Closed)
                    {
                        i.StopNote();
                        currentlyPlayingNote = false;
                    }

         /*           if (!timeSignature.isEstablished)
                    {
                        timeSignature.CheckForBeats(currentTime);
                    }
                    else
                    {*/
                if (currentBody.HandLeftState == HandState.Open)
                {
                    double handShoulderRelativeVelocity = VelocityComputer.GetRelativeVelocity(JointType.ShoulderLeft, JointType.HandLeft);


                    if (handShoulderRelativeVelocity > 2.0 && !currentlyPlayingFastNote)
                    {
                        if (currentlyPlayingSlowNote)
                        {
                            slow.StopNote();
                            currentlyPlayingSlowNote = false;
                        }
                        int pitch = (int)((currentBody.Joints[JointType.HandLeft].Position.X) * 100) % 200 + 100;
                        fast.PlayNote(pitch);
                        currentlyPlayingFastNote = true;
                    }
                    else if (handShoulderRelativeVelocity <= 2.0)
                    {
                        double vol = ((currentBody.Joints[JointType.HandLeft].Position.Z) * 100) % 200 + 20;
                  //      OscElement vol2 = new OscElement("/instr0vol", vol);
                   //     osc.Send(vol2);
                        if (!currentlyPlayingSlowNote)
                        {
                            if (currentlyPlayingFastNote)
                            {
                                fast.StopNote();
                                currentlyPlayingFastNote = false;
                            }
                            slow.PlayNote(60);
                            currentlyPlayingSlowNote = true;

                        }
                    }




                }
                if (currentBody.HandLeftState == HandState.Closed)
                {
                    slow.StopNote();
                    fast.StopNote();
                    currentlyPlayingSlowNote = false;
                    currentlyPlayingFastNote = false;
                }
                //  }


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
