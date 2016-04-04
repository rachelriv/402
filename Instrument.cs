using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ventuz.OSC;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    class Instrument
    {
        public string name;
        private Boolean sustain;

        public Instrument(string name)
        {
            this.name = name;
        }

        public Instrument()
        {
        }

        public void PlayNote(int pitch, int velocity = 127, int duration = 500, int midiChannel = 1)
        {

            Console.WriteLine("Playing: " + this.name + " " + pitch + " " + velocity + " " + duration + " " + midiChannel);
            OscElement elem = new OscElement("/" + this.name, pitch, velocity, duration, midiChannel);
            MainWindow.osc.Send(elem);
        }
    }
}