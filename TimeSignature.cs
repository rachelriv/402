using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Timers;
using Ventuz.OSC;

namespace Instrumovement
{
    internal class TimeSignature
    {
        enum beatType :  int { stressed=0, regular=1 };
        private Timer beatTimer;
        private List<double> beatTimes;
        private int numOfStressedBeats;
        public bool isEstablished;
        private int totalNumberOfBeatsPlayed;
        private const int MINIMUM_NUM_OF_STRESSED_BEATS = 3;
        Instrument beat0 = new Instrument("beat0", "127.0.0.1", 8000);
        Instrument beat1 = new Instrument("beat1", "127.0.0.1", 8000);
        UdpWriter oscloop = new UdpWriter("127.0.0.1", 9001);
        private bool firstBeat;

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
                if (!firstBeat)
                {

                    firstBeat = true;

                }
                beat0.PlayNote(60);
                Console.WriteLine("Stressed beat: " + numOfStressedBeats);
                beatTimes.Add(timeInMilliseconds);

                numOfStressedBeats++;
            }
            else if (IsBeat())
            {
                beat1.PlayNote(60);
                Console.WriteLine("Regular beat: " + beatTimes.Count);
                beatTimes.Add(timeInMilliseconds);
            }
            if (MainWindow.currentBody.HandLeftState == HandState.Open && MainWindow.currentBody.HandRightState == HandState.Closed)
            {
                Console.WriteLine("Establishing time signature");
                Establish();
                return;
            }

        }

        public TimeSignature()
        {
            oscloop.Send(new OscElement("/state0", "Overdub"));
            oscloop.Send(new OscElement("/state1", "Overdub"));


            this.beatTimes = new List<double>();
            this.numOfStressedBeats = 1;
            this.isEstablished = false;
            this.totalNumberOfBeatsPlayed = 0;
        }

        private void Establish()
        {
            /*         double sumOfBeatTimeDifferences = 0;
                     for (int i = 0; i < beatTimes.Count - 1; i++)
                     {
                         sumOfBeatTimeDifferences += beatTimes[i + 1] - beatTimes[i];
                     }
                     beatTimer = new Timer(sumOfBeatTimeDifferences / ((double)beatTimes.Count));
                     beatTimer.Elapsed += SendBeat;
                     beatTimer.AutoReset = true;
                     beatTimer.Enabled = true;
                     this.isEstablished = true;*/
            oscloop.Send(new OscElement("/state0", "Play"));
            oscloop.Send(new OscElement("/state1", "Play"));

            this.isEstablished = true;
        }

        private void SendBeat(Object source, ElapsedEventArgs e)
        {

            if (totalNumberOfBeatsPlayed % (Math.Round(beatTimes.Count/ ((double) numOfStressedBeats))) == 0)
            {
                beat0.PlayNote(60);
            }
            else
            {
                beat1.PlayNote(60);
            }
            totalNumberOfBeatsPlayed++;
        }
    }
}