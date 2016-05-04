
using Microsoft.Kinect;
using System;
using Instrumovement.BodyTracking;
using System.Collections.Generic;

namespace Instrumovement.Music
{
    class SoundCreator
    {
        // plays fast note if relative velocity (in meters) is greater than this threshold
        private const double FAST_NOTE_MIN_THRESHOLD = 2.0;

        // plays (and sustains) slow note if relative velocity (in meters) is greater than this threshold
        // but less than the minimum threshold for fast notes
        private const double SLOW_NOTE_MIN_THRESHOLD = .2;

        // pitch range we want our notes to fall in
        private const int PITCH_RANGE = 30;

        // minimum pitch of range
        private const int MIN_PITCH = 50;

        // default pitch for when the pitch of the note is NOT a function of a joint's position along the Y-axis
        private const int DEFAULT_PITCH = 60;

        // default velocity of all notes played
        private const int DEFAULT_VELOCITY = 127;

        // default duration of all notes played
        private const int DEFAULT_DURATION = 50;


        public static void Create()
        {
            foreach (Tuple<JointType, JointType> jointPair in MainWindow.steadyMovingJointPairs.Values)
            {
                if (MainWindow.currentBody.HandLeftState == HandState.Closed && MainWindow.currentBody.HandRightState == HandState.Closed)
                {
                    StopAll();
                }
                else {
                    double relativeJointVelocity = VelocityComputer.GetRelativeVelocity(jointPair);
                    if (relativeJointVelocity > FAST_NOTE_MIN_THRESHOLD)
                    {
                        PlayNoteForJointPair(jointPair, "Fast");
                    }
                    else if (isShoulderHandPair(jointPair)  && relativeJointVelocity > SLOW_NOTE_MIN_THRESHOLD)
                    {
                        // only play slow notes for arm movements
                        // (becomes cacophony of sounds if leg movements are included)
                        PlayNoteForJointPair(jointPair, "Slow");
                    }
                    else
                    {
                        StopInstrumentFor(jointPair);
                    }
                }
            }
        }

        private static bool isShoulderHandPair(Tuple<JointType, JointType> jointPair)
        {
            return MainWindow.steadyMovingJointPairs["shoulderHandLeft"] == jointPair || 
                   MainWindow.steadyMovingJointPairs["shoulderHandRight"] == jointPair;
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
                // the pitch for a fast note is dependent on the Y position of the moving joint
                int pitch = noteTypeIsFast ? ConvertYPositionToPitch(jointPair.Item2) : DEFAULT_PITCH;
              
                // the note will be sustained if slow note, and not sustain if fast note 
                int sustain = noteTypeIsFast ? 0 : 1;

                MainWindow.instrumentsForJointPair[jointPair][noteType].PlayNote(pitch, DEFAULT_VELOCITY, DEFAULT_DURATION, sustain);
            }
        }


        /// <summary>
        /// Converts Y position of moving joint (value between 0 and 1) to an int denoting pitch (value between 0 and 127)
        /// </summary>
        /// <param name="movingJoint"></param>
        /// <returns></returns>
        private static int ConvertYPositionToPitch(JointType movingJoint)
        {
            return ((int)(MainWindow.currentBody.Joints[JointType.HandLeft].Position.Y * PITCH_RANGE) + PITCH_RANGE + MIN_PITCH);
        }
    }
}