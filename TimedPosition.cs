using Microsoft.Kinect;

namespace Instrumovement
{
    public class TimedPosition
    {
        public double timestamp;
        public CameraSpacePoint position;

        public TimedPosition(double timestamp, CameraSpacePoint position)
        {
            this.timestamp = timestamp;
            this.position = position;
        }

    }
}