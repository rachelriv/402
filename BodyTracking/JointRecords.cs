using Microsoft.Kinect;
using System;
using System.Collections.Generic;

namespace Instrumovement.BodyTracking
{
    /// <summary>
    /// Frames of previous joint positions and their corresponding timestamps
    /// </summary>
    public class JointRecords
    {
        /// <summary>
        /// A mapping of joint types to a list of previous timed positions of that joint type
        /// </summary>
        private Dictionary<JointType, List<TimedPosition>> previousPositionsOf;

        /// <summary>
        /// Max number of positions we keep at any given time (bounded queue)
        /// </summary>
        private const int MAX_NUMBER_OF_POSITIONS = 5;

        public JointRecords()
        {
            previousPositionsOf = new Dictionary<JointType, List<TimedPosition>>();
        }

        private void AddPosition(JointType joint, TimedPosition timedPosition)
        {
            if (previousPositionsOf.ContainsKey(joint))
            {
                while (previousPositionsOf[joint].Count >= MAX_NUMBER_OF_POSITIONS)
                {
                    previousPositionsOf[joint].RemoveAt(0);
                }
                previousPositionsOf[joint].Add(timedPosition);

            } else
            {
                List<TimedPosition> timedPositions = new List<TimedPosition>();
                timedPositions.Add(timedPosition);
                previousPositionsOf.Add(joint, timedPositions);
            }

        }

        public void AddRecordForEachJoint(double currentTime)
        {
            foreach (JointType jointType in MainWindow.currentBody.Joints.Keys)
            {
                TimedPosition timedPositionOfJoint = new TimedPosition(currentTime, CopyPosition(MainWindow.currentBody.Joints[jointType].Position));
                this.AddPosition(jointType, timedPositionOfJoint);
            }
        }

        private static CameraSpacePoint CopyPosition(CameraSpacePoint position)
        {
            CameraSpacePoint result = new CameraSpacePoint();
            result.X = position.X;
            result.Y = position.Y;
            result.Z = position.Z;
            return result;
        }

        public bool PositionExistsAt(JointType joint, int n)
        {
            return (previousPositionsOf.ContainsKey(joint)) &&
                   (n <= previousPositionsOf[joint].Count - 1) &&
                   (n >= 0) &&
                   (previousPositionsOf[joint][previousPositionsOf[joint].Count - n - 1] != null);
        }

        public TimedPosition GetNthMostRecentPosition(JointType joint, int n)
        {
            if (n > previousPositionsOf[joint].Count - 1 || n < 0)
            {
                return null;
            }
            return previousPositionsOf[joint][previousPositionsOf[joint].Count - n - 1];
        }

        public double MillisBetweenPositions(JointType joint, int first, int second)
        {
            return previousPositionsOf[joint][previousPositionsOf[joint].Count - second - 1].timestamp -
                 previousPositionsOf[joint][previousPositionsOf[joint].Count - first - 1].timestamp;
        }

    }
}