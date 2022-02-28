using System.Text;

namespace VRage.Ansel
{
    internal struct Vec3
    {
        public float x, y, z;

        public Vec3(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("[" + x.ToString("N2"));
            stringBuilder.Append("," + y.ToString("N2"));
            stringBuilder.Append("," + z.ToString("N2") + "]");

            return stringBuilder.ToString();
        }
    }
}
