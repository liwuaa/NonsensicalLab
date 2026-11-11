using UnityEngine;

namespace CineShot.Core
{
    public sealed class TransformFocusProvider : IFocusProvider
    {
        public Transform Target;
        public Vector3 LocalOffset;
        public int SourceId;

        public bool TryEvaluate(in CameraPose pose, float dt, out FocusRequest request)
        {
            request = default(FocusRequest);
            request.SourceId = SourceId;
            if (Target == null)
            {
                request.Distance = 10f;
                request.Weight = 0f;
                return false;
            }
            Vector3 world = Target.TransformPoint(LocalOffset);
            request.Distance = Mathf.Max(0.1f, Vector3.Distance(pose.Position, world));
            request.Weight = 1f;
            return true;
        }
    }

    public sealed class ScreenPointFocusProvider : IFocusProvider
    {
        public Camera Camera;
        public Vector2 ViewportPoint = new Vector2(0.5f, 0.5f);
        public LayerMask Mask = ~0;
        public float MaxDistance = 100f;
        public float FallbackDistance = 10f;
        public int SourceId;

        public bool TryEvaluate(in CameraPose pose, float dt, out FocusRequest request)
        {
            request = default(FocusRequest);
            request.SourceId = SourceId;
            request.Distance = FallbackDistance;
            if (Camera == null)
            {
                request.Weight = 0f;
                return false;
            }
            Ray ray = Camera.ViewportPointToRay(new Vector3(ViewportPoint.x, ViewportPoint.y, 0f));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, MaxDistance, Mask, QueryTriggerInteraction.Ignore))
            {
                request.Distance = Mathf.Max(0.1f, hit.distance);
                request.Weight = 1f;
                return true;
            }
            request.Weight = 0f;
            return false;
        }
    }

    public sealed class FixedDistanceFocusProvider : IFocusProvider
    {
        public float Distance = 10f;
        public float Weight = 1f;
        public int SourceId;

        public bool TryEvaluate(in CameraPose pose, float dt, out FocusRequest request)
        {
            request = new FocusRequest
            {
                Distance = Mathf.Max(0.1f, Distance),
                Weight = Weight,
                SourceId = SourceId
            };
            return true;
        }
    }

    public sealed class AutoNearestFarthestProvider : IFocusProvider
    {
        public Transform[] Candidates;
        public bool PreferNearest = true;
        public float Hysteresis = 0.25f;
        public int SourceId;
        Transform m_Current;

        public bool TryEvaluate(in CameraPose pose, float dt, out FocusRequest request)
        {
            request = default(FocusRequest);
            request.SourceId = SourceId;
            request.Distance = 10f;
            if (Candidates == null || Candidates.Length == 0)
            {
                request.Weight = 0f;
                return false;
            }

            Transform best = null;
            float bestMetric = PreferNearest ? float.MaxValue : float.MinValue;
            for (int i = 0; i < Candidates.Length; i++)
            {
                var c = Candidates[i];
                if (c == null)
                    continue;
                float d = Vector3.Distance(pose.Position, c.position);
                if (PreferNearest)
                {
                    if (d < bestMetric)
                    {
                        bestMetric = d;
                        best = c;
                    }
                }
                else if (d > bestMetric)
                {
                    bestMetric = d;
                    best = c;
                }
            }

            if (best == null)
            {
                request.Weight = 0f;
                return false;
            }

            if (m_Current != null && m_Current != best)
            {
                float cur = Vector3.Distance(pose.Position, m_Current.position);
                if (Mathf.Abs(cur - bestMetric) < Hysteresis)
                    best = m_Current;
            }
            m_Current = best;
            request.Distance = Mathf.Max(0.1f, Vector3.Distance(pose.Position, best.position));
            request.Weight = 1f;
            return true;
        }
    }
}
