using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Media;
using System.Text;
using VRageMath;

namespace VRage.UserInterface.Media
{
    public class MyFont : FontBase
    {
        private VRageRender.MyFont? m_font;
        private int m_fontIndex;

        public static float GlobalFontScale { get; set; } = 1f;

        public override char? DefaultCharacter => new char?(' ');

        public override object GetNativeFont() => (object)m_fontIndex;

        public override int LineSpacing => m_font.LineHeight;

        public override FontEffectType EffectType => FontEffectType.None;

        public override float Spacing
        {
            get => m_font.Spacing;
            set => m_font.Spacing = (int)value;
        }

        public float Scale { get; set; }

        public MyFont(object nativeFont, int index, float scale)
          : base(nativeFont)
        {
            m_font = nativeFont as VRageRender.MyFont;
            m_fontIndex = index;
            Scale = scale;
        }

        public override Size MeasureString(StringBuilder text, float dpiScaleX, float dpiScaleY)
        {
            Vector2 result = m_font.MeasureString(text, (float)(GlobalFontScale * Scale * (1.0f / dpiScaleX)));
            return new Size(result.X, result.Y);
        }

        public override Size MeasureString(string text, float dpiScaleX, float dpiScaleY)
        {
            Vector2 result = m_font.MeasureString(text, (float)(GlobalFontScale * Scale * (1.0f / dpiScaleX)));
            return new Size(result.X, result.Y);
        }
    }
}
