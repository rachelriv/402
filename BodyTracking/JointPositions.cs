using Microsoft.Kinect;
using System;
using System.Collections.Generic;

namespace Instrumovement.BodyTracking
{
    public class JointPositions
    {
        private Dictionary<JointType, List<TimedPosition>> previousPositionsOf;

        public JointPositions()
        {
            previousPositionsOf = new Dictionary<JointType, List<TimedPosition>>();
        }

        private const int MAX_NUMBER_OF_POSITIONS = 5;

        public void AddPosition(JointType joint, TimedPosition timedPosition)
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