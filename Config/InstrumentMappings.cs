using Microsoft.Kinect;
using System;
using System.Collections.Generic;

namespace Instrumovement.Config
{
    internal class InstrumentMappings
    {
        public static void Initialize()
        {
            MainWindow.instrumentsForJointPair = new Dictionary<Tuple<JointType, JointType>, Dictionary<String, Instrument>>();

            Dictionary<string, Instrument> instrumentsForShoulderHandLeft = new Dictionary<string, Instrument>();
            instrumentsForShoulderHandLeft.Add("Slow", new Instrument("instr0"));
            instrumentsForShoulderHandLeft.Add("Fast", new Instrument("instr1"));
            MainWindow.instrumentsForJointPair.Add(MainWindow.steadyMovingJointPairs["shoulderHandLeft"], instrumentsForShoulderHandLeft);

            Dictionary<string, Instrument> instrumentsForShoulderHandRight = new Dictionary<string, Instrument>();
            instrumentsForShoulderHandRight.Add("Slow", new Instrument("instr2"));
            instrumentsForShoulderHandRight.Add("Fast", new Instrument("instr3"));
            MainWindow.instrumentsForJointPair.Add(MainWindow.steadyMovingJointPairs["shoulderHandRight"], instrumentsForShoulderHandRight);

            Dictionary<string, Instrument> instrumentsForHipKneeLeft = new Dictionary<string, Instrument>();
            instrumentsForHipKneeLeft.Add("Slow", new Instrument("instr4"));
            instrumentsForHipKneeLeft.Add("Fast", new Instrument("instr5"));
            MainWindow.instrumentsForJointPair.Add(MainWindow.steadyMovingJointPairs["hipKneeLeft"], instrumentsForHipKneeLeft);

            Dictionary<string, Instrument> instrumentsForHipKneeRight = new Dictionary<string, Instrument>();
            instrumentsForHipKneeRight.Add("Slow", new Instrument("instr6"));
            instrumentsForHipKneeRight.Add("Fast", new Instrument("instr7"));
            MainWindow.instrumentsForJointPair.Add(MainWindow.steadyMovingJointPairs["hipKneeRight"], instrumentsForHipKneeRight);
        }
    }
}