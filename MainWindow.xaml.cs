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
    using System.Collections.Generic;

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


        private static HandState lastHandLeftState;
        private static HandState lastHandRightState;
        private const int MINIMUM_NUMBER_OF_STRESSED_BEATS = 3;
        private static bool timeSignatureIsEstablished = false;
        private static int stressedBeatsCounter = 0;
        private static List<double> beatTimes;
        private static double pitch = 0;

        private static bool currentlyPlayingNote = false;

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

            // Set up OSC
            osc = new UdpWriter(oscHost, oscPort);

            oscBeat = new UdpWriter(oscHost, 8000);

            beatTimes = new List<double>();

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

        private bool IsBeat(Body body)
        {
            return body.HandLeftState == HandState.Open
                   && body.HandRightState == HandState.Closed
                   && lastHandLeftState == HandState.Closed
                   && lastHandRightState == HandState.Closed;
        }

        private bool IsStressedBeat(Body body)
        {
            return body.HandLeftState == HandState.Closed
                   && body.HandRightState == HandState.Open
                   && lastHandLeftState == HandState.Closed
                   && lastHandRightState == HandState.Closed;
        }


        private void CheckForTimeSignature(Body body, double timeInMilliseconds)
        {

            if (IsStressedBeat(body))
            {
                stressedBeatsCounter++;
                Console.WriteLine("Stressed beat: " + stressedBeatsCounter);
                Console.WriteLine("time: " + timeInMilliseconds);
                beatTimes.Add(timeInMilliseconds);
            }
            else if (IsBeat(body))
            {
                Console.WriteLine("regular beat: " + beatTimes.Count);
                Console.WriteLine("time: " + timeInMilliseconds);
                beatTimes.Add(timeInMilliseconds);
            }
            if (stressedBeatsCounter > MINIMUM_NUMBER_OF_STRESSED_BEATS)
            {
                Console.WriteLine("Establishing time signature");
                (new TimeSignature(beatTimes, stressedBeatsCounter - 1)).Establish();
                timeSignatureIsEstablished = true;
            }

        }


        private void Process(BodyFrame bodyFrame)
        {
            TimeSpan relativeTime = bodyFrame.RelativeTime;

            // Selects the first body that is tracked and use that for our calculations
            Body body = System.Linq.Enumerable.FirstOrDefault(this.bodies, bod => bod.IsTracked);

            if (body != null && body.IsTracked)
            {
                drawer.Draw(body);
                /*      if (!timeSignatureIsEstablished)
                      {
                          CheckForTimeSignature(body, relativeTime.TotalMilliseconds);
                      }*/
                // else
                //{
                Instrument i = new Instrument("instr0");

                if (body.HandLeftState == HandState.Open && !currentlyPlayingNote)
                {




                    OscElement elem = new OscElement("/instr3", 60, 127, 1000, 1, 0);
                    osc.Send(elem);
                    Console.WriteLine("sending elem");

                    //          OscElement elem2 = new OscElement("/sustain3", 0);
                    //         osc.Send(elem2);
                    //           i.PlayNote(30, 1000, 10, 1, 1);
                    currentlyPlayingNote = true;
                }
                if (currentlyPlayingNote)
                {
                    if (pitch > 100) { pitch = 0; }
                    osc.Send(new OscElement("/test", (float)pitch));
                    pitch += .1;

                    //    Filter f = new Filter("instr0");
                    //    f.SendFilterData(body);

                    if (body.HandLeftState == HandState.Closed)
                    {
                        OscElement elem = new OscElement("/sustain3", 0);
                        osc.Send(elem);
                        //           Console.WriteLine("STOP NOTE");
                        //           i.StopNote();
                        currentlyPlayingNote = false;
                    }


                }


                lastHandLeftState = body.HandLeftState;
                lastHandRightState = body.HandRightState;
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
