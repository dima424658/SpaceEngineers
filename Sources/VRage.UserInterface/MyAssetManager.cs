using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Media;
using System.Collections.Generic;

namespace VRage.UserInterface
{
    public class MyAssetManager : AssetManager
    {
        public Dictionary<string, FontBase> Fonts { get; private set; } = new Dictionary<string, FontBase>();

        public HashSet<string> GeneratedTextures { get; private set; } = new HashSet<string>();

        public override FontBase LoadFont(object contentManager, string file)
        {
            FontBase fontBase;
            if (!Fonts.TryGetValue(file, out fontBase))
                throw new KeyNotFoundException("Font not found - " + file);

            return fontBase;
        }

        public override SoundBase LoadSound(object contentManager, string file) => Engine.Instance.AudioDevice.CreateSound(file);

        public override TextureBase LoadTexture(object contentManager, string file) => Engine.Instance.Renderer.CreateTexture(file);

        public override EffectBase LoadEffect(object contentManager, string file) => null;

        public void UnloadGeneratedTextures() => GeneratedTextures.Clear();
    }
}
