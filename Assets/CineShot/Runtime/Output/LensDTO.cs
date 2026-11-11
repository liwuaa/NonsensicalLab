namespace CineShot.Output
{
    public struct LensDTO
    {
        public float FieldOfView;
        public float NearClipPlane;
        public float FarClipPlane;
        public float Dutch;
    }

    public interface ILensWriter
    {
        bool IsValid { get; }
        int Priority { get; set; }
        UnityEngine.Transform Transform { get; }
        bool SetPose(UnityEngine.Vector3 position, UnityEngine.Quaternion rotation);
        bool SetLens(in LensDTO lens);
        void SetEnabled(bool enabled);
    }
}
