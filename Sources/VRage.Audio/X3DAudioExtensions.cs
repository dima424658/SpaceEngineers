using VRage.Data.Audio;
using VRageMath;
using SharpDX.X3DAudio;

namespace VRage.Audio.X3DAudio
{
    public static class X3DAudioExtensions
    {
        /// <summary>
        /// Sets default values for emitter, makes it valid
        /// </summary>
        internal static void SetDefaultValues(this Emitter emitter)
        {
            emitter.Position = SharpDX.Vector3.Zero;
            emitter.Velocity = SharpDX.Vector3.Zero;
            emitter.OrientFront = SharpDX.Vector3.UnitZ;
            emitter.OrientTop = SharpDX.Vector3.UnitY;
            emitter.ChannelCount = 1;
            emitter.CurveDistanceScaler = float.MinValue;

            emitter.Cone = null;
        }

        /// <summary>
        /// Sets default values for listener, makes it valid
        /// </summary>
        internal static void SetDefaultValues(this Listener listener)
        {
            listener.Position = SharpDX.Vector3.Zero;
            listener.Velocity = SharpDX.Vector3.Zero;
            listener.OrientFront = SharpDX.Vector3.UnitZ;
            listener.OrientTop = SharpDX.Vector3.UnitY;
        }

        /// <summary>
        /// Updates values of omnidirectional emitter.
        /// Omnidirectional means it's same for all directions. There's no Cone and Front/Top vectors are not used.
        /// </summary>
        internal static void UpdateValuesOmni(this Emitter emitter, Vector3 position, Vector3 velocity, MySoundData cue, int channelsCount, float? customMaxDistance)
        {
            emitter.Position = new SharpDX.Vector3(position.X, position.Y, position.Z);
            emitter.Velocity = new SharpDX.Vector3(velocity.X, velocity.Y, velocity.Z);

            float maxDistance = customMaxDistance.HasValue ? customMaxDistance.Value : cue.MaxDistance;
            emitter.DopplerScaler = 1f;
            emitter.CurveDistanceScaler = maxDistance;
            emitter.VolumeCurve = MyDistanceCurves.Curves[(int)cue.VolumeCurve].Points;

            emitter.InnerRadius = (channelsCount > 2) ? maxDistance : 0f;
            emitter.InnerRadiusAngle = (channelsCount > 2) ? 0.5f * SharpDX.AngleSingle.RightAngle.Radians : 0f;
        }

        internal static void UpdateValuesOmni(this Emitter emitter, Vector3 position, Vector3 velocity, float maxDistance, int channelsCount, MyCurveType volumeCurve)
        {
            emitter.Position = new SharpDX.Vector3(position.X, position.Y, position.Z);
            emitter.Velocity = new SharpDX.Vector3(velocity.X, velocity.Y, velocity.Z);

            emitter.DopplerScaler = 1f;
            emitter.CurveDistanceScaler = maxDistance;
            emitter.VolumeCurve = MyDistanceCurves.Curves[(int)volumeCurve].Points;

            emitter.InnerRadius = (channelsCount > 2) ? maxDistance : 0f;
            emitter.InnerRadiusAngle = (channelsCount > 2) ? 0.5f * SharpDX.AngleSingle.RightAngle.Radians : 0f;
        }
    }
}