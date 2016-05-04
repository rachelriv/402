
using Microsoft.Kinect;
using System;
using Instrumovement.BodyTracking;
using System.Collections.Generic;

namespace Instrumovement.Music
{
    class SoundCreator
    {
        public static void Map()
        {
            foreach (Tuple<JointType, JointType> jointPair in MainWindow.steadyMovingJointPairs.Values)
            {
                if (MainWindow.currentBody.HandLeftState == HandState.Closed && MainWindow.currentBody.HandRightState == HandState.Closed)
                {
                    StopAll();
                }
                else {
                    double relativeJointVelocity = VelocityComputer.GetRelativeVelocity(jointPair);
                    if (relativeJointVelocity > 2.0)
                    {
                        PlayNoteForJointPair(jointPair, "Fast");
                    }
                    else if ((MainWindow.steadyMovingJointPairs["shoulderHandLeft"] == jointPair || MainWindow.steadyMovingJointPairs["shoulderHandRight"] == jointPair) && relativeJointVelocity > .2 && MainWindow.currentBody.HandRightState == HandState.Open)
                    {
                        PlayNoteForJointPair(jointPair, "Slow");
                    }
                    else
                    {
                        StopInstrumentFor(jointPair);
                    }
                }
            }
        }




        private static void StopInstrumentFor(Tuple<JointType, JointType> jointPair)
        {
            foreach (Instrument i in MainWindow.instrumentsForJointPair[jointPair].Values)
            {
                i.StopNote();
            }
        }

        private static void StopAll()
        {
            foreach (Dictionary<String, Instrument> instrumentMapping in MainWindow.instrumentsForJointPair.Values)
            {
                foreach (Instrument i in instrumentMapping.Values)
                {
                    i.StopNote();
                }
            }
        }

        private static void PlayNoteForJointPair(Tuple<JointType, JointType> jointPair, String noteType)
        {
            bool noteTypeIsFast = noteType == "Fast";
            String oppositeNoteType = noteTypeIsFast ? "Slow" : "Fast";
            if (!MainWindow.instrumentsForJointPair[jointPair][noteType].isPlaying)
            {
                if (MainWindow.instrumentsForJointPair[jointPair][oppositeNoteType].isPlaying)
                {
                    MainWindow.instrumentsForJointPair[jointPair][oppositeNoteType].StopNote();
                }
                int pitch = noteTypeIsFast ? ((int)(MainWindow.currentBody.Joints[JointType.HandLeft].Position.Y * 30) + 80) : 60;
                int sustain = noteTypeIsFast ? 0 : 1;
                MainWindow.instrumentsForJointPair[jointPair][noteType].PlayNote(pitch, 127, 50, sustain);
            }
        }

    }
}