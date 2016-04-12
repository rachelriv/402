using System;
using Ventuz.OSC;

namespace Instrumovement
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
            if (name == "beat")
            {
                MainWindow.oscBeat.Send(elem);
            } else
            {
                MainWindow.osc.Send(elem);
            }
        }
        
        public void StopNote()
        {
            OscElement elem = new OscElement("/stopinstr0", 1);
            MainWindow.osc.Send(elem);
        }
    }
}