using System.Runtime.InteropServices;

namespace VRage.Ansel
{
    internal class NativeMethods
    {
        [DllImport("AnselSDK64")]
        public static extern void updateCamera(ref Camera camera);

        [DllImport("AnselSDK64")]
        public static extern void quaternionToRotationMatrixVectors(ref Quat q, ref Vec3 right, ref Vec3 up, ref Vec3 forward);

        [DllImport("AnselSDK64")]
        public static extern void rotationMatrixVectorsToQuaternion(ref Vec3 right, ref Vec3 up, ref Vec3 forward, ref Quat q);

        [DllImport("AnselSDK64")]
        public static extern void startSession();

        [DllImport("AnselSDK64")]
        public static extern void stopSession();

        [DllImport("AnselSDK64")]
        public static extern SetConfigurationStatus setConfiguration(Configuration cfg);

        [DllImport("AnselSDK64")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool isAnselAvailable();

        [DllImport("AnselSDK64")]
        public static extern UserControlStatus addUserControl(ref UserControlDesc desc);

        [DllImport("AnselSDK64")]
        public static extern UserControlStatus setUserControlLabelLocalization(uint userControlId, string lang, string labelUtf8);

        [DllImport("AnselSDK64")]
        public static extern UserControlStatus removeUserControl(uint userControlId);

        [DllImport("AnselSDK64")]
        public static extern UserControlStatus getUserControlValue(uint userControlId, IntPtr value);

        [DllImport("AnselSDK64")]
        public static extern void markHdrBufferBind(HintType hintType = HintType.kHintTypePreBind, ulong threadId = 18446744073709551615);

        [DllImport("AnselSDK64")]
        public static extern void markHdrBufferFinished(ulong threadId = 18446744073709551615);
    }
}
