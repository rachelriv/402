using Microsoft.Kinect;
using System;
using Ventuz.OSC;

namespace Instrumovement
{
    internal class TimeSignature
    {

        public bool isEstablished;

        private Instrument beat;

        private static UdpWriter recordingState = new UdpWriter("127.0.0.1", 9001);

        private const int STRESSED_BEAT_PITCH = 40;

        private const int REGULAR_BEAT_PITCH = 60;

        private const string BEAT_NAME = "beat0";

        public TimeSignature()
        {
            beat = new Instrument(BEAT_NAME);
            recordingState.Send(new OscElement("/beat0state", "Overdub"));
        }

        private bool IsBeat()
        {
            return MainWindow.currentBody.HandLeftState == HandState.Open && MainWindow.lastHandLeftState == HandState.Closed;
        }

        private bool IsStressedBeat()
        {
            return MainWindow.currentBody.HandRightState == HandState.Open && MainWindow.lastHandRightState == HandState.Closed;
        }


        public void CheckForBeats()
        {
            if (IsStressedBeat())
            {
                beat.PlayNote(STRESSED_BEAT_PITCH);
            }
            else if (IsBeat())
            {
                beat.PlayNote(REGULAR_BEAT_PITCH);
            } 
            else if (MainWindow.currentBody.HandRightState == HandState.Closed && MainWindow.currentBody.HandLeftState == HandState.Closed)
            {
                beat.StopNote();
            } 
            if (MainWindow.currentBody.HandRightState == HandState.Lasso)
            {
                Console.WriteLine("Establishing time signature");
                Establish();
                return;
            }

        }

        private void Establish()
        {
            recordingState.Send(new OscElement("/" + BEAT_NAME + "state", "Stop"));
            this.isEstablished = true;
        }

    }
}