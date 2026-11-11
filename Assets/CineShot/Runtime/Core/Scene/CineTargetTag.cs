using UnityEngine;

namespace CineShot.Core
{
    [DisallowMultipleComponent]
    public sealed class CineTargetTag : MonoBehaviour
    {
        [SerializeField] string m_Id = "Target";
        [SerializeField] int m_Priority;

        public string Id { get { return m_Id; } }
        public int Priority { get { return m_Priority; } }

        void OnEnable()
        {
            CineTargets.Register(this);
        }

        void OnDisable()
        {
            CineTargets.Unregister(this);
        }
    }

    public sealed class CineTargets
    {
        const int k_Max = 64;
        readonly CineTargetTag[] m_Tags = new CineTargetTag[k_Max];
        int m_Count;

        static CineTargets s_Active;

        public static void SetActive(CineTargets targets)
        {
            s_Active = targets;
        }

        public static void Register(CineTargetTag tag)
        {
            if (s_Active != null)
                s_Active.RegisterInternal(tag);
        }

        public static void Unregister(CineTargetTag tag)
        {
            if (s_Active != null)
                s_Active.UnregisterInternal(tag);
        }

        public void Activate()
        {
            s_Active = this;
        }

        public void Deactivate()
        {
            if (s_Active == this)
                s_Active = null;
            m_Count = 0;
        }

        void RegisterInternal(CineTargetTag tag)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Tags[i] == tag)
                    return;
            }
            if (m_Count >= k_Max)
                return;
            m_Tags[m_Count++] = tag;
        }

        void UnregisterInternal(CineTargetTag tag)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Tags[i] != tag)
                    continue;
                m_Tags[i] = m_Tags[m_Count - 1];
                m_Tags[m_Count - 1] = null;
                m_Count--;
                return;
            }
        }

        public bool TryResolve(string id, out Transform transform)
        {
            transform = null;
            if (string.IsNullOrEmpty(id))
                return false;

            int bestPri = int.MinValue;
            CineTargetTag best = null;
            for (int i = 0; i < m_Count; i++)
            {
                var t = m_Tags[i];
                if (t == null || t.Id != id)
                    continue;
                if (t.Priority >= bestPri)
                {
                    bestPri = t.Priority;
                    best = t;
                }
            }
            if (best == null)
                return false;
            transform = best.transform;
            return true;
        }

        public TargetSample Sample(string id, Vector3 offset, float dt, ref Vector3 lastPos, ref bool hasLast)
        {
            Transform tr;
            if (!TryResolve(id, out tr) || tr == null)
                return default(TargetSample);

            Vector3 pos = tr.TransformPoint(offset);
            Vector3 vel = Vector3.zero;
            if (hasLast && dt > 1e-5f)
                vel = (pos - lastPos) / dt;
            lastPos = pos;
            hasLast = true;
            return new TargetSample
            {
                Position = pos,
                Velocity = vel,
                Rotation = tr.rotation,
                Valid = true
            };
        }
    }
}
