using System;
using System.Collections.Generic;
using System.Timers;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    internal class TimeSignature
    {
        private Timer beatTimer;
        private List<double> beatTimes;
        private int numOfStressedBeats;
        private int totalNumberOfBeatsPlayed;

        public TimeSignature(List<double> beatTimes, int numOfStressedBeats)
        {
            this.beatTimes = beatTimes;
            this.numOfStressedBeats = numOfStressedBeats;
            Console.WriteLine("TOTAL BEATS: " + beatTimes.Count);
            Console.WriteLine("STRESSED BEATS: " + numOfStressedBeats);
            totalNumberOfBeatsPlayed = 0;
        }

        public void Establish()
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