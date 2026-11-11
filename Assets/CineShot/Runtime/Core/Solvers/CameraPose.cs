using UnityEngine;

namespace CineShot.Core
{
    public struct CameraPose
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Fov;
        public float NearClip;
        public float FarClip;
        public float FocusDistance;
        public float Dutch;

        public static readonly CameraPose Identity = new CameraPose
        {
            Rotation = Quaternion.identity,
            Fov = 40f,
            NearClip = 0.1f,
            FarClip = 1000f,
            FocusDistance = 10f
        };
    }

    public struct TargetSample
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation;
        public bool Valid;
    }

    public struct ShotContext
    {
        public float LocalTime;
        public float Duration;
        public float DeltaTime;
        public TargetSample LookAt;
        public TargetSample Follow;
        public Vector3 CompositionOffset;
        public int Seed;
        public CineSpline Spline;
    }

    public interface ISolver
    {
        void Solve(in ShotContext ctx, in RigParams p, ref CameraPose pose);
    }

    public delegate void SolveDelegate(in ShotContext ctx, in RigParams p, ref CameraPose pose);
}
