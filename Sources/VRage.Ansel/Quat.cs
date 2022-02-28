using System.Text;

namespace VRage.Ansel
{
  internal struct Quat
  {
    public float x, y, z, w;

    public override string ToString()
    {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("[" + x.ToString("N2"));
      stringBuilder.Append("," + y.ToString("N2"));
      stringBuilder.Append("," + z.ToString("N2"));
      stringBuilder.Append("," + w.ToString("N2") + "]");

      return stringBuilder.ToString();
    }
  }
}
