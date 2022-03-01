using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Media;
using VRageMath;
using VRageRender;

namespace VRage.UserInterface.Media
{
    public class MyTexture : TextureBase
    {
        private int m_width;
        private int m_height;
        private string m_textureName;
        private bool m_initialized;

        public override int Height
        {
            get
            {
                if (!m_initialized && !string.IsNullOrEmpty(m_textureName))
                {
                    Vector2 textureSize = MyRenderProxy.GetTextureSize(m_textureName);
                    m_width = (int)textureSize.X;
                    m_height = (int)textureSize.Y;
                    if (m_width * m_height != 0)
                        m_initialized = true;
                }

                return m_height;
            }
        }

        public override int Width
        {
            get
            {
                if (!m_initialized && !string.IsNullOrEmpty(m_textureName))
                {
                    Vector2 textureSize = MyRenderProxy.GetTextureSize(m_textureName);
                    m_width = (int)textureSize.X;
                    m_height = (int)textureSize.Y;
                    if (m_width * m_height != 0)
                        m_initialized = true;
                }

                return m_width;
            }
        }

        public override TextureSurfaceFormat Format => throw new NotImplementedException();

        public override void GenerateArrow(ArrowDirection direction, int startX, int lineSize) { }

        public override void GenerateCheckbox() { }

        public override void GenerateLinearGradient(PointF lineStart, PointF lineEnd, Thickness borderThickness, List<GradientStop> sortedStops, GradientSpreadMethod spread, bool isBorder) { }

        public override void GenerateOneToOne() { }

        public override void GenerateSolidColor(Thickness borderThickness, bool isBorder)
        {
            if (Width == 0 || Height == 0)
                return;

            m_textureName = string.Format("EKUI_Texture_{0}_{1}_{2}_{3}_{4}_{5}", Width, Height, borderThickness.Left, borderThickness.Top, borderThickness.Right, borderThickness.Bottom);

            if (Engine.Instance.AssetManager is MyAssetManager assetManager)
            {
                m_initialized = true;
                if (!assetManager.GeneratedTextures.Contains(m_textureName))
                {
                    assetManager.GeneratedTextures.Add(m_textureName);
                    byte[] data = new byte[Width * Height * 4];
                    for (int i = 0; i < data.Length; ++i)
                    {
                        int num1 = i / (Width * 4);
                        int num2 = i / 4 - num1 * Width;
                        data[i] = borderThickness.Top > num1 || borderThickness.Bottom >= (Height - num1) || borderThickness.Left > num2 || borderThickness.Right >= (Width - num2) ? byte.MaxValue : byte.MinValue;
                    }

                    m_initialized = true;
                    MyRenderProxy.CreateGeneratedTexture(m_textureName, Width, Height, data: data, generateMipmaps: false);
                }
            }
        }

        public override object GetNativeTexture() => m_textureName;

        public override void SetColorData(uint[] data) { }

        public override void Dispose()
        {
            if (Engine.Instance.AssetManager is MyAssetManager assetManager && assetManager.GeneratedTextures.Contains(m_textureName))
            {
                MyRenderProxy.DestroyGeneratedTexture(m_textureName);
                assetManager.GeneratedTextures.Remove(m_textureName);
            }
            else
                MyRenderProxy.UnloadTexture(m_textureName);
        }

        public MyTexture(int width, int height) : base(null)
        {
            m_width = width;
            m_height = height;
            if (width == 1 || height == 1)
                m_textureName = "Textures\\Fake.dds";
            else
                m_textureName = "";
        }

        public MyTexture(string textureName) : base(null)
        {
            m_textureName = textureName;
        }
    }
}
