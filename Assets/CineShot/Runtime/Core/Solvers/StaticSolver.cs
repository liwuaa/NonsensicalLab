using UnityEngine;

namespace CineShot.Core
{
    public struct StaticSolver : ISolver
    {
        public void Solve(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            SolveStatic(in ctx, in p, ref pose);
        }

        public static void SolveStatic(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            Vector3 anchor = SolverMath.Anchor(in ctx);
            Quaternion anchorRot = ctx.Follow.Valid ? ctx.Follow.Rotation : Quaternion.identity;
            pose.Position = anchor + anchorRot * p.A;
            SolverMath.LookAtTarget(in ctx, ref pose, p.P0, p.P1);
            SolverMath.ApplyComposition(ref pose, ctx.CompositionOffset);
        }
    }

    public struct DollyTrackSolver : ISolver
    {
        public void Solve(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            SolveStatic(in ctx, in p, ref pose);
        }

        public static void SolveStatic(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            float u = SolverMath.NormalizedTime(in ctx);
            float eased = u * u * (3f - 2f * u);
            Vector3 onTrack = Vector3.Lerp(p.A, p.B, eased);
            Vector3 lateral = Vector3.Cross(Vector3.up, (p.B - p.A).normalized);
            if (lateral.sqrMagnitude < 1e-6f)
                lateral = Vector3.right;
            pose.Position = onTrack + lateral * p.P1 + Vector3.up * (p.P2 - onTrack.y);
            TargetSample look = ctx.LookAt;
            if (look.Valid && p.P0 > 0f)
                look.Position = SolverMath.Predict(in look, p.P0);
            var lookCtx = ctx;
            lookCtx.LookAt = look;
            SolverMath.LookAtTarget(in lookCtx, ref pose, 0f, 0f);
            pose.Dutch = p.P3;
            SolverMath.ApplyComposition(ref pose, ctx.CompositionOffset);
        }
    }

    public struct OrbitSolver : ISolver
    {
        public void Solve(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            SolveStatic(in ctx, in p, ref pose);
        }

        public static void SolveStatic(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            Vector3 c = SolverMath.Anchor(in ctx);
            float ang = (p.P0 + p.P1 * ctx.LocalTime) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
            pose.Position = c + dir * p.P2 + Vector3.up * p.P3;
            SolverMath.LookAtTarget(in ctx, ref pose, 0f, 0f);
            float u = SolverMath.NormalizedTime(in ctx);
            pose.Fov = Mathf.Lerp(pose.Fov, p.A.x > 1f ? p.A.x : pose.Fov, u * 0.5f);
            SolverMath.ApplyComposition(ref pose, ctx.CompositionOffset);
        }
    }

    public struct PushPullSolver : ISolver
    {
        public void Solve(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            SolveStatic(in ctx, in p, ref pose);
        }

        public static void SolveStatic(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            Vector3 anchor = SolverMath.Anchor(in ctx);
            float u = SolverMath.NormalizedTime(in ctx);
            float dist = Mathf.Lerp(p.P0, p.P1, u * u * (3f - 2f * u));
            Vector3 back = ctx.Follow.Valid
                ? -(ctx.Follow.Rotation * Vector3.forward)
                : Vector3.back;
            if (back.sqrMagnitude < 1e-6f)
                back = Vector3.back;
            pose.Position = anchor + back.normalized * dist + Vector3.up * p.P2;
            SolverMath.LookAtTarget(in ctx, ref pose, 0f, 0f);
            if (p.P3 > 0.5f && p.A.x > 1f)
            {
                float startFov = pose.Fov;
                float endFov = p.A.x;
                pose.Fov = Mathf.Lerp(startFov, endFov, u);
            }
            SolverMath.ApplyComposition(ref pose, ctx.CompositionOffset);
        }
    }
}
