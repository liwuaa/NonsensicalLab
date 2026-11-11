using CineShot.Core;
using UnityEngine;

namespace CineShot.Output
{
    public sealed class AdapterCameraOutput : ICameraOutput
    {
        readonly ILensWriter m_Writer;
        bool m_Controlling;
        int m_PlayingPriority = 100;
        int m_IdlePriority;

        public AdapterCameraOutput(ILensWriter writer)
        {
            m_Writer = writer;
            if (m_Writer != null)
                m_IdlePriority = m_Writer.Priority;
        }

        public bool SupportsDutch { get { return true; } }
        public bool SupportsPhysicalLens { get { return false; } }
        public Transform OutputTransform { get { return m_Writer != null ? m_Writer.Transform : null; } }
        public bool IsControlling { get { return m_Controlling; } }

        public int Priority
        {
            get { return m_Writer != null ? m_Writer.Priority : 0; }
            set { if (m_Writer != null) m_Writer.Priority = value; }
        }

        public void Bind(Camera fallbackCamera) { }

        public void Apply(in CameraPose pose)
        {
            if (m_Writer == null || !m_Writer.IsValid)
                return;
            m_Writer.SetPose(pose.Position, pose.Rotation);
            var lens = new LensDTO
            {
                FieldOfView = pose.Fov,
                NearClipPlane = pose.NearClip,
                FarClipPlane = pose.FarClip,
                Dutch = pose.Dutch
            };
            m_Writer.SetLens(in lens);
        }

        public void SetControlling(bool controlling)
        {
            m_Controlling = controlling;
            if (m_Writer == null)
                return;
            m_Writer.SetEnabled(controlling);
            m_Writer.Priority = controlling ? m_PlayingPriority : m_IdlePriority;
        }

        public static ICameraOutput CreateFromComponent(Component vcam)
        {
            if (vcam == null)
                return NullCameraOutput.Instance;

#if CM_V3_OR_NEWER
            var cm3 = vcam as Unity.Cinemachine.CinemachineCamera;
            if (cm3 != null)
                return new AdapterCameraOutput(new CM3LensWriter(cm3));
#elif CM_2_8_OR_NEWER
            var cm2 = vcam as Cinemachine.CinemachineVirtualCamera;
            if (cm2 != null)
                return new AdapterCameraOutput(new CM2LensWriter(cm2));
#endif
            return NullCameraOutput.Instance;
        }
    }
}
