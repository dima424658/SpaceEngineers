using System.Text;

namespace VRage.Ansel
{
    internal struct Camera
    {
        public Vec3 position;
        public Quat rotation;
        public float fov;
        public float projectionOffsetX;
        public float projectionOffsetY;

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Camera:");
            stringBuilder.Append("  Pos: " + position);
            stringBuilder.Append("  Rot: " + rotation.ToString());
            stringBuilder.Append("  FOV:" + fov.ToString("N2"));
            stringBuilder.Append("  offsets: (" + projectionOffsetX.ToString("N3") + ", " + projectionOffsetY.ToString("N3") + ")");

            return stringBuilder.ToString();
        }
    }
}
