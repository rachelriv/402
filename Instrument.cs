using System;
using Ventuz.OSC;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    class Instrument
    {
        public string name;

        public Instrument(string name)
        {
            this.name = name;
        }

        public Instrument()
        {
        }

        public void PlayNote(int pitch, int velocity = 127, int duration = 500, int midiChannel = 1, int sustain = 0)
        {

            Console.WriteLine("Playing: " + this.name + " " + pitch + " " + velocity + " " + duration + " " + midiChannel + " " + sustain);
            OscElement elem = new OscElement("/" + this.name, pitch, velocity, duration, midiChannel, sustain);
            MainWindow.osc.Send(elem);
        }
    }
}