using System;
using Ventuz.OSC;

namespace Instrumovement
{
    public class Instrument
    {
        
        /// <summary>
        /// UDP Writer that writes messages to the host & port that Ableton + Max for Live is listening on
        /// </summary>
        public UdpWriter osc;


        public void AlterPitch(int pitch)
        {
            this.osc.Send(new OscElement("/" + this.name + "pitch", pitch));
        }

        private int midiChannel = 1;

        private string name;

        public Instrument(string name, string oscHost = "127.0.0.1", int oscPort = 22345)
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
            this.osc.Send(new OscElement("/" + this.name + "start", pitch, velocity, midiChannel, duration, sustain));
        }
        
        public void StopNote()
        {
            Console.WriteLine("Stoping: " + this.name);
            this.osc.Send(new OscElement("/" + this.name + "stop", 1));
        }
    }
}