using Microsoft.Kinect;
using System;
using Ventuz.OSC;

namespace Instrumovement
{
    internal class TimeSignature
    {

        public bool isEstablished;
        Instrument beat0 = new Instrument("beat0", "127.0.0.1", 8000);
        Instrument beat1 = new Instrument("beat1", "127.0.0.1", 8000);
        UdpWriter oscloop = new UdpWriter("127.0.0.1", 9001);

        private bool IsBeat()
        {
            return MainWindow.currentBody.HandLeftState == HandState.Open
                   && MainWindow.currentBody.HandRightState == HandState.Closed
                   && BothHandsLastClosed();
        }

        private bool IsStressedBeat()
        {
            return MainWindow.currentBody.HandLeftState == HandState.Closed
                   && MainWindow.currentBody.HandRightState == HandState.Open
                   && BothHandsLastClosed();
        }

        private bool BothHandsLastClosed()
        {
            return MainWindow.lastHandLeftState == HandState.Closed
                   && MainWindow.lastHandRightState == HandState.Closed;
        }


        public void CheckForBeats(double timeInMilliseconds)
        {
            if (IsStressedBeat())
            {
                beat0.PlayNote(40);
            }
            else if (IsBeat())
            {
                beat0.PlayNote(60);
            }
            if (MainWindow.currentBody.HandLeftState == HandState.Open && MainWindow.currentBody.HandRightState == HandState.Open)
            {
                Console.WriteLine("Establishing time signature");
                Establish();
                return;
            }

        }

        public TimeSignature()
        {
            oscloop.Send(new OscElement("/beat0state", "Overdub"));
        }

        private void Establish()
        {
            oscloop.Send(new OscElement("/beat0state", "Play"));
            this.isEstablished = true;
        }

    }
}