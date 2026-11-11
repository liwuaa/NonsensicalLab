using UnityEngine;

namespace CineShot.Core
{
    public struct CraneSolver : ISolver
    {
        public void Solve(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            SolveStatic(in ctx, in p, ref pose);
        }

        public static void SolveStatic(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            Vector3 anchor = SolverMath.Anchor(in ctx);
            float u = SolverMath.NormalizedTime(in ctx);
            float eased = u * u * (3f - 2f * u);
            float height = Mathf.Lerp(p.P0, p.P1, eased);
            float arc = p.P2 * Mathf.Sin(eased * Mathf.PI) * (p.I0 >= 0 ? 1f : -1f);
            float horiz = Mathf.Lerp(0f, p.P3, eased);
            Vector3 right = ctx.Follow.Valid ? ctx.Follow.Rotation * Vector3.right : Vector3.right;
            pose.Position = anchor + right * arc + Vector3.forward * -horiz + Vector3.up * height;
            float pitch = Mathf.Lerp(5f, 38f, eased);
            SolverMath.LookAtTarget(in ctx, ref pose, 0f, pitch);
            SolverMath.ApplyComposition(ref pose, ctx.CompositionOffset);
        }
    }

    public struct ShoulderSolver : ISolver
    {
        public void Solve(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            SolveStatic(in ctx, in p, ref pose);
        }

        public static void SolveStatic(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            Vector3 followPos = ctx.Follow.Valid ? ctx.Follow.Position : SolverMath.Anchor(in ctx);
            Quaternion followRot = ctx.Follow.Valid ? ctx.Follow.Rotation : Quaternion.identity;
            float side = p.I0 >= 0 ? 1f : -1f;
            Vector3 shoulder = followPos
                               + followRot * (Vector3.right * (side * p.P0))
                               + Vector3.up * p.P1
                               + followRot * (Vector3.back * 0.35f);
            pose.Position = shoulder;
            SolverMath.LookAtTarget(in ctx, ref pose, p.P2, 0f);
            pose.Dutch = p.P3;
            pose.NearClip = Mathf.Min(pose.NearClip, 0.05f);
            SolverMath.ApplyComposition(ref pose, ctx.CompositionOffset);
        }
    }

    public struct HandheldSolver : ISolver
    {
        public void Solve(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            SolveStatic(in ctx, in p, ref pose);
        }

        public static void SolveStatic(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            float t = ctx.LocalTime * Mathf.Max(0.01f, p.P1) + ctx.Seed * 0.137f;
            float nx = (Mathf.PerlinNoise(t, 0.31f) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(0.71f, t) - 0.5f) * 2f;
            float nz = (Mathf.PerlinNoise(t * 0.7f, 1.17f) - 0.5f) * 2f;
            pose.Position += pose.Rotation * new Vector3(nx, ny, nz * 0.35f) * p.P0;
            pose.Rotation *= Quaternion.Euler(ny * p.P2, nx * p.P2, nx * p.P3);
        }
    }

    public struct SplineDollySolver : ISolver
    {
        public void Solve(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            SolveStatic(in ctx, in p, ref pose);
        }

        public static void SolveStatic(in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            float u = SolverMath.NormalizedTime(in ctx);
            float eased = u * u * (3f - 2f * u);
            eased = Mathf.Clamp01(eased * Mathf.Max(0.01f, p.P1));
            if (ctx.Spline != null)
            {
                pose.Position = ctx.Spline.Evaluate(eased);
            }
            else
            {
                pose.Position = Vector3.Lerp(p.A, p.B, eased) + Vector3.up * p.P3;
            }

            TargetSample look = ctx.LookAt;
            if (look.Valid && p.P2 > 0f)
                look.Position = SolverMath.Predict(in look, p.P2);
            var lookCtx = ctx;
            lookCtx.LookAt = look;
            SolverMath.LookAtTarget(in lookCtx, ref pose, 0f, 0f);
            SolverMath.ApplyComposition(ref pose, ctx.CompositionOffset);
        }
    }
}
