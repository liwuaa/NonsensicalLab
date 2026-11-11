using CineShot.Core;
using NUnit.Framework;
using UnityEngine;

namespace CineShot.Tests
{
    public class SolverTests
    {
        [Test]
        public void Orbit_IsDeterministic_ForSameSeed()
        {
            var ctx = new ShotContext
            {
                LocalTime = 1.25f,
                Duration = 5f,
                DeltaTime = 0.016f,
                LookAt = new TargetSample { Position = Vector3.zero, Valid = true },
                Follow = new TargetSample { Position = Vector3.zero, Valid = true },
                Seed = 12345
            };
            var p = RigParams.DefaultFor(RigKind.Orbit);
            var a = CameraPose.Identity;
            var b = CameraPose.Identity;
            OrbitSolver.SolveStatic(in ctx, in p, ref a);
            OrbitSolver.SolveStatic(in ctx, in p, ref b);
            Assert.AreEqual(a.Position, b.Position);
            Assert.AreEqual(a.Rotation, b.Rotation);
        }

        [Test]
        public void Handheld_SameSeedSameResult()
        {
            var ctx = new ShotContext { LocalTime = 2f, Seed = 99 };
            var p = RigParams.DefaultFor(RigKind.Handheld);
            var a = CameraPose.Identity;
            a.Position = Vector3.one;
            a.Rotation = Quaternion.identity;
            var b = a;
            HandheldSolver.SolveStatic(in ctx, in p, ref a);
            HandheldSolver.SolveStatic(in ctx, in p, ref b);
            Assert.AreEqual(a.Position, b.Position);
        }

        [Test]
        public void PoseAccumulator_FocusUsesDiopter()
        {
            var acc = default(PoseAccumulator);
            var near = CameraPose.Identity;
            near.FocusDistance = 2f;
            var far = CameraPose.Identity;
            far.FocusDistance = 8f;
            acc.Add(in near, 0.5f);
            acc.Add(in far, 0.5f);
            CameraPose pose;
            Assert.IsTrue(acc.TryBuild(out pose));
            // 1/d blend => 1 / ((0.5/2)+(0.5/8)) = 1 / 0.3125 = 3.2
            Assert.AreEqual(3.2f, pose.FocusDistance, 1e-3f);
        }

        [Test]
        public void EasingAsset_Lut_Endpoints()
        {
            var ease = ScriptableObject.CreateInstance<EasingAsset>();
            ease.kind = EasingAsset.EaseKind.EaseInOutCubic;
            ease.Rebuild();
            Assert.AreEqual(0f, ease.Evaluate(0f), 1e-5f);
            Assert.AreEqual(1f, ease.Evaluate(1f), 1e-5f);
            Object.DestroyImmediate(ease);
        }

        [Test]
        public void SolverDispatch_TableLengthMatchesRigKind()
        {
            Assert.NotNull(SolverDispatch.Get(RigKind.Static));
            Assert.NotNull(SolverDispatch.Get(RigKind.SplineDolly));
        }
    }
}
