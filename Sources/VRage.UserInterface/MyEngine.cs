using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Input;
using EmptyKeys.UserInterface.Renderers;
using EmptyKeys.UserInterface.Media;
using VRage.UserInterface.Input;
using VRage.UserInterface.Media;
using VRage.UserInterface.Renderers;

namespace VRage.UserInterface
{
    public class MyEngine : Engine
    {
        private Renderer m_renderer;
        private InputDeviceBase m_inputDevice = new MyInputDevice();
        private AssetManager m_assetmanager = new MyAssetManager();
        private AudioDevice m_audioDevice = new MyAudioDevice();

        public override AssetManager AssetManager => m_assetmanager;

        public override AudioDevice AudioDevice => m_audioDevice;

        public override InputDeviceBase InputDevice => m_inputDevice;

        public override Renderer Renderer => m_renderer;

        public MyEngine() => m_renderer = new MyRenderer();
    }
}
