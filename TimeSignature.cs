using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Timers;

namespace Instrumovement
{
    internal class TimeSignature
    {
        private Timer beatTimer;
        private List<double> beatTimes;
        private int numOfStressedBeats;
        public bool isEstablished;
        private int totalNumberOfBeatsPlayed;
        private const int MINIMUM_NUM_OF_STRESSED_BEATS = 3;

        private bool IsBeat()
        {
            return MainWindow.currentBody.HandLeftState == HandState.Open
                   && MainWindow.currentBody.HandRightState == HandState.Closed
                   && MainWindow.lastHandLeftState == HandState.Closed
                   && MainWindow.lastHandRightState == HandState.Closed;
        }

        private bool IsStressedBeat()
        {
            return MainWindow.currentBody.HandLeftState == HandState.Closed
                   && MainWindow.currentBody.HandRightState == HandState.Open
                   && MainWindow.lastHandLeftState == HandState.Closed
                   && MainWindow.lastHandRightState == HandState.Closed;
        }


        public void CheckForBeats(double timeInMilliseconds)
        {
            if (IsStressedBeat())
            {
                Console.WriteLine("Stressed beat: " + numOfStressedBeats);
                beatTimes.Add(timeInMilliseconds);
                numOfStressedBeats++;
            }
            else if (IsBeat())
            {
                Console.WriteLine("Regular beat: " + beatTimes.Count);
                beatTimes.Add(timeInMilliseconds);
            }

            if (numOfStressedBeats > MINIMUM_NUM_OF_STRESSED_BEATS)
            {
                Console.WriteLine("Establishing time signature");
                numOfStressedBeats--;
                Establish();
            }
        }

        public TimeSignature()
        {
            this.beatTimes = new List<double>();
            this.numOfStressedBeats = 0;
            this.isEstablished = false;
            this.totalNumberOfBeatsPlayed = 0;
        }

        private void Establish()
        {
            double sumOfBeatTimeDifferences = 0;
            for (int i = 0; i < beatTimes.Count - 1; i++)
            {
                sumOfBeatTimeDifferences += beatTimes[i + 1] - beatTimes[i];
            }
            beatTimer = new Timer(sumOfBeatTimeDifferences / ((double)beatTimes.Count));
            beatTimer.Elapsed += SendBeat;
            beatTimer.AutoReset = true;
            beatTimer.Enabled = true;
            this.isEstablished = true;
        }

        private void SendBeat(Object source, ElapsedEventArgs e)
        {
            Instrument beatInstrument = new Instrument("beat");
            if (totalNumberOfBeatsPlayed % (Math.Round(beatTimes.Count/ ((double) numOfStressedBeats))) == 0)
            {
                beatInstrument.PlayNote(10);
            }
            else
            {
                beatInstrument.PlayNote(40);
            }
            totalNumberOfBeatsPlayed++;
        }
    }
}