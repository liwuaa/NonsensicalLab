using System.Diagnostics;
using Unity.Profiling;
using UnityEngine;

namespace CineShot.Core
{
    public sealed class CineFrameProbe : MonoBehaviour
    {
        public int WarmupFrames = 200;
        public int SampleFrames = 600;
        public float ThresholdP95Ms = 0.10f;

        readonly Stopwatch m_Sw = new Stopwatch();
        ProfilerRecorder m_Gc;
        float[] m_Samples;
        int m_Frame;
        int m_Idx;
        bool m_Reported;

        public float LastP95 { get; private set; }
        public long LastGcTotal { get; private set; }

        void OnEnable()
        {
            m_Samples = new float[SampleFrames];
            m_Gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc");
            m_Frame = 0;
            m_Idx = 0;
            m_Reported = false;
            LastGcTotal = 0;
        }

        void OnDisable()
        {
            if (m_Gc.Valid)
                m_Gc.Dispose();
        }

        void Update()
        {
            if (m_Reported)
                return;
            if (m_Frame++ < WarmupFrames)
                return;
            if (m_Idx >= SampleFrames)
            {
                Report();
                enabled = false;
                return;
            }

            m_Sw.Restart();
            CinePerfProbe.BeginSolve();
            CinePerfProbe.EndSolve();
            m_Sw.Stop();

            float ms = (float)m_Sw.Elapsed.TotalMilliseconds;
            m_Samples[m_Idx++] = ms;
            CinePerfProbe.Record(ms);

            if (m_Gc.Valid)
            {
                long v = m_Gc.LastValue;
                LastGcTotal += v;
                if (v > 0)
                    UnityEngine.Debug.LogError("[Cine] GC.Alloc=" + v + "B @frame " + m_Frame);
            }
        }

        void Report()
        {
            m_Reported = true;
            var sorted = (float[])m_Samples.Clone();
            System.Array.Sort(sorted);
            float P(float q)
            {
                int i = Mathf.Clamp((int)(q * (sorted.Length - 1)), 0, sorted.Length - 1);
                return sorted[i];
            }
            LastP95 = P(0.95f);
            UnityEngine.Debug.Log(
                "[Cine] P50=" + P(0.50f).ToString("F4") +
                " P95=" + LastP95.ToString("F4") +
                " Max=" + P(1f).ToString("F4") +
                " ms, n=" + sorted.Length +
                ", GCTotal=" + LastGcTotal);
            if (LastP95 > ThresholdP95Ms)
                UnityEngine.Debug.LogError("[Cine] P95 超过阈值 " + ThresholdP95Ms + " ms");
        }
    }
}
