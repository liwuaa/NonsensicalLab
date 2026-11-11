#if CM_2_8_OR_NEWER && !CM_V3_OR_NEWER
using Cinemachine;
using UnityEngine;

namespace CineShot.Output
{
    public sealed class CM2LensWriter : ILensWriter
    {
        readonly CinemachineVirtualCamera m_Vcam;

        public CM2LensWriter(CinemachineVirtualCamera vcam)
        {
            m_Vcam = vcam;
        }

        public bool IsValid { get { return m_Vcam != null; } }
        public Transform Transform { get { return m_Vcam != null ? m_Vcam.transform : null; } }

        public int Priority
        {
            get { return m_Vcam != null ? m_Vcam.m_Priority : 0; }
            set { if (m_Vcam != null) m_Vcam.m_Priority = value; }
        }

        public bool SetPose(Vector3 position, Quaternion rotation)
        {
            if (m_Vcam == null) return false;
            m_Vcam.transform.SetPositionAndRotation(position, rotation);
            return true;
        }

        public bool SetLens(in LensDTO lens)
        {
            if (m_Vcam == null) return false;
            LensSettings l = m_Vcam.m_Lens;
            l.FieldOfView = lens.FieldOfView;
            l.NearClipPlane = lens.NearClipPlane;
            l.FarClipPlane = lens.FarClipPlane;
            l.Dutch = lens.Dutch;
            m_Vcam.m_Lens = l;
            return true;
        }

        public void SetEnabled(bool enabled)
        {
            if (m_Vcam != null)
                m_Vcam.enabled = enabled;
        }
    }
}
#endif
