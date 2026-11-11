using UnityEngine;
using UnityEngine.Playables;

namespace CineShot.Core
{
    public class CineMixerBehaviour : PlayableBehaviour
    {
        CameraPose m_PrevPose;
        bool m_HasPrev;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var rig = playerData as CineShotRig;
            if (rig == null)
                return;

            float dt = info.deltaTime;
            int n = playable.GetInputCount();
            PoseAccumulator acc = default(PoseAccumulator);
            float wsum = 0f;

            ShotAsset activeShot = null;
            FocusMode focusMode = FocusMode.None;
            float focusHint = 10f;
            Transform focusTarget = null;
            HandheldLayer handheldLayer = default(HandheldLayer);
            bool matchFrame = false;
            CameraPose matchPose = CameraPose.Identity;

            for (int j = 0; j < n; j++)
            {
                float w = playable.GetInputWeight(j);
                if (w <= 0f)
                    continue;

                var input = (ScriptPlayable<CineShotBehaviour>)playable.GetInput(j);
                var beh = input.GetBehaviour();
                if (beh == null || beh.Clip == null || beh.Clip.shot == null)
                    continue;

                var clip = beh.Clip;
                var shot = clip.shot;
                double dur = input.GetDuration();
                float local = dur > 0.0 ? (float)(input.GetTime() / dur) : 0f;
                local = Mathf.Clamp01(local);

                EasingAsset easeIn = clip.easeIn != null ? clip.easeIn : shot.easeIn;
                EasingAsset easeOut = clip.easeOut != null ? clip.easeOut : shot.easeOut;
                if ((clip.overrides.mask & ShotOverrideMask.Easing) != 0)
                {
                    if (clip.overrides.easeIn != null) easeIn = clip.overrides.easeIn;
                    if (clip.overrides.easeOut != null) easeOut = clip.overrides.easeOut;
                }

                float shaped = w;
                if (clip.blendMode != ShotBlendMode.Cut)
                {
                    float edge = 0.25f;
                    if (local < edge)
                        shaped *= EasingAsset.EvaluateOrLinear(easeIn, local / edge);
                    else if (local > 1f - edge)
                        shaped *= EasingAsset.EvaluateOrLinear(easeOut, (1f - local) / edge);
                }
                if (shaped <= 0f)
                    continue;

                RigParams rigParams;
                Vector3 composition;
                float fov, near, far, focusDistance;
                FocusMode mode;
                EasingAsset ignoredIn, ignoredOut;
                clip.overrides.ApplyTo(shot, out rigParams, out composition, out fov, out near, out far,
                    out mode, out focusDistance, out ignoredIn, out ignoredOut);

                Transform lookTr = clip.lookAt.Resolve(playable.GetGraph().GetResolver());
                Transform followTr = clip.follow.Resolve(playable.GetGraph().GetResolver());

                var look = rig.SampleLookAt(lookTr, shot.targetTag, shot.targetBinding.lookAtOffset, dt);
                var follow = rig.SampleFollow(followTr, shot.targetTag, shot.targetBinding.followOffset, dt);

                var ctx = new ShotContext
                {
                    LocalTime = (float)input.GetTime(),
                    Duration = (float)dur,
                    DeltaTime = dt,
                    LookAt = look,
                    Follow = follow,
                    CompositionOffset = composition,
                    Seed = shot.Seed,
                    Spline = clip.spline
                };

                var pose = CameraPose.Identity;
                pose.Fov = fov;
                pose.NearClip = near;
                pose.FarClip = far;
                pose.FocusDistance = focusDistance;
                SolverDispatch.Solve(shot.rigKind, in ctx, in rigParams, ref pose);

                if (clip.blendMode == ShotBlendMode.MatchFrame && shot.matchPoint.valid && local < 0.02f)
                {
                    matchFrame = true;
                    matchPose.Position = shot.matchPoint.position;
                    matchPose.Rotation = shot.matchPoint.rotation;
                    matchPose.Fov = shot.matchPoint.fov > 1f ? shot.matchPoint.fov : pose.Fov;
                    matchPose.NearClip = pose.NearClip;
                    matchPose.FarClip = pose.FarClip;
                    matchPose.FocusDistance = pose.FocusDistance;
                }

                acc.Add(in pose, shaped);
                wsum += shaped;

                if (shaped >= wsum * 0.5f || activeShot == null)
                {
                    activeShot = shot;
                    focusMode = mode;
                    focusHint = focusDistance;
                    focusTarget = lookTr != null ? lookTr : followTr;
                    handheldLayer = shot.handheld;
                }
            }

            if (wsum <= 0f)
            {
                rig.ReleasePose(dt);
                return;
            }

            CameraPose blended;
            if (!acc.TryBuild(out blended))
            {
                rig.ReleasePose(dt);
                return;
            }

            if (matchFrame && m_HasPrev)
            {
                blended.Position = matchPose.Position;
                blended.Rotation = matchPose.Rotation;
                blended.Fov = matchPose.Fov;
            }

            if (handheldLayer.enabled)
                HandheldSolver.SolveStatic(
                    new ShotContext { LocalTime = (float)playable.GetTime(), Seed = activeShot != null ? activeShot.Seed : 0 },
                    in handheldLayer.parameters,
                    ref blended);

            Transform focusTr = focusTarget;
            rig.SubmitPose(in blended, dt, focusHint, focusMode, focusTr);
            m_PrevPose = blended;
            m_HasPrev = true;
        }
    }
}
