using UnityEngine;

namespace CineShot.Core
{
    public sealed class FocusSolver
    {
        FocusState m_State;
        float m_PullElapsed;
        float m_PullFrom;
        float m_PullTo;
        bool m_Pulling;
        int m_LastSource = int.MinValue;

        public FocusState State { get { return m_State; } }

        public void Reset(float distance)
        {
            m_State.Distance = Mathf.Max(0.1f, distance);
            m_State.Velocity = 0f;
            m_State.Pulling = false;
            m_Pulling = false;
            m_LastSource = int.MinValue;
        }

        public FocusState Evaluate(FocusRequest[] requests, int count, float baseDistance, float dt, FocusProfile profile)
        {
            float damping = profile != null ? Mathf.Max(0.01f, profile.damping) : 6f;
            float maxSpeed = profile != null ? Mathf.Max(0.01f, profile.maxFocusSpeed) : 20f;
            float pullDur = profile != null ? Mathf.Max(0.01f, profile.pullDuration) : 0.5f;

            float wSum = 0f;
            float logSum = 0f;
            int dominantSource = m_LastSource;
            float dominantW = -1f;

            for (int i = 0; i < count; i++)
            {
                float w = requests[i].Weight;
                if (w <= 0f)
                    continue;
                float d = Mathf.Max(0.1f, requests[i].Distance);
                wSum += w;
                logSum += w * Mathf.Log(d);
                if (w > dominantW)
                {
                    dominantW = w;
                    dominantSource = requests[i].SourceId;
                }
            }

            float target;
            if (wSum < 1e-5f)
            {
                target = Mathf.Max(0.1f, baseDistance);
            }
            else if (wSum < 1f)
            {
                float remain = 1f - wSum;
                logSum += remain * Mathf.Log(Mathf.Max(0.1f, baseDistance));
                wSum = 1f;
                target = Mathf.Exp(logSum / wSum);
            }
            else
            {
                target = Mathf.Exp(logSum / wSum);
            }

            if (dominantSource != m_LastSource && m_LastSource != int.MinValue)
            {
                m_Pulling = true;
                m_PullElapsed = 0f;
                m_PullFrom = m_State.Distance;
                m_PullTo = target;
            }
            m_LastSource = dominantSource;

            if (m_Pulling)
            {
                m_PullElapsed += dt;
                float u = Mathf.Clamp01(m_PullElapsed / pullDur);
                float s = u * u * (3f - 2f * u);
                target = Mathf.Lerp(m_PullFrom, m_PullTo, s);
                if (u >= 1f)
                    m_Pulling = false;
            }

            float alpha = SolverMath.FrameRateIndependentAlpha(dt, 1f / damping);
            float damped = Mathf.Lerp(m_State.Distance, target, alpha);
            float maxDelta = maxSpeed * dt;
            float delta = Mathf.Clamp(damped - m_State.Distance, -maxDelta, maxDelta);
            float next = Mathf.Max(0.1f, m_State.Distance + delta);
            m_State.Velocity = dt > 1e-5f ? (next - m_State.Distance) / dt : 0f;
            m_State.Distance = next;
            m_State.Pulling = m_Pulling;
            return m_State;
        }
    }
}
