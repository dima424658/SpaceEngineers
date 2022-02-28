using System.Runtime.InteropServices;

namespace VRage.Ansel
{
    internal struct Configuration
    {
        public Vec3 right;
        public Vec3 up;
        public Vec3 forward;
        public float translationalSpeedInWorldUnitsPerSecond;
        public float rotationalSpeedInDegreesPerSecond;
        public uint captureLatency;
        public uint captureSettleLatency;
        public float metersInWorldUnit;

        [MarshalAs(UnmanagedType.I1)]
        public bool isCameraOffcenteredProjectionSupported;

        [MarshalAs(UnmanagedType.I1)]
        public bool isCameraTranslationSupported;

        [MarshalAs(UnmanagedType.I1)]
        public bool isCameraRotationSupported;

        [MarshalAs(UnmanagedType.I1)]
        public bool isCameraFovSupported;

        [MarshalAs(UnmanagedType.LPStr)]
        public string unused1;
        public FovType fovType;
        public IntPtr userPointer;
        public IntPtr gameWindowHandle;
        public StartSessionCallback startSessionCallback;
        public StopSessionCallback stopSessionCallback;
        public StartCaptureCallback startCaptureCallback;
        public StopCaptureCallback stopCaptureCallback;
        public bool isFilterOutsideSessionAllowed;
        public bool unused2;
        public ulong sdkVersion;
    }
}
