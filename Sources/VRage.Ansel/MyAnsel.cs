using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VRage.Ansel
{
    public sealed class MyAnsel : IAnsel
    {
        private bool m_enableAnselWithSprites;
        private Mutex m_anselMutex;
        private CaptureType m_captureType;
        private UserControlCallback m_visibleOverlayCallback;
        private static int m_displayCounterForCursor;
        private readonly StopSessionCallback m_stopSessionDelegate;
        private readonly StopCaptureCallback m_stopCaptureDelegate;
        private readonly StartCaptureCallback m_startCaptureDelegate;
        private readonly StartSessionCallback m_startSessionDelegate;
        private MyAnselCamera m_camera;
        public IntPtr WindowHandle;

        [DllImport("user32.dll")]
        public static extern int ShowCursor(bool bVisible);

        public bool IsSessionEnabled { get; set; }

        public bool IsGamePausable { get; set; }

        public bool IsOverlayEnabled { get; private set; }

        public bool IsSessionRunning { get; private set; }

        public bool IsCaptureRunning { get; private set; }

        public event Action<int> StartCaptureDelegate;

        public event Action StopCaptureDelegate;

        public event Action<bool, bool> WarningMessageDelegate;

        public event Func<bool> IsSpectatorEnabledDelegate;

        public MyAnsel()
        {
            m_stopSessionDelegate = new StopSessionCallback(StopSession);
            m_stopCaptureDelegate = new StopCaptureCallback(StopCapture);
            m_startCaptureDelegate = new StartCaptureCallback(StartCapture);
            m_startSessionDelegate = new StartSessionCallback(StartSession);
        }

        public void SetCamera(ref MyCameraSetup cameraSetup) => m_camera = new MyAnselCamera(cameraSetup.ViewMatrix, cameraSetup.FOV, cameraSetup.AspectRatio, cameraSetup.NearPlane, cameraSetup.FarPlane, cameraSetup.FarPlane, cameraSetup.Position, cameraSetup.ProjectionOffsetX, cameraSetup.ProjectionOffsetY);

        public void GetCamera(out MyCameraSetup cameraSetup)
        {
            m_camera.Update(IsSpectatorEnabledDelegate == null || IsSpectatorEnabledDelegate());
            cameraSetup.ViewMatrix = m_camera.ViewMatrix;
            cameraSetup.Position = m_camera.Position;
            cameraSetup.FarPlane = m_camera.FarFarPlane;
            cameraSetup.FOV = m_camera.FOV;
            cameraSetup.NearPlane = m_camera.NearPlane;
            cameraSetup.ProjectionMatrix = m_camera.ProjectionFarMatrix;
            cameraSetup.ProjectionOffsetX = m_camera.ProjectionOffset.X;
            cameraSetup.ProjectionOffsetY = m_camera.ProjectionOffset.Y;
            cameraSetup.AspectRatio = 0.0f;
        }

        public bool IsMultiresCapturing
        {
            get
            {
                if (!IsCaptureRunning)
                    return false;
                return m_captureType == CaptureType.kCaptureTypeStereo || m_captureType == CaptureType.kCaptureTypeSuperResolution;
            }
        }

        public bool IsInitializedSuccessfuly { get; private set; }

        public bool Is360Capturing
        {
            get
            {
                if (!IsCaptureRunning)
                    return false;
                return m_captureType == CaptureType.kCaptureType360Mono || m_captureType == CaptureType.kCaptureType360Stereo;
            }
        }

        private void VisibleOverlayUserControlCallback(ref UserControlInfo info) => IsOverlayEnabled = Marshal.ReadByte(info.value) > (byte)0;

        private unsafe void AddControls()
        {
            bool isOverlayEnabled = IsOverlayEnabled;
            m_visibleOverlayCallback = new UserControlCallback(VisibleOverlayUserControlCallback);
            UserControlDesc desc = new UserControlDesc()
            {
                labelUtf8 = "Visible overlay",
                callback = m_visibleOverlayCallback,
                info = new UserControlInfo()
                {
                    userControlId = 1,
                    userControlType = UserControlType.kUserControlBoolean,
                    value = new IntPtr(&isOverlayEnabled)
                }
            };
            int num = (int)NativeMethods.addUserControl(ref desc);
        }

        private StartSessionStatus StartSession(
          ref SessionConfiguration settings,
          IntPtr userPointer)
        {
            if (!IsSessionEnabled || !MyVRage.Platform.Windows.Window.IsActive)
                return StartSessionStatus.kDisallowed;

            WarningMessageDelegate?.Invoke(IsGamePausable, IsSpectatorEnabledDelegate == null || IsSpectatorEnabledDelegate());
            m_displayCounterForCursor = ShowCursor(false);
            settings.isRawAllowed = false;
            settings.isPauseAllowed = IsGamePausable;
            settings.isHighresAllowed = true;
            settings.isRotationAllowed = true;
            settings.isTranslationAllowed = true;
            settings.isFovChangeAllowed = true;
            settings.is360MonoAllowed = true;
            settings.is360StereoAllowed = true;
            settings.isRawAllowed = true;
            IsSessionRunning = true;
            return StartSessionStatus.kAllowed;
        }

        private void StopSession(IntPtr userPointer)
        {
            IsSessionRunning = false;
            ShowCursor(true);
        }

        private void StartCapture(ref CaptureType captureType, IntPtr userPointer)
        {
            IsCaptureRunning = true;
            m_captureType = captureType;
            StartCaptureDelegate?.Invoke((int)captureType);
        }

        private void StopCapture(IntPtr userPointer)
        {
            StopCaptureDelegate?.Invoke();
            IsCaptureRunning = false;
        }

        private Configuration GetConfiguration(IntPtr windowHandle) => new Configuration()
        {
            right = new Vec3(1f, 0.0f, 0.0f),
            up = new Vec3(0.0f, 1f, 0.0f),
            forward = new Vec3(0.0f, 0.0f, -1f),
            translationalSpeedInWorldUnitsPerSecond = 5f,
            rotationalSpeedInDegreesPerSecond = 55f,
            captureLatency = 0U,
            captureSettleLatency = 1U,
            metersInWorldUnit = 1f,
            isCameraOffcenteredProjectionSupported = true,
            isCameraRotationSupported = true,
            isCameraTranslationSupported = true,
            isCameraFovSupported = true,
            fovType = FovType.kVerticalFov,
            isFilterOutsideSessionAllowed = false,
            sdkVersion = NativeConstants.ANSEL_SDK_VERSION
        } with
        {
            gameWindowHandle = windowHandle,
            startSessionCallback = m_startSessionDelegate,
            startCaptureCallback = m_startCaptureDelegate,
            stopCaptureCallback = m_stopCaptureDelegate,
            stopSessionCallback = m_stopSessionDelegate
        };

        public void Enable() => m_anselMutex = new Mutex(false, string.Format("NVIDIA/Ansel/{0}", (object)Process.GetCurrentProcess().Id));

        public SetConfigurationStatus Init(bool enableAnselWithSprites)
        {
            IsInitializedSuccessfuly = false;
            m_enableAnselWithSprites = enableAnselWithSprites;

            if (!NativeMethods.isAnselAvailable())
                return SetConfigurationStatus.kSetConfigurationSdkNotLoaded;

            var result = NativeMethods.setConfiguration(GetConfiguration(WindowHandle));
            if (m_enableAnselWithSprites)
                AddControls();

            if (result == SetConfigurationStatus.kSetConfigurationSuccess)
                IsInitializedSuccessfuly = true;

            return result;
        }

        public void StartSession()
        {
            NativeMethods.startSession();
            IsSessionRunning = true;
        }

        public void StopSession()
        {
            IsSessionRunning = false;
            NativeMethods.stopSession();
        }

        public void MarkHdrBufferBind()
        {
            if (IsInitializedSuccessfuly)
                NativeMethods.markHdrBufferBind();
        }

        public void MarkHdrBufferFinished()
        {
            if (IsInitializedSuccessfuly)
                NativeMethods.markHdrBufferFinished();
        }
    }
}
