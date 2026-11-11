using UnityEngine.Assertions;

namespace CineShot.Core
{
    public static class SolverDispatch
    {
        static readonly SolveDelegate[] s_Table =
        {
            StaticSolver.SolveStatic,
            DollyTrackSolver.SolveStatic,
            OrbitSolver.SolveStatic,
            PushPullSolver.SolveStatic,
            CraneSolver.SolveStatic,
            ShoulderSolver.SolveStatic,
            HandheldSolver.SolveStatic,
            SplineDollySolver.SolveStatic
        };

        static SolverDispatch()
        {
            Assert.AreEqual((int)RigKind.SplineDolly + 1, s_Table.Length, "RigKind 与求解器表长度不一致");
        }

        public static SolveDelegate Get(RigKind kind)
        {
            int i = (int)kind;
            if (i < 0 || i >= s_Table.Length)
                return StaticSolver.SolveStatic;
            return s_Table[i];
        }

        public static void Solve(RigKind kind, in ShotContext ctx, in RigParams p, ref CameraPose pose)
        {
            Get(kind)(in ctx, in p, ref pose);
        }

        public static void SolveGeneric<T>(in ShotContext ctx, in RigParams p, ref CameraPose pose)
            where T : struct, ISolver
        {
            default(T).Solve(in ctx, in p, ref pose);
        }
    }
}
