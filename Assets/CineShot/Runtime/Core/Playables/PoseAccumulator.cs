using UnityEngine;

namespace CineShot.Core
{
    public struct PoseAccumulator
    {
        Vector3 m_Pos;
        Vector4 m_Rot;
        float m_Fov;
        float m_Dutch;
        float m_FocusDiopter;
        float m_Near;
        float m_Far;
        float m_Wsum;
        bool m_HasNear;

        public void Add(in CameraPose pose, float w)
        {
            if (w <= 0f)
                return;
            m_Pos += pose.Position * w;
            Quaternion q = pose.Rotation;
            if (m_Wsum > 0f && Vector4.Dot(m_Rot, new Vector4(q.x, q.y, q.z, q.w)) < 0f)
                q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
            m_Rot += new Vector4(q.x, q.y, q.z, q.w) * w;
            m_Fov += pose.Fov * w;
            m_Dutch += pose.Dutch * w;
            float d = Mathf.Max(0.1f, pose.FocusDistance);
            m_FocusDiopter += (1f / d) * w;
            if (!m_HasNear)
            {
                m_Near = pose.NearClip;
                m_Far = pose.FarClip;
                m_HasNear = true;
            }
            else
            {
                m_Near = Mathf.Max(m_Near, pose.NearClip);
                m_Far = Mathf.Min(m_Far, pose.FarClip);
            }
            m_Wsum += w;
        }

        public bool TryBuild(out CameraPose pose)
        {
            pose = CameraPose.Identity;
            if (m_Wsum <= 1e-6f)
                return false;
            float inv = 1f / m_Wsum;
            pose.Position = m_Pos * inv;
            Vector4 r = m_Rot * inv;
            float mag = Mathf.Sqrt(r.x * r.x + r.y * r.y + r.z * r.z + r.w * r.w);
            if (mag > 1e-6f)
            {
                r /= mag;
                pose.Rotation = new Quaternion(r.x, r.y, r.z, r.w);
            }
            pose.Fov = m_Fov * inv;
            pose.Dutch = m_Dutch * inv;
            float diopter = m_FocusDiopter * inv;
            pose.FocusDistance = diopter > 1e-6f ? 1f / diopter : 10f;
            pose.NearClip = m_Near;
            pose.FarClip = Mathf.Max(m_Far, m_Near + 0.1f);
            return true;
        }
    }
}
