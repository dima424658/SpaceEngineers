using System.Runtime.InteropServices;

namespace VRage.Ansel
{
    internal struct SessionConfiguration
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool isTranslationAllowed;

        [MarshalAs(UnmanagedType.I1)]
        public bool isRotationAllowed;

        [MarshalAs(UnmanagedType.I1)]
        public bool isFovChangeAllowed;

        [MarshalAs(UnmanagedType.I1)]
        public bool isPauseAllowed;

        [MarshalAs(UnmanagedType.I1)]
        public bool isHighresAllowed;

        [MarshalAs(UnmanagedType.I1)]
        public bool is360MonoAllowed;

        [MarshalAs(UnmanagedType.I1)]
        public bool is360StereoAllowed;

        [MarshalAs(UnmanagedType.I1)]
        public bool isRawAllowed;

        public float translationalSpeedInWorldUnitsPerSecond;
        public float rotationalSpeedInDegreesPerSecond;

        public void setDefaults()
        {
            isTranslationAllowed = true;
            isRotationAllowed = true;
            isFovChangeAllowed = true;
            isPauseAllowed = true;
            isHighresAllowed = true;
            is360MonoAllowed = true;
            is360StereoAllowed = true;
            isRawAllowed = true;
        }
    }
}
