using ProtoBuf;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace VRageMath
{
    /// <summary>Defines a matrix.</summary>
    [ProtoContract, Serializable, StructLayout(LayoutKind.Explicit)]
    public struct Matrix3x3 : IEquatable<Matrix3x3>
    {
        public static Matrix3x3 Identity = new Matrix3x3(
            1f, 0.0f, 0.0f,
            0.0f, 1f, 0.0f,
            0.0f, 0.0f, 1f);

        public static Matrix3x3 Zero = new Matrix3x3(
            0.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.0f);

        /// <summary>Matrix3x3 values</summary>
        [FieldOffset(0)]
        private F9 M;
        /// <summary>Value at row 1 column 1 of the matrix.</summary>
        [ProtoMember(1), FieldOffset(0)]
        public float M11;
        /// <summary>Value at row 1 column 2 of the matrix.</summary>
        [ProtoMember(4), FieldOffset(4)]
        public float M12;
        /// <summary>Value at row 1 column 3 of the matrix.</summary>
        [ProtoMember(7), FieldOffset(8)]
        public float M13;
        /// <summary>Value at row 2 column 1 of the matrix.</summary>
        [ProtoMember(10), FieldOffset(12)]
        public float M21;
        /// <summary>Value at row 2 column 2 of the matrix.</summary>
        [ProtoMember(13), FieldOffset(16)]
        public float M22;
        /// <summary>Value at row 2 column 3 of the matrix.</summary>
        [ProtoMember(16), FieldOffset(20)]
        public float M23;
        /// <summary>Value at row 3 column 1 of the matrix.</summary>
        [ProtoMember(19), FieldOffset(24)]
        public float M31;
        /// <summary>Value at row 3 column 2 of the matrix.</summary>
        [ProtoMember(22), FieldOffset(28)]
        public float M32;
        /// <summary>Value at row 3 column 3 of the matrix.</summary>
        [ProtoMember(25), FieldOffset(32)]
        public float M33;

        /// <summary>Gets and sets the up vector of the Matrix3x3.</summary>
        public Vector3 Up
        {
            get => new Vector3(M21, M22, M23);
            set
            {
                M21 = value.X; M22 = value.Y; M23 = value.Z;
            }
        }

        /// <summary>Gets and sets the down vector of the Matrix3x3.</summary>
        public Vector3 Down
        {
            get => new Vector3(-M21, -M22, -M23);
            set
            {
                M21 = -value.X; M22 = -value.Y; M23 = -value.Z;
            }
        }

        /// <summary>Gets and sets the right vector of the Matrix3x3.</summary>
        public Vector3 Right
        {
            get => new Vector3(M11, M12, M13);
            set
            {
                M11 = value.X; M12 = value.Y; M13 = value.Z;
            }
        }

        public Vector3 Col0
        {
            get => new Vector3(M11, M21, M31);
        }

        public Vector3 Col1
        {
            get => new Vector3(M12, M22, M32);
        }

        public Vector3 Col2
        {
            get => new Vector3(M13, M23, M33);
        }

        /// <summary>Gets and sets the left vector of the Matrix3x3.</summary>
        public Vector3 Left
        {
            get => new Vector3(-M11, -M12, -M13);
            set
            {
                M11 = -value.X; M12 = -value.Y; M13 = -value.Z;
            }
        }

        /// <summary>Gets and sets the forward vector of the Matrix3x3.</summary>
        public Vector3 Forward
        {
            get => new Vector3(-M31, -M32, -M33);
            set
            {
                M31 = -value.X; M32 = -value.Y; M33 = -value.Z;
            }
        }

        /// <summary>Gets and sets the backward vector of the Matrix3x3.</summary>
        public Vector3 Backward
        {
            get => new Vector3(M31, M32, M33);
            set
            {
                M31 = value.X; M32 = value.Y; M33 = value.Z;
            }
        }

        /// <summary>
        /// Gets the base vector of the matrix, corresponding to the given direction
        /// </summary>
        public Vector3 GetDirectionVector(Base6Directions.Direction direction)
        {
            switch (direction)
            {
                case Base6Directions.Direction.Forward:
                    return Forward;
                case Base6Directions.Direction.Backward:
                    return Backward;
                case Base6Directions.Direction.Left:
                    return Left;
                case Base6Directions.Direction.Right:
                    return Right;
                case Base6Directions.Direction.Up:
                    return Up;
                case Base6Directions.Direction.Down:
                    return Down;
                default:
                    return Vector3.Zero;
            }
        }

        /// <summary>
        /// Sets the base vector of the matrix, corresponding to the given direction
        /// </summary>
        public void SetDirectionVector(Base6Directions.Direction direction, Vector3 newValue)
        {
            switch (direction)
            {
                case Base6Directions.Direction.Forward:
                    Forward = newValue;
                    break;
                case Base6Directions.Direction.Backward:
                    Backward = newValue;
                    break;
                case Base6Directions.Direction.Left:
                    Left = newValue;
                    break;
                case Base6Directions.Direction.Right:
                    Right = newValue;
                    break;
                case Base6Directions.Direction.Up:
                    Up = newValue;
                    break;
                case Base6Directions.Direction.Down:
                    Down = newValue;
                    break;
            }
        }

        public Base6Directions.Direction GetClosestDirection(Vector3 referenceVector) => GetClosestDirection(ref referenceVector);

        public Base6Directions.Direction GetClosestDirection(ref Vector3 referenceVector)
        {
            var dotRight = Vector3.Dot(referenceVector, Right);
            var dotUp = Vector3.Dot(referenceVector, Up);
            var dotBack = Vector3.Dot(referenceVector, Backward);
            var max = Math.Max(Math.Abs(dotRight), Math.Max(Math.Abs(dotUp), Math.Abs(dotBack)));

            if (max == Math.Abs(dotRight))
                return dotRight > 0 ? Base6Directions.Direction.Right : Base6Directions.Direction.Left;
            else if (max == Math.Abs(dotUp))
                return dotUp > 0 ? Base6Directions.Direction.Up : Base6Directions.Direction.Down;
            else // if(max == Math.Abs(dotBack))
                return dotBack > 0 ? Base6Directions.Direction.Backward : Base6Directions.Direction.Forward;
        }

        public Vector3 Scale
        {
            get => new Vector3(Right.Length(), Up.Length(), Forward.Length());
        }

        /// <summary>
        /// Same result as Matrix3x3.CreateScale(scale) * matrix, but much faster
        /// </summary>
        public static void Rescale(ref Matrix3x3 matrix, float scale)
        {
            matrix.M11 *= scale; matrix.M12 *= scale; matrix.M13 *= scale;
            matrix.M21 *= scale; matrix.M22 *= scale; matrix.M23 *= scale;
            matrix.M31 *= scale; matrix.M32 *= scale; matrix.M33 *= scale;
        }

        /// <summary>
        /// Same result as Matrix3x3.CreateScale(scale) * matrix, but much faster
        /// </summary>
        public static void Rescale(ref Matrix3x3 matrix, ref Vector3 scale)
        {
            matrix.M11 *= scale.X; matrix.M12 *= scale.X; matrix.M13 *= scale.X;
            matrix.M21 *= scale.Y; matrix.M22 *= scale.Y; matrix.M23 *= scale.Y;
            matrix.M31 *= scale.Z; matrix.M32 *= scale.Z; matrix.M33 *= scale.Z;
        }

        public static Matrix3x3 Rescale(Matrix3x3 matrix, float scale)
        {
            Rescale(ref matrix, scale);
            return matrix;
        }

        public static Matrix3x3 Rescale(Matrix3x3 matrix, Vector3 scale)
        {
            Rescale(ref matrix, ref scale);
            return matrix;
        }

        /// <summary>Initializes a new instance of Matrix3x3.</summary>
        /// <param name="m11">Value to initialize m11 to.</param>
        /// <param name="m12">Value to initialize m12 to.</param>
        /// <param name="m13">Value to initialize m13 to.</param>
        /// <param name="m21">Value to initialize m21 to.</param>
        /// <param name="m22">Value to initialize m22 to.</param>
        /// <param name="m23">Value to initialize m23 to.</param>
        /// <param name="m31">Value to initialize m31 to.</param>
        /// <param name="m32">Value to initialize m32 to.</param>
        /// <param name="m33">Value to initialize m33 to.</param>
        public Matrix3x3(
            float m11, float m12, float m13,
            float m21, float m22, float m23,
            float m31, float m32, float m33)
        {
            M11 = m11; M12 = m12; M13 = m13;
            M21 = m21; M22 = m22; M23 = m23;
            M31 = m31; M32 = m32; M33 = m33;
        }

        public Matrix3x3(Matrix3x3 other)
        {
            M11 = other.M11; M12 = other.M12; M13 = other.M13;
            M21 = other.M21; M22 = other.M22; M23 = other.M23;
            M31 = other.M31; M32 = other.M32; M33 = other.M33;
        }

        public Matrix3x3(MatrixD other)
        {
            M11 = (float)other.M11; M12 = (float)other.M12; M13 = (float)other.M13;
            M21 = (float)other.M21; M22 = (float)other.M22; M23 = (float)other.M23;
            M31 = (float)other.M31; M32 = (float)other.M32; M33 = (float)other.M33;
        }

        /// <summary>Creates a scaling Matrix3x3.</summary>
        /// <param name="xScale">Value to scale by on the x-axis.</param>
        /// <param name="yScale">Value to scale by on the y-axis.</param>
        /// <param name="zScale">Value to scale by on the z-axis.</param>
        public static Matrix3x3 CreateScale(float xScale, float yScale, float zScale)
        {
            return new Matrix3x3(
                xScale, 0.0f, 0.0f,
                0.0f, yScale, 0.0f,
                0.0f, 0.0f, zScale
                );
        }

        /// <summary>Creates a scaling Matrix3x3.</summary>
        /// <param name="xScale">Value to scale by on the x-axis.</param>
        /// <param name="yScale">Value to scale by on the y-axis.</param>
        /// <param name="zScale">Value to scale by on the z-axis.</param>
        /// <param name="result">[OutAttribute] The created scaling Matrix3x3.</param>
        public static void CreateScale(float xScale, float yScale, float zScale, out Matrix3x3 result)
        {
            result.M11 = xScale; result.M12 = 0.0f; result.M13 = 0.0f;
            result.M21 = 0.0f; result.M22 = yScale; result.M23 = 0.0f;
            result.M31 = 0.0f; result.M32 = 0.0f; result.M33 = zScale;
        }

        /// <summary>Creates a scaling Matrix3x3.</summary>
        /// <param name="scales">Amounts to scale by on the x, y, and z axes.</param>
        public static Matrix3x3 CreateScale(Vector3 scales)
        {
            return new Matrix3x3(
                scales.X, 0.0f, 0.0f,
                0.0f, scales.Y, 0.0f,
                0.0f, 0.0f, scales.Z
                );
        }

        /// <summary>Creates a scaling Matrix3x3.</summary>
        /// <param name="scales">Amounts to scale by on the x, y, and z axes.</param>
        /// <param name="result">[OutAttribute] The created scaling Matrix3x3.</param>
        public static void CreateScale(ref Vector3 scales, out Matrix3x3 result)
        {
            result.M11 = scales.X; result.M12 = 0.0f; result.M13 = 0.0f;
            result.M21 = 0.0f; result.M22 = scales.Y; result.M23 = 0.0f;
            result.M31 = 0.0f; result.M32 = 0.0f; result.M33 = scales.Z;
        }

        /// <summary>Creates a scaling Matrix3x3.</summary>
        /// <param name="scale">Amount to scale by.</param>
        public static Matrix3x3 CreateScale(float scale)
        {
            return new Matrix3x3(
                scale, 0.0f, 0.0f,
                0.0f, scale, 0.0f,
                0.0f, 0.0f, scale
                );
        }

        /// <summary>Creates a scaling Matrix3x3.</summary>
        /// <param name="scale">Value to scale by.</param>
        /// <param name="result">[OutAttribute] The created scaling Matrix3x3.</param>
        public static void CreateScale(float scale, out Matrix3x3 result)
        {
            result.M11 = scale; result.M12 = 0.0f; result.M13 = 0.0f;
            result.M21 = 0.0f; result.M22 = scale; result.M23 = 0.0f;
            result.M31 = 0.0f; result.M32 = 0.0f; result.M33 = scale;
        }

        /// <summary>
        /// Returns a matrix that can be used to rotate a set of vertices around the x-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the x-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        public static Matrix3x3 CreateRotationX(float radians)
        {
            return new Matrix3x3(
                1f, 0.0f, 0.0f,
                0.0f, MathF.Cos(radians), MathF.Sin(radians),
                0.0f, -MathF.Sin(radians), MathF.Cos(radians)
                );
        }

        /// <summary>
        /// Populates data into a user-specified matrix that can be used to rotate a set of vertices around the x-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the x-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        /// <param name="result">[OutAttribute] The matrix in which to place the calculated data.</param>
        public static void CreateRotationX(float radians, out Matrix3x3 result)
        {
            result.M11 = 1.0f; result.M12 = 0.0f; result.M13 = 0.0f;
            result.M21 = 0.0f; result.M22 = MathF.Cos(radians); result.M23 = MathF.Sin(radians);
            result.M31 = 0.0f; result.M32 = -MathF.Sin(radians); result.M33 = MathF.Cos(radians);
        }

        /// <summary>
        /// Returns a matrix that can be used to rotate a set of vertices around the y-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the y-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        public static Matrix3x3 CreateRotationY(float radians)
        {
            return new Matrix3x3(
                MathF.Cos(radians), 0.0f, -MathF.Sin(radians),
                0.0f, 1f, 0.0f,
                MathF.Sin(radians), 0.0f, MathF.Cos(radians)
                );
        }

        /// <summary>
        /// Populates data into a user-specified matrix that can be used to rotate a set of vertices around the y-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the y-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        /// <param name="result">[OutAttribute] The matrix in which to place the calculated data.</param>
        public static void CreateRotationY(float radians, out Matrix3x3 result)
        {
            result.M11 = MathF.Cos(radians); result.M12 = 0.0f; result.M13 = -MathF.Sin(radians);
            result.M21 = 0.0f; result.M22 = 1f; result.M23 = 0.0f;
            result.M31 = MathF.Sin(radians); result.M32 = 0.0f; result.M33 = MathF.Cos(radians);
        }

        /// <summary>
        /// Returns a matrix that can be used to rotate a set of vertices around the z-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the z-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        public static Matrix3x3 CreateRotationZ(float radians)
        {
            return new Matrix3x3(
                MathF.Cos(radians), MathF.Sin(radians), 0.0f,
                -MathF.Sin(radians), MathF.Cos(radians), 0.0f,
                0.0f, 0.0f, 1f
                );
        }

        /// <summary>
        /// Populates data into a user-specified matrix that can be used to rotate a set of vertices around the z-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, in which to rotate around the z-axis. Note that you can use ToRadians to convert degrees to radians.</param>
        /// <param name="result">[OutAttribute] The rotation matrix.</param>
        public static void CreateRotationZ(float radians, out Matrix3x3 result)
        {
            result.M11 = MathF.Cos(radians); result.M12 = MathF.Sin(radians); result.M13 = 0.0f;
            result.M21 = -MathF.Sin(radians); result.M22 = MathF.Cos(radians); result.M23 = 0.0f;
            result.M31 = 0.0f; result.M32 = 0.0f; result.M33 = 1f;
        }

        /// <summary>
        /// Creates a new Matrix3x3 that rotates around an arbitrary vector.
        /// </summary>
        /// <param name="axis">The axis to rotate around.</param>
        /// <param name="angle">The angle to rotate around the vector.</param>
        public static Matrix3x3 CreateFromAxisAngle(Vector3 axis, float angle)
        {
            float sinus = MathF.Sin(angle), cosinus = MathF.Cos(angle);
            Matrix3x3 fromAxisAngle;

            fromAxisAngle.M11 = axis.X * axis.X * (1 - cosinus) + cosinus;
            fromAxisAngle.M22 = axis.Y * axis.Y * (1 - cosinus) + cosinus;
            fromAxisAngle.M33 = axis.Z * axis.Z * (1 - cosinus) + cosinus;

            fromAxisAngle.M12 = fromAxisAngle.M21 = (1 - cosinus) * axis.X * axis.Y + sinus * axis.Z;
            fromAxisAngle.M13 = fromAxisAngle.M31 = (1 - cosinus) * axis.X * axis.Z - sinus * axis.Y;
            fromAxisAngle.M23 = fromAxisAngle.M32 = (1 - cosinus) * axis.Y * axis.Z + sinus * axis.X;

            return fromAxisAngle;
        }

        /// <summary>
        /// Creates a new Matrix3x3 that rotates around an arbitrary vector.
        /// </summary>
        /// <param name="axis">The axis to rotate around.</param>
        /// <param name="angle">The angle to rotate around the vector.</param>
        /// <param name="result">[OutAttribute] The created Matrix3x3.</param>
        public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Matrix3x3 result)
        {
            float sinus = MathF.Sin(angle), cosinus = MathF.Cos(angle);

            result.M11 = axis.X * axis.X * (1 - cosinus) + cosinus;
            result.M22 = axis.Y * axis.Y * (1 - cosinus) + cosinus;
            result.M33 = axis.Z * axis.Z * (1 - cosinus) + cosinus;

            result.M12 = result.M21 = (1 - cosinus) * axis.X * axis.Y + sinus * axis.Z;
            result.M13 = result.M31 = (1 - cosinus) * axis.X * axis.Z - sinus * axis.Y;
            result.M23 = result.M32 = (1 - cosinus) * axis.Y * axis.Z + sinus * axis.X;
        }

        public static void CreateRotationFromTwoVectors(ref Vector3 fromVector, ref Vector3 toVector, out Matrix3x3 resultMatrix)
        {
            Vector3 normalizedFrom = Vector3.Normalize(fromVector);
            Vector3 normalizedTo = Vector3.Normalize(toVector);

            Vector3 result1 = Vector3.Cross(normalizedFrom, normalizedTo);
            Vector3 result2 = Vector3.Cross(normalizedFrom, result1);

            Matrix3x3 matrix1 = new Matrix3x3(
                normalizedFrom.X, result1.X, result2.X,
                normalizedFrom.Y, result1.Y, result2.Y,
                normalizedFrom.Z, result1.Z, result2.Z);

            Vector3 result3 = Vector3.Cross(normalizedTo, result1);
            Matrix3x3 matrix2 = new Matrix3x3(
                normalizedTo.X, normalizedTo.Y, normalizedTo.Z,
                result1.X, result1.Y, result1.Z,
                result3.X, result3.Y, result3.Z);

            Multiply(ref matrix1, ref matrix2, out resultMatrix);
        }

        /// <summary>Creates a rotation Matrix3x3 from a Quaternion.</summary>
        /// <param name="quaternion">Quaternion to create the Matrix3x3 from.</param>
        public static Matrix3x3 CreateFromQuaternion(Quaternion quaternion)
        {
            float num1 = quaternion.X * quaternion.X;
            float num2 = quaternion.Y * quaternion.Y;
            float num3 = quaternion.Z * quaternion.Z;
            float num4 = quaternion.X * quaternion.Y;
            float num5 = quaternion.Z * quaternion.W;
            float num6 = quaternion.Z * quaternion.X;
            float num7 = quaternion.Y * quaternion.W;
            float num8 = quaternion.Y * quaternion.Z;
            float num9 = quaternion.X * quaternion.W;
            Matrix3x3 fromQuaternion;
            fromQuaternion.M11 = (float)(1.0 - 2.0 * ((double)num2 + (double)num3));
            fromQuaternion.M12 = (float)(2.0 * ((double)num4 + (double)num5));
            fromQuaternion.M13 = (float)(2.0 * ((double)num6 - (double)num7));
            fromQuaternion.M21 = (float)(2.0 * ((double)num4 - (double)num5));
            fromQuaternion.M22 = (float)(1.0 - 2.0 * ((double)num3 + (double)num1));
            fromQuaternion.M23 = (float)(2.0 * ((double)num8 + (double)num9));
            fromQuaternion.M31 = (float)(2.0 * ((double)num6 + (double)num7));
            fromQuaternion.M32 = (float)(2.0 * ((double)num8 - (double)num9));
            fromQuaternion.M33 = (float)(1.0 - 2.0 * ((double)num2 + (double)num1));
            return fromQuaternion;
        }

        /// <summary>Creates a rotation Matrix3x3 from a Quaternion.</summary>
        /// <param name="quaternion">Quaternion to create the Matrix3x3 from.</param>
        /// <param name="result">[OutAttribute] The created Matrix3x3.</param>
        public static void CreateFromQuaternion(ref Quaternion quaternion, out Matrix3x3 result)
        {
            float num1 = quaternion.X * quaternion.X;
            float num2 = quaternion.Y * quaternion.Y;
            float num3 = quaternion.Z * quaternion.Z;
            float num4 = quaternion.X * quaternion.Y;
            float num5 = quaternion.Z * quaternion.W;
            float num6 = quaternion.Z * quaternion.X;
            float num7 = quaternion.Y * quaternion.W;
            float num8 = quaternion.Y * quaternion.Z;
            float num9 = quaternion.X * quaternion.W;
            result.M11 = (float)(1.0 - 2.0 * ((double)num2 + (double)num3));
            result.M12 = (float)(2.0 * ((double)num4 + (double)num5));
            result.M13 = (float)(2.0 * ((double)num6 - (double)num7));
            result.M21 = (float)(2.0 * ((double)num4 - (double)num5));
            result.M22 = (float)(1.0 - 2.0 * ((double)num3 + (double)num1));
            result.M23 = (float)(2.0 * ((double)num8 + (double)num9));
            result.M31 = (float)(2.0 * ((double)num6 + (double)num7));
            result.M32 = (float)(2.0 * ((double)num8 - (double)num9));
            result.M33 = (float)(1.0 - 2.0 * ((double)num2 + (double)num1));
        }

        /// <summary>
        /// Creates a new rotation matrix from a specified yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Angle of rotation, in radians, around the y-axis.</param>
        /// <param name="pitch">Angle of rotation, in radians, around the x-axis.</param>
        /// <param name="roll">Angle of rotation, in radians, around the z-axis.</param>
        public static Matrix3x3 CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            Quaternion result1;
            Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out result1);
            Matrix3x3 result2;
            Matrix3x3.CreateFromQuaternion(ref result1, out result2);
            return result2;
        }

        /// <summary>
        /// Fills in a rotation matrix from a specified yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Angle of rotation, in radians, around the y-axis.</param>
        /// <param name="pitch">Angle of rotation, in radians, around the x-axis.</param>
        /// <param name="roll">Angle of rotation, in radians, around the z-axis.</param>
        /// <param name="result">[OutAttribute] An existing matrix filled in to represent the specified yaw, pitch, and roll.</param>
        public static void CreateFromYawPitchRoll(
          float yaw,
          float pitch,
          float roll,
          out Matrix3x3 result)
        {
            Quaternion result1;
            Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out result1);
            Matrix3x3.CreateFromQuaternion(ref result1, out result);
        }

        /// <summary>
        /// Transforms a Matrix3x3 by applying a Quaternion rotation.
        /// </summary>
        /// <param name="value">The Matrix3x3 to transform.</param>
        /// <param name="rotation">The rotation to apply, expressed as a Quaternion.</param>
        /// <param name="result">[OutAttribute] An existing Matrix3x3 filled in with the result of the transform.</param>
        public static void Transform(
          ref Matrix3x3 value,
          ref Quaternion rotation,
          out Matrix3x3 result)
        {
            float num1 = rotation.X + rotation.X;
            float num2 = rotation.Y + rotation.Y;
            float num3 = rotation.Z + rotation.Z;
            float num4 = rotation.W * num1;
            float num5 = rotation.W * num2;
            float num6 = rotation.W * num3;
            float num7 = rotation.X * num1;
            double num8 = (double)rotation.X * (double)num2;
            float num9 = rotation.X * num3;
            float num10 = rotation.Y * num2;
            float num11 = rotation.Y * num3;
            float num12 = rotation.Z * num3;
            float num13 = 1f - num10 - num12;
            float num14 = (float)num8 - num6;
            float num15 = num9 + num5;
            float num16 = (float)num8 + num6;
            float num17 = 1f - num7 - num12;
            float num18 = num11 - num4;
            float num19 = num9 - num5;
            float num20 = num11 + num4;
            float num21 = 1f - num7 - num10;
            float num22 = (float)((double)value.M11 * (double)num13 + (double)value.M12 * (double)num14 + (double)value.M13 * (double)num15);
            float num23 = (float)((double)value.M11 * (double)num16 + (double)value.M12 * (double)num17 + (double)value.M13 * (double)num18);
            float num24 = (float)((double)value.M11 * (double)num19 + (double)value.M12 * (double)num20 + (double)value.M13 * (double)num21);
            float num25 = (float)((double)value.M21 * (double)num13 + (double)value.M22 * (double)num14 + (double)value.M23 * (double)num15);
            float num26 = (float)((double)value.M21 * (double)num16 + (double)value.M22 * (double)num17 + (double)value.M23 * (double)num18);
            float num27 = (float)((double)value.M21 * (double)num19 + (double)value.M22 * (double)num20 + (double)value.M23 * (double)num21);
            float num28 = (float)((double)value.M31 * (double)num13 + (double)value.M32 * (double)num14 + (double)value.M33 * (double)num15);
            float num29 = (float)((double)value.M31 * (double)num16 + (double)value.M32 * (double)num17 + (double)value.M33 * (double)num18);
            float num30 = (float)((double)value.M31 * (double)num19 + (double)value.M32 * (double)num20 + (double)value.M33 * (double)num21);
            result.M11 = num22;
            result.M12 = num23;
            result.M13 = num24;
            result.M21 = num25;
            result.M22 = num26;
            result.M23 = num27;
            result.M31 = num28;
            result.M32 = num29;
            result.M33 = num30;
        }

        public unsafe Vector3 GetRow(int row)
        {
            if (row < 0 || row > 2)
                throw new ArgumentOutOfRangeException();
            fixed (float* numPtr1 = &M11)
            {
                float* numPtr2 = numPtr1 + row * 3;
                return new Vector3(*numPtr2, numPtr2[1], numPtr2[2]);
            }
        }

        public unsafe void SetRow(int row, Vector3 value)
        {
            if (row < 0 || row > 2)
                throw new ArgumentOutOfRangeException();

            fixed (float* numPtr1 = &M11)
            {
                float* numPtr2 = numPtr1 + row * 3;
                numPtr2[0] = value.X;
                numPtr2[1] = value.Y;
                numPtr2[2] = value.Z;
            }
        }

        public unsafe float this[int row, int column]
        {
            get
            {
                if (row < 0 || row > 2 || column < 0 || column > 2)
                    throw new ArgumentOutOfRangeException();

                fixed (float* numPtr = &M11)
                    return numPtr[row * 3 + column];
            }
            set
            {
                if (row < 0 || row > 2 || column < 0 || column > 2)
                    throw new ArgumentOutOfRangeException();

                fixed (float* numPtr = &M11)
                    numPtr[row * 3 + column] = value;
            }
        }

        /// <summary>
        /// Retrieves a string representation of the current object.
        /// </summary>
        public override string ToString()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return "{ " + string.Format((IFormatProvider)currentCulture, "{{M11:{0} M12:{1} M13:{2}}} ", (object)M11.ToString((IFormatProvider)currentCulture), (object)M12.ToString((IFormatProvider)currentCulture), (object)(M13.ToString((IFormatProvider)currentCulture) + string.Format((IFormatProvider)currentCulture, "{{M21:{0} M22:{1} M23:{2}}} ", (object)M21.ToString((IFormatProvider)currentCulture), (object)M22.ToString((IFormatProvider)currentCulture), (object)(M23.ToString((IFormatProvider)currentCulture) + string.Format((IFormatProvider)currentCulture, "{{M31:{0} M32:{1} M33:{2}}} ", (object)M31.ToString((IFormatProvider)currentCulture), (object)M32.ToString((IFormatProvider)currentCulture), (object)M33.ToString((IFormatProvider)currentCulture)))))) + "}";
        }

        /// <summary>
        /// Determines whether the specified Object is equal to the Matrix3x3.
        /// </summary>
        /// <param name="other">The Object to compare with the current Matrix3x3.</param>
        public bool Equals(Matrix3x3 other) => (double)M11 == (double)other.M11 && (double)M22 == (double)other.M22 && (double)M33 == (double)other.M33 && (double)M12 == (double)other.M12 && (double)M13 == (double)other.M13 && (double)M21 == (double)other.M21 && (double)M23 == (double)other.M23 && (double)M31 == (double)other.M31 && (double)M32 == (double)other.M32;

        /// <summary>Compares just position, forward and up</summary>
        public bool EqualsFast(ref Matrix3x3 other, float epsilon = 0.0001f)
        {
            double num1 = (double)M21 - (double)other.M21;
            float num2 = M22 - other.M22;
            float num3 = M23 - other.M23;
            float num4 = M31 - other.M31;
            float num5 = M32 - other.M32;
            float num6 = M33 - other.M33;
            float num7 = epsilon * epsilon;
            return num1 * num1 + (double)num2 * (double)num2 + (double)num3 * (double)num3 < (double)num7 & (double)num4 * (double)num4 + (double)num5 * (double)num5 + (double)num6 * (double)num6 < (double)num7;
        }

        /// <summary>
        /// Returns a value that indicates whether the current instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">Object with which to make the comparison.</param>
        public override bool Equals(object obj)
        {
            bool flag = false;
            if (obj is Matrix3x3 other)
                flag = Equals(other);
            return flag;
        }

        /// <summary>Gets the hash code of this object.</summary>
        public override int GetHashCode() => M11.GetHashCode() + M12.GetHashCode() + M13.GetHashCode() + M21.GetHashCode() + M22.GetHashCode() + M23.GetHashCode() + M31.GetHashCode() + M32.GetHashCode() + M33.GetHashCode();

        /// <summary>Transposes the rows and columns of a matrix.</summary>
        /// <param name="matrix">Source matrix.</param>
        /// <param name="result">[OutAttribute] Transposed matrix.</param>
        public static void Transpose(ref Matrix3x3 matrix, out Matrix3x3 result)
        {
            float m11 = matrix.M11;
            float m12 = matrix.M12;
            float m13 = matrix.M13;
            float m21 = matrix.M21;
            float m22 = matrix.M22;
            float m23 = matrix.M23;
            float m31 = matrix.M31;
            float m32 = matrix.M32;
            float m33 = matrix.M33;

            result.M11 = m11;
            result.M12 = m21;
            result.M13 = m31;
            result.M21 = m12;
            result.M22 = m22;
            result.M23 = m32;
            result.M31 = m13;
            result.M32 = m23;
            result.M33 = m33;
        }

        public void Transpose()
        {
            float m12 = M12;
            float m13 = M13;
            float m21 = M21;
            float m23 = M23;
            float m31 = M31;
            float m32 = M32;

            M12 = m21;
            M13 = m31;
            M21 = m12;
            M23 = m32;
            M31 = m13;
            M32 = m23;
        }

        public float Determinant() => (float)((double)M11 * ((double)M22 * (double)M33 - (double)M32 * (double)M23) - (double)M12 * ((double)M21 * (double)M33 - (double)M23 * (double)M31) + (double)M13 * ((double)M21 * (double)M32 - (double)M22 * (double)M31));

        /// <summary>Calculates the inverse of a matrix.</summary>
        /// <param name="matrix">The source matrix.</param>
        /// <param name="result">[OutAttribute] The inverse of matrix. The same matrix can be used for both arguments.</param>
        public static void Invert(ref Matrix3x3 matrix, out Matrix3x3 result)
        {
            float num = 1f / matrix.Determinant();
            result.M11 = matrix.M22 * matrix.M33 - matrix.M32 * matrix.M23 * num;
            result.M12 = matrix.M13 * matrix.M32 - matrix.M12 * matrix.M33 * num;
            result.M13 = matrix.M12 * matrix.M23 - matrix.M13 * matrix.M22 * num;
            result.M21 = matrix.M23 * matrix.M31 - matrix.M21 * matrix.M33 * num;
            result.M22 = matrix.M11 * matrix.M33 - matrix.M13 * matrix.M31 * num;
            result.M23 = matrix.M21 * matrix.M13 - matrix.M11 * matrix.M23 * num;
            result.M31 = matrix.M21 * matrix.M32 - matrix.M31 * matrix.M22 * num;
            result.M32 = matrix.M31 * matrix.M12 - matrix.M11 * matrix.M32 * num;
            result.M33 = matrix.M11 * matrix.M22 - matrix.M21 * matrix.M12 * num;
        }

        /// <summary>
        /// Linearly interpolates between the corresponding values of two matrices.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        /// <param name="amount">Interpolation value.</param>
        /// <param name="result">[OutAttribute] Resulting matrix.</param>
        public static void Lerp(ref Matrix3x3 matrix1, ref Matrix3x3 matrix2, float amount, out Matrix3x3 result)
        {
            result.M11 = matrix1.M11 + (matrix2.M11 - matrix1.M11) * amount;
            result.M12 = matrix1.M12 + (matrix2.M12 - matrix1.M12) * amount;
            result.M13 = matrix1.M13 + (matrix2.M13 - matrix1.M13) * amount;
            result.M21 = matrix1.M21 + (matrix2.M21 - matrix1.M21) * amount;
            result.M22 = matrix1.M22 + (matrix2.M22 - matrix1.M22) * amount;
            result.M23 = matrix1.M23 + (matrix2.M23 - matrix1.M23) * amount;
            result.M31 = matrix1.M31 + (matrix2.M31 - matrix1.M31) * amount;
            result.M32 = matrix1.M32 + (matrix2.M32 - matrix1.M32) * amount;
            result.M33 = matrix1.M33 + (matrix2.M33 - matrix1.M33) * amount;
        }

        /// <summary>
        /// Performs spherical linear interpolation of position and rotation.
        /// </summary>
        public static void Slerp(ref Matrix3x3 matrix1, ref Matrix3x3 matrix2, float amount, out Matrix3x3 result)
        {
            Quaternion result1;
            Quaternion.CreateFromRotationMatrix(ref matrix1, out result1);
            Quaternion result2;
            Quaternion.CreateFromRotationMatrix(ref matrix2, out result2);
            Quaternion result3;
            Quaternion.Slerp(ref result1, ref result2, amount, out result3);
            Matrix3x3.CreateFromQuaternion(ref result3, out result);
        }

        /// <summary>
        /// Performs spherical linear interpolation of position and rotation and scale.
        /// </summary>
        public static void SlerpScale(ref Matrix3x3 matrix1, ref Matrix3x3 matrix2, float amount, out Matrix3x3 result)
        {
            Vector3 scale1 = matrix1.Scale;
            Vector3 scale2 = matrix2.Scale;
            if ((double)scale1.LengthSquared() < 9.99999997475243E-07 || (double)scale2.LengthSquared() < 9.99999997475243E-07)
            {
                result = Matrix3x3.Zero;
            }
            else
            {
                Matrix3x3 matrix3 = Matrix3x3.Normalize(matrix1);
                Matrix3x3 matrix4 = Matrix3x3.Normalize(matrix2);
                Quaternion result1;
                Quaternion.CreateFromRotationMatrix(ref matrix3, out result1);
                Quaternion result2;
                Quaternion.CreateFromRotationMatrix(ref matrix4, out result2);
                Quaternion result3;
                Quaternion.Slerp(ref result1, ref result2, amount, out result3);
                Matrix3x3.CreateFromQuaternion(ref result3, out result);
                Vector3 scale3 = Vector3.Lerp(scale1, scale2, amount);
                Matrix3x3.Rescale(ref result, ref scale3);
            }
        }

        /// <summary>Negates individual elements of a matrix.</summary>
        /// <param name="matrix">Source matrix.</param>
        /// <param name="result">[OutAttribute] Negated matrix.</param>
        public static void Negate(ref Matrix3x3 matrix, out Matrix3x3 result)
        {
            result.M11 = -matrix.M11;
            result.M12 = -matrix.M12;
            result.M13 = -matrix.M13;
            result.M21 = -matrix.M21;
            result.M22 = -matrix.M22;
            result.M23 = -matrix.M23;
            result.M31 = -matrix.M31;
            result.M32 = -matrix.M32;
            result.M33 = -matrix.M33;
        }

        /// <summary>Adds a matrix to another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        /// <param name="result">[OutAttribute] Resulting matrix.</param>
        public static void Add(ref Matrix3x3 matrix1, ref Matrix3x3 matrix2, out Matrix3x3 result)
        {
            result.M11 = matrix1.M11 + matrix2.M11;
            result.M12 = matrix1.M12 + matrix2.M12;
            result.M13 = matrix1.M13 + matrix2.M13;
            result.M21 = matrix1.M21 + matrix2.M21;
            result.M22 = matrix1.M22 + matrix2.M22;
            result.M23 = matrix1.M23 + matrix2.M23;
            result.M31 = matrix1.M31 + matrix2.M31;
            result.M32 = matrix1.M32 + matrix2.M32;
            result.M33 = matrix1.M33 + matrix2.M33;
        }

        /// <summary>Subtracts matrices.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        /// <param name="result">[OutAttribute] Result of the subtraction.</param>
        public static void Subtract(ref Matrix3x3 matrix1, ref Matrix3x3 matrix2, out Matrix3x3 result)
        {
            result.M11 = matrix1.M11 - matrix2.M11;
            result.M12 = matrix1.M12 - matrix2.M12;
            result.M13 = matrix1.M13 - matrix2.M13;
            result.M21 = matrix1.M21 - matrix2.M21;
            result.M22 = matrix1.M22 - matrix2.M22;
            result.M23 = matrix1.M23 - matrix2.M23;
            result.M31 = matrix1.M31 - matrix2.M31;
            result.M32 = matrix1.M32 - matrix2.M32;
            result.M33 = matrix1.M33 - matrix2.M33;
        }

        /// <summary>Multiplies a matrix by another matrix.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">Source matrix.</param>
        /// <param name="result">[OutAttribute] Result of the multiplication.</param>
        public static void Multiply(ref Matrix3x3 matrix1, ref Matrix3x3 matrix2, out Matrix3x3 result)
        {
            float num1 = (float)((double)matrix1.M11 * (double)matrix2.M11 + (double)matrix1.M12 * (double)matrix2.M21 + (double)matrix1.M13 * (double)matrix2.M31);
            float num2 = (float)((double)matrix1.M11 * (double)matrix2.M12 + (double)matrix1.M12 * (double)matrix2.M22 + (double)matrix1.M13 * (double)matrix2.M32);
            float num3 = (float)((double)matrix1.M11 * (double)matrix2.M13 + (double)matrix1.M12 * (double)matrix2.M23 + (double)matrix1.M13 * (double)matrix2.M33);
            float num4 = (float)((double)matrix1.M21 * (double)matrix2.M11 + (double)matrix1.M22 * (double)matrix2.M21 + (double)matrix1.M23 * (double)matrix2.M31);
            float num5 = (float)((double)matrix1.M21 * (double)matrix2.M12 + (double)matrix1.M22 * (double)matrix2.M22 + (double)matrix1.M23 * (double)matrix2.M32);
            float num6 = (float)((double)matrix1.M21 * (double)matrix2.M13 + (double)matrix1.M22 * (double)matrix2.M23 + (double)matrix1.M23 * (double)matrix2.M33);
            float num7 = (float)((double)matrix1.M31 * (double)matrix2.M11 + (double)matrix1.M32 * (double)matrix2.M21 + (double)matrix1.M33 * (double)matrix2.M31);
            float num8 = (float)((double)matrix1.M31 * (double)matrix2.M12 + (double)matrix1.M32 * (double)matrix2.M22 + (double)matrix1.M33 * (double)matrix2.M32);
            float num9 = (float)((double)matrix1.M31 * (double)matrix2.M13 + (double)matrix1.M32 * (double)matrix2.M23 + (double)matrix1.M33 * (double)matrix2.M33);
            result.M11 = num1;
            result.M12 = num2;
            result.M13 = num3;
            result.M21 = num4;
            result.M22 = num5;
            result.M23 = num6;
            result.M31 = num7;
            result.M32 = num8;
            result.M33 = num9;
        }

        /// <summary>Multiplies a matrix by a scalar value.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="scaleFactor">Scalar value.</param>
        /// <param name="result">[OutAttribute] The result of the multiplication.</param>
        public static void Multiply(ref Matrix3x3 matrix1, float scaleFactor, out Matrix3x3 result)
        {
            float num = scaleFactor;
            result.M11 = matrix1.M11 * num;
            result.M12 = matrix1.M12 * num;
            result.M13 = matrix1.M13 * num;
            result.M21 = matrix1.M21 * num;
            result.M22 = matrix1.M22 * num;
            result.M23 = matrix1.M23 * num;
            result.M31 = matrix1.M31 * num;
            result.M32 = matrix1.M32 * num;
            result.M33 = matrix1.M33 * num;
        }

        /// <summary>
        /// Divides the components of a matrix by the corresponding components of another matrix.
        /// </summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="matrix2">The divisor.</param>
        /// <param name="result">[OutAttribute] Result of the division.</param>
        public static void Divide(ref Matrix3x3 matrix1, ref Matrix3x3 matrix2, out Matrix3x3 result)
        {
            result.M11 = matrix1.M11 / matrix2.M11;
            result.M12 = matrix1.M12 / matrix2.M12;
            result.M13 = matrix1.M13 / matrix2.M13;
            result.M21 = matrix1.M21 / matrix2.M21;
            result.M22 = matrix1.M22 / matrix2.M22;
            result.M23 = matrix1.M23 / matrix2.M23;
            result.M31 = matrix1.M31 / matrix2.M31;
            result.M32 = matrix1.M32 / matrix2.M32;
            result.M33 = matrix1.M33 / matrix2.M33;
        }

        /// <summary>Divides the components of a matrix by a scalar.</summary>
        /// <param name="matrix1">Source matrix.</param>
        /// <param name="divider">The divisor.</param>
        /// <param name="result">[OutAttribute] Result of the division.</param>
        public static void Divide(ref Matrix3x3 matrix1, float divider, out Matrix3x3 result)
        {
            float num = 1f / divider;
            result.M11 = matrix1.M11 * num;
            result.M12 = matrix1.M12 * num;
            result.M13 = matrix1.M13 * num;
            result.M21 = matrix1.M21 * num;
            result.M22 = matrix1.M22 * num;
            result.M23 = matrix1.M23 * num;
            result.M31 = matrix1.M31 * num;
            result.M32 = matrix1.M32 * num;
            result.M33 = matrix1.M33 * num;
        }

        /// <summary>Gets the orientation.</summary>
        /// <returns></returns>
        public Matrix3x3 GetOrientation() => Matrix3x3.Identity with
        {
            Forward = Forward,
            Up = Up,
            Right = Right
        };

        [Conditional("DEBUG")]
        public void AssertIsValid()
        {
        }

        public bool IsValid() => float.IsFinite(M11 + M12 + M13 + M21 + M22 + M23 + M31 + M32 + M33);

        public bool IsNan() => float.IsNaN(M11 + M12 + M13 + M21 + M22 + M23 + M31 + M32 + M33);

        public bool IsRotation()
        {
            float num = 0.01f;
            return (double)Math.Abs(Right.Dot(Up)) <= (double)num && (double)Math.Abs(Right.Dot(Backward)) <= (double)num && (double)Math.Abs(Up.Dot(Backward)) <= (double)num && (double)Math.Abs(Right.LengthSquared() - 1f) <= (double)num && (double)Math.Abs(Up.LengthSquared() - 1f) <= (double)num && (double)Math.Abs(Backward.LengthSquared() - 1f) <= (double)num;
        }

        public static Matrix3x3 CreateFromDir(Vector3 dir)
        {
            Vector3 vector2 = new Vector3(0.0f, 0.0f, 1f);
            float z = dir.Z;
            Vector3 vector3;
            if ((double)z > -0.99999 && (double)z < 0.99999)
            {
                vector2 = Vector3.Normalize(vector2 - dir * z);
                vector3 = Vector3.Cross(dir, vector2);
            }
            else
            {
                vector2 = new Vector3(dir.Z, 0.0f, -dir.X);
                vector3 = new Vector3(0.0f, 1f, 0.0f);
            }
            return Matrix3x3.Identity with
            {
                Right = vector2,
                Up = vector3,
                Forward = dir
            };
        }

        /// <summary>Creates a world matrix with the specified parameters.</summary>
        /// <param name="forward">Forward direction of the object.</param>
        /// <param name="up">Upward direction of the object; usually [0, 1, 0].</param>
        public static Matrix3x3 CreateWorld(ref Vector3 forward, ref Vector3 up)
        {
            Vector3 result1;
            Vector3.Normalize(ref forward, out result1);
            Vector3 vector3 = -result1;
            Vector3 result2;
            Vector3.Cross(ref up, ref vector3, out result2);
            Vector3 result3;
            Vector3.Normalize(ref result2, out result3);
            Vector3 result4;
            Vector3.Cross(ref vector3, ref result3, out result4);
            Matrix3x3 world;
            world.M11 = result3.X;
            world.M12 = result3.Y;
            world.M13 = result3.Z;
            world.M21 = result4.X;
            world.M22 = result4.Y;
            world.M23 = result4.Z;
            world.M31 = vector3.X;
            world.M32 = vector3.Y;
            world.M33 = vector3.Z;
            return world;
        }

        public static Matrix3x3 CreateFromDir(Vector3 dir, Vector3 suggestedUp)
        {
            Vector3 up = Vector3.Cross(Vector3.Cross(dir, suggestedUp), dir);
            return Matrix3x3.CreateWorld(ref dir, ref up);
        }

        public static Matrix3x3 Normalize(Matrix3x3 matrix)
        {
            Matrix3x3 matrix3x3 = matrix;
            matrix3x3.Right = Vector3.Normalize(matrix3x3.Right);
            matrix3x3.Up = Vector3.Normalize(matrix3x3.Up);
            matrix3x3.Forward = Vector3.Normalize(matrix3x3.Forward);
            return matrix3x3;
        }

        public static Matrix3x3 Orthogonalize(Matrix3x3 rotationMatrix)
        {
            Matrix3x3 matrix3x3 = rotationMatrix;
            matrix3x3.Right = Vector3.Normalize(matrix3x3.Right);
            matrix3x3.Up = Vector3.Normalize(matrix3x3.Up - matrix3x3.Right * matrix3x3.Up.Dot(matrix3x3.Right));
            matrix3x3.Backward = Vector3.Normalize(matrix3x3.Backward - matrix3x3.Right * matrix3x3.Backward.Dot(matrix3x3.Right) - matrix3x3.Up * matrix3x3.Backward.Dot(matrix3x3.Up));
            return matrix3x3;
        }

        public static Matrix3x3 Round(ref Matrix3x3 matrix)
        {
            Matrix3x3 matrix3x3 = matrix;
            matrix3x3.Right = (Vector3)Vector3I.Round(matrix3x3.Right);
            matrix3x3.Up = (Vector3)Vector3I.Round(matrix3x3.Up);
            matrix3x3.Forward = (Vector3)Vector3I.Round(matrix3x3.Forward);
            return matrix3x3;
        }

        public static Matrix3x3 AlignRotationToAxes(
          ref Matrix3x3 toAlign,
          ref Matrix3x3 axisDefinitionMatrix)
        {
            Matrix3x3 identity = Matrix3x3.Identity;
            bool flag1 = false;
            bool flag2 = false;
            bool flag3 = false;
            float num1 = toAlign.Right.Dot(axisDefinitionMatrix.Right);
            float num2 = toAlign.Right.Dot(axisDefinitionMatrix.Up);
            float num3 = toAlign.Right.Dot(axisDefinitionMatrix.Backward);
            if ((double)Math.Abs(num1) > (double)Math.Abs(num2))
            {
                if ((double)Math.Abs(num1) > (double)Math.Abs(num3))
                {
                    identity.Right = (double)num1 > 0.0 ? axisDefinitionMatrix.Right : axisDefinitionMatrix.Left;
                    flag1 = true;
                }
                else
                {
                    identity.Right = (double)num3 > 0.0 ? axisDefinitionMatrix.Backward : axisDefinitionMatrix.Forward;
                    flag3 = true;
                }
            }
            else if ((double)Math.Abs(num2) > (double)Math.Abs(num3))
            {
                identity.Right = (double)num2 > 0.0 ? axisDefinitionMatrix.Up : axisDefinitionMatrix.Down;
                flag2 = true;
            }
            else
            {
                identity.Right = (double)num3 > 0.0 ? axisDefinitionMatrix.Backward : axisDefinitionMatrix.Forward;
                flag3 = true;
            }
            Vector3 vector3 = toAlign.Up;
            float num4 = vector3.Dot(axisDefinitionMatrix.Right);
            vector3 = toAlign.Up;
            float num5 = vector3.Dot(axisDefinitionMatrix.Up);
            vector3 = toAlign.Up;
            float num6 = vector3.Dot(axisDefinitionMatrix.Backward);
            bool flag4;
            if (flag2 || (double)Math.Abs(num4) > (double)Math.Abs(num5) && !flag1)
            {
                if ((double)Math.Abs(num4) > (double)Math.Abs(num6) | flag3)
                {
                    identity.Up = (double)num4 > 0.0 ? axisDefinitionMatrix.Right : axisDefinitionMatrix.Left;
                    flag1 = true;
                }
                else
                {
                    identity.Up = (double)num6 > 0.0 ? axisDefinitionMatrix.Backward : axisDefinitionMatrix.Forward;
                    flag4 = true;
                }
            }
            else if ((double)Math.Abs(num5) > (double)Math.Abs(num6) | flag3)
            {
                identity.Up = (double)num5 > 0.0 ? axisDefinitionMatrix.Up : axisDefinitionMatrix.Down;
                flag2 = true;
            }
            else
            {
                identity.Up = (double)num6 > 0.0 ? axisDefinitionMatrix.Backward : axisDefinitionMatrix.Forward;
                flag4 = true;
            }
            if (!flag1)
            {
                vector3 = toAlign.Backward;
                float num7 = vector3.Dot(axisDefinitionMatrix.Right);
                identity.Backward = (double)num7 > 0.0 ? axisDefinitionMatrix.Right : axisDefinitionMatrix.Left;
            }
            else if (!flag2)
            {
                vector3 = toAlign.Backward;
                float num8 = vector3.Dot(axisDefinitionMatrix.Up);
                identity.Backward = (double)num8 > 0.0 ? axisDefinitionMatrix.Up : axisDefinitionMatrix.Down;
            }
            else
            {
                vector3 = toAlign.Backward;
                float num9 = vector3.Dot(axisDefinitionMatrix.Backward);
                identity.Backward = (double)num9 > 0.0 ? axisDefinitionMatrix.Backward : axisDefinitionMatrix.Forward;
            }
            return identity;
        }

        public static bool GetEulerAnglesXYZ(ref Matrix3x3 mat, out Vector3 xyz)
        {
            float x1 = mat.GetRow(0).X;
            float y1 = mat.GetRow(0).Y;
            float z1 = mat.GetRow(0).Z;
            float x2 = mat.GetRow(1).X;
            float y2 = mat.GetRow(1).Y;
            float z2 = mat.GetRow(1).Z;
            mat.GetRow(2);
            mat.GetRow(2);
            float z3 = mat.GetRow(2).Z;
            float num = z1;
            if ((double)num < 1.0)
            {
                if ((double)num > -1.0)
                {
                    xyz = new Vector3((float)Math.Atan2(-(double)z2, (double)z3), (float)Math.Asin((double)z1), (float)Math.Atan2(-(double)y1, (double)x1));
                    return true;
                }
                xyz = new Vector3((float)-Math.Atan2((double)x2, (double)y2), -1.570796f, 0.0f);
                return false;
            }
            xyz = new Vector3((float)Math.Atan2((double)x2, (double)y2), -1.570796f, 0.0f);
            return false;
        }

        public bool IsMirrored() => Determinant() < 0;

        public bool IsOrthogonal() =>
            Math.Abs(Up.LengthSquared()) - 1 < 10 &&
            Math.Abs(Right.LengthSquared()) - 1 < 10 &&
            Math.Abs(Forward.LengthSquared()) - 1.0 < 10 &&
            Math.Abs(Right.Dot(Up)) < 10 &&
            Math.Abs(Right.Dot(Forward)) < 10;

        private struct F9
        {
            public unsafe fixed float data[9];
        }
    }
}