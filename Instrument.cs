using System;
using Ventuz.OSC;

namespace Instrumovement
{
    class Instrument
    {

        /// <summary>
        /// UDP Writer that writes messages to the host & port that Ableton + Max for Live is listening on
        /// </summary>
        private UdpWriter osc;

        public void Loop()
        {
            OscElement loop = new OscElement("/" + this.name + "loop", 1);
            osc.Send(loop);
        }

        private int midiChannel = 1;

        private string name;

        public Instrument(string name, string oscHost, int oscPort)
        {
            this.name = name;
            this.osc = new UdpWriter(oscHost, oscPort);
        }

        public void PlayNote(int pitch, int velocity = 127, int duration = 500, int sustain = 0)
        {
            
            Console.WriteLine("Playing: " + this.name + 
                              " pitch: " + pitch + 
                              " velocity: " + velocity + 
                              " duration: " + duration + 
                              " sustain: " + sustain);
            this.osc.Send(new OscElement("/" + this.name + "start", pitch, velocity, duration, midiChannel, sustain));
        }
        
        public void StopNote()
        {
            Console.WriteLine("Stoping: " + this.name);
            this.osc.Send(new OscElement("/" + this.name + "stop", 1));
        }
    }
}