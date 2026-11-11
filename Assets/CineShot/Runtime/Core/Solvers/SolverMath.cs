using UnityEngine;

namespace CineShot.Core
{
    public static class SolverMath
    {
        public static float FrameRateIndependentAlpha(float dt, float tau)
        {
            if (tau <= 1e-5f)
                return 1f;
            return 1f - Mathf.Exp(-dt / tau);
        }

        public static Vector3 Anchor(in ShotContext ctx)
        {
            if (ctx.Follow.Valid)
                return ctx.Follow.Position;
            if (ctx.LookAt.Valid)
                return ctx.LookAt.Position;
            return Vector3.zero;
        }

        public static void LookAtTarget(in ShotContext ctx, ref CameraPose pose, float yawBiasDeg, float pitchBiasDeg)
        {
            Vector3 target = ctx.LookAt.Valid ? ctx.LookAt.Position : (pose.Position + Vector3.forward);
            Vector3 fwd = target - pose.Position;
            if (fwd.sqrMagnitude > 1e-6f)
            {
                Quaternion look = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                pose.Rotation = look * Quaternion.Euler(pitchBiasDeg, yawBiasDeg, 0f);
            }
            else
            {
                pose.Rotation = Quaternion.identity;
            }
        }

        public static void ApplyComposition(ref CameraPose pose, in Vector3 compositionOffset)
        {
            pose.Position += pose.Rotation * compositionOffset;
        }

        public static void ClampOptics(ref CameraPose pose)
        {
            pose.Fov = Mathf.Clamp(pose.Fov, 12f, 100f);
            pose.NearClip = Mathf.Clamp(pose.NearClip, 0.01f, 1f);
            pose.FarClip = Mathf.Max(pose.FarClip, pose.NearClip + 0.1f);
            pose.FocusDistance = Mathf.Max(0.1f, pose.FocusDistance);
        }

        public static void KeepMinDistance(ref CameraPose pose, in TargetSample target, float minDistance)
        {
            if (!target.Valid || minDistance <= 0f)
                return;
            Vector3 d = pose.Position - target.Position;
            float sqr = d.sqrMagnitude;
            float minSqr = minDistance * minDistance;
            if (sqr < minSqr && sqr > 1e-8f)
                pose.Position = target.Position + d.normalized * minDistance;
        }

        public static float NormalizedTime(in ShotContext ctx)
        {
            return ctx.Duration > 1e-5f ? Mathf.Clamp01(ctx.LocalTime / ctx.Duration) : 0f;
        }

        public static Vector3 Predict(in TargetSample sample, float lookAhead)
        {
            if (!sample.Valid)
                return Vector3.zero;
            return sample.Position + sample.Velocity * lookAhead;
        }
    }
}
