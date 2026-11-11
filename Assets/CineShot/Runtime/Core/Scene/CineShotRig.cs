using UnityEngine;

namespace CineShot.Core
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CineShotRig : MonoBehaviour
    {
        [SerializeField] Component m_OutputVcam;
        [SerializeField] Component m_GameVcam;
        [SerializeField] FocusProfile m_FocusProfile;
        [SerializeField] float m_MinTargetDistance = 0.35f;
        [SerializeField] Camera m_FallbackCamera;

        readonly CineTargets m_Targets = new CineTargets();
        readonly FocusSolver m_FocusSolver = new FocusSolver();
        readonly FocusRequest[] m_FocusBuffer = new FocusRequest[8];
        ICameraOutput m_Output = NullCameraOutput.Instance;
        IFocusOutput m_FocusOutput = NullFocusOutput.Instance;
        CameraPose m_LastPose = CameraPose.Identity;
        bool m_HasPose;
        Vector3 m_LookLast;
        Vector3 m_FollowLast;
        bool m_HasLookLast;
        bool m_HasFollowLast;

        public CineTargets Targets { get { return m_Targets; } }
        public FocusProfile FocusProfile { get { return m_FocusProfile; } }
        public ICameraOutput Output { get { return m_Output; } }
        public CameraPose LastPose { get { return m_LastPose; } }
        public bool HasPose { get { return m_HasPose; } }
        public Component OutputVcam { get { return m_OutputVcam; } }
        public Component GameVcam { get { return m_GameVcam; } }

        void OnEnable()
        {
            m_Targets.Activate();
            RebuildOutputs();
            m_FocusSolver.Reset(m_FocusProfile != null ? 10f : 10f);
        }

        void OnDisable()
        {
            if (m_FocusOutput != null)
            {
                m_FocusOutput.Dispose();
                m_FocusOutput = NullFocusOutput.Instance;
            }
            m_Output = NullCameraOutput.Instance;
            m_Targets.Deactivate();
            m_HasPose = false;
        }

        public void RebuildOutputs()
        {
            m_Output = CineOutputFactory.GetCameraOutput(m_OutputVcam);
            m_Output.Bind(m_FallbackCamera != null ? m_FallbackCamera : Camera.main);
            if (m_FocusOutput != null && !ReferenceEquals(m_FocusOutput, NullFocusOutput.Instance))
                m_FocusOutput.Dispose();
            m_FocusOutput = CineOutputFactory.GetFocusOutput(transform, m_FocusProfile);
        }

        public TargetSample SampleLookAt(Transform explicitTarget, string tag, Vector3 offset, float dt)
        {
            if (explicitTarget != null)
            {
                Vector3 pos = explicitTarget.TransformPoint(offset);
                Vector3 vel = Vector3.zero;
                if (m_HasLookLast && dt > 1e-5f)
                    vel = (pos - m_LookLast) / dt;
                m_LookLast = pos;
                m_HasLookLast = true;
                return new TargetSample
                {
                    Position = pos,
                    Velocity = vel,
                    Rotation = explicitTarget.rotation,
                    Valid = true
                };
            }
            return m_Targets.Sample(tag, offset, dt, ref m_LookLast, ref m_HasLookLast);
        }

        public TargetSample SampleFollow(Transform explicitTarget, string tag, Vector3 offset, float dt)
        {
            if (explicitTarget != null)
            {
                Vector3 pos = explicitTarget.TransformPoint(offset);
                Vector3 vel = Vector3.zero;
                if (m_HasFollowLast && dt > 1e-5f)
                    vel = (pos - m_FollowLast) / dt;
                m_FollowLast = pos;
                m_HasFollowLast = true;
                return new TargetSample
                {
                    Position = pos,
                    Velocity = vel,
                    Rotation = explicitTarget.rotation,
                    Valid = true
                };
            }
            return m_Targets.Sample(tag, offset, dt, ref m_FollowLast, ref m_HasFollowLast);
        }

        public void SubmitPose(in CameraPose pose, float dt, float focusDistanceHint, FocusMode focusMode, Transform focusTarget)
        {
            CameraPose finalPose = pose;
            SolverMath.ClampOptics(ref finalPose);
            if (finalPose.Rotation == default(Quaternion))
                finalPose.Rotation = Quaternion.identity;

            TargetSample protect = default(TargetSample);
            if (focusTarget != null)
            {
                protect.Position = focusTarget.position;
                protect.Valid = true;
            }
            SolverMath.KeepMinDistance(ref finalPose, in protect, m_MinTargetDistance);

            m_LastPose = finalPose;
            m_HasPose = true;
            m_Output.Apply(in finalPose);

            int count = 0;
            if (focusMode == FocusMode.FixedDistance)
            {
                m_FocusBuffer[count++] = new FocusRequest
                {
                    Distance = focusDistanceHint,
                    Weight = 1f,
                    SourceId = 1
                };
            }
            else if (focusMode == FocusMode.Target && focusTarget != null)
            {
                m_FocusBuffer[count++] = new FocusRequest
                {
                    Distance = Mathf.Max(0.1f, Vector3.Distance(finalPose.Position, focusTarget.position)),
                    Weight = 1f,
                    SourceId = 2
                };
            }
            else if (focusMode == FocusMode.Hyperfocal)
            {
                m_FocusBuffer[count++] = new FocusRequest
                {
                    Distance = Mathf.Max(focusDistanceHint, 50f),
                    Weight = 1f,
                    SourceId = 3
                };
            }

            var state = m_FocusSolver.Evaluate(m_FocusBuffer, count, focusDistanceHint, dt, m_FocusProfile);
            finalPose.FocusDistance = state.Distance;
            m_LastPose = finalPose;
            m_FocusOutput.Apply(in state, m_FocusProfile);
        }

        public void ReleasePose(float dt)
        {
            // keep last pose; mixer may call when no active weight
        }

        public void BlendToGameCamera(float k)
        {
            if (m_GameVcam == null || !m_HasPose)
                return;
            Transform gameTr = m_GameVcam.transform;
            CameraPose blended = m_LastPose;
            blended.Position = Vector3.Lerp(m_LastPose.Position, gameTr.position, k);
            blended.Rotation = Quaternion.Slerp(m_LastPose.Rotation, gameTr.rotation, k);
            m_Output.Apply(in blended);
            m_LastPose = blended;
        }

        public void SetControlling(bool controlling)
        {
            m_Output.SetControlling(controlling);
        }
    }
}
