using VRageMath;

namespace VRage.Ansel
{
    internal struct MyAnselCamera
    {
        private Quaternion m_initQuaternion;
        private Quaternion m_anselQuaterion;
        public Matrix ProjectionMatrix;
        public Matrix ProjectionFarMatrix;
        public float FOV;
        public float NearPlane;
        public float FarPlane;
        public Vector2 ProjectionOffset;
        private Vector3D m_initPosition;
        private Vector3 m_anselPosition;
        public float FarFarPlane;

        public Vector3D Position => m_initPosition + Vector3D.Transform((Vector3D)m_anselPosition, Quaternion.Inverse(m_initQuaternion));

        public MatrixD ViewMatrix => MatrixD.CreateTranslation(-Position) * MatrixD.CreateFromQuaternion(Quaternion.Inverse(m_anselQuaterion) * m_initQuaternion);

        private Quat QuaterionToNvQuat(Quaternion quaternion) => new Quat()
        {
            x = quaternion.X,
            y = quaternion.Y,
            z = quaternion.Z,
            w = quaternion.W
        };

        private Quaternion NvQuatToQuaterion(Quat quat) => new Quaternion(quat.x, quat.y, quat.z, quat.w);

        public MyAnselCamera(MatrixD viewMatrix, float fov, float aspectRatio, float nearPlane, float farPlane, float farFarPlane, Vector3D position, float projectionOffsetX, float projectionOffsetY)
        {
            m_initQuaternion = Quaternion.CreateFromRotationMatrix(in viewMatrix);
            m_anselQuaterion = Quaternion.Identity;
            ProjectionMatrix = MatrixD.CreatePerspectiveFieldOfView(fov, aspectRatio, nearPlane, farPlane);
            ProjectionFarMatrix = MatrixD.CreatePerspectiveFieldOfView(fov, aspectRatio, nearPlane, farFarPlane);
            FOV = fov;
            NearPlane = nearPlane;
            FarPlane = farPlane;
            FarFarPlane = farFarPlane;
            m_initPosition = position;
            m_anselPosition = Vector3.Zero;
            ProjectionOffset = new Vector2(projectionOffsetX, projectionOffsetY);
        }

        public void Update(bool spectator)
        {
            Camera camera = new Camera()
            {
                rotation = QuaterionToNvQuat(m_anselQuaterion),
                fov = FOV / MathF.PI * 180,
                position = new Vec3(m_anselPosition.X, m_anselPosition.Y, m_anselPosition.Z)
            };

            NativeMethods.updateCamera(ref camera);
            m_anselQuaterion = NvQuatToQuaterion(camera.rotation);
            if (spectator)
                m_anselPosition = new Vector3(camera.position.x, camera.position.y, camera.position.z);

            ProjectionOffset = new Vector2(camera.projectionOffsetX, camera.projectionOffsetY);
            ProjectionMatrix.M31 = ProjectionOffset.X;
            ProjectionMatrix.M32 = ProjectionOffset.Y;

            FOV = FOV / MathF.PI * 180;
        }
    }
}
