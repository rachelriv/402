using Microsoft.Kinect;
using System;
using System.Collections.Generic;

namespace Instrumovement.Config
{
    internal class JointPairMappings
    {
        public static void Initialize()
        {
            MainWindow.steadyMovingJointPairs = new Dictionary<string, Tuple<JointType, JointType>>();

            MainWindow.steadyMovingJointPairs.Add("shoulderHandLeft", new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.HandLeft));
            MainWindow.steadyMovingJointPairs.Add("shoulderHandRight", new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.HandRight));
            MainWindow.steadyMovingJointPairs.Add("hipKneeLeft", new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            MainWindow.steadyMovingJointPairs.Add("hipKneeRight", new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
        }
    }
}