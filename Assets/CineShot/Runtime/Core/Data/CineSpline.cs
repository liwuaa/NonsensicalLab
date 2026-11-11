using UnityEngine;

namespace CineShot.Core
{
    [CreateAssetMenu(menuName = "CineShot/Spline Asset", fileName = "CineSpline_")]
    public class CineSpline : ScriptableObject
    {
        public Vector3[] knots = new Vector3[]
        {
            new Vector3(-4f, 1.6f, -3f),
            new Vector3(-1f, 1.8f, -4f),
            new Vector3(1f, 1.8f, -4f),
            new Vector3(4f, 1.6f, -3f)
        };
        public float tension = 0.5f;

        public Vector3 Evaluate(float u)
        {
            if (knots == null || knots.Length == 0)
                return Vector3.zero;
            if (knots.Length == 1)
                return knots[0];

            u = Mathf.Clamp01(u);
            int n = knots.Length;
            float scaled = u * (n - 1);
            int i = Mathf.Min((int)scaled, n - 2);
            float t = scaled - i;

            Vector3 p0 = knots[Mathf.Max(i - 1, 0)];
            Vector3 p1 = knots[i];
            Vector3 p2 = knots[i + 1];
            Vector3 p3 = knots[Mathf.Min(i + 2, n - 1)];
            return CatmullRom(p0, p1, p2, p3, t, tension);
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float tension)
        {
            float s = (1f - tension) * 0.5f;
            Vector3 m1 = (p2 - p0) * s;
            Vector3 m2 = (p3 - p1) * s;
            float t2 = t * t;
            float t3 = t2 * t;
            return (2f * t3 - 3f * t2 + 1f) * p1
                 + (t3 - 2f * t2 + t) * m1
                 + (-2f * t3 + 3f * t2) * p2
                 + (t3 - t2) * m2;
        }
    }
}
