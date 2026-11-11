using System;
using UnityEngine;

namespace CineShot.Core
{
    [Serializable]
    public struct RigParams
    {
        public Vector3 A;
        public Vector3 B;
        public float P0;
        public float P1;
        public float P2;
        public float P3;
        public int I0;
        public int I1;

        public static RigParams DefaultFor(RigKind kind)
        {
            var p = new RigParams();
            switch (kind)
            {
                case RigKind.Static:
                    p.A = new Vector3(0f, 1.6f, -3.5f);
                    break;
                case RigKind.DollyTrack:
                    p.A = new Vector3(-4f, 1.6f, -3f);
                    p.B = new Vector3(4f, 1.6f, -3f);
                    p.P0 = 0.5f;
                    p.P1 = 0.8f;
                    p.P2 = 1.6f;
                    break;
                case RigKind.Orbit:
                    p.P0 = 45f;
                    p.P1 = 24f;
                    p.P2 = 4f;
                    p.P3 = 1.2f;
                    p.A = new Vector3(40f, 0f, 0f);
                    break;
                case RigKind.PushPull:
                    p.P0 = 6f;
                    p.P1 = 2.4f;
                    p.P2 = 1.6f;
                    p.P3 = 1f;
                    p.A = new Vector3(34f, 0f, 0f);
                    break;
                case RigKind.Crane:
                    p.P0 = 0.9f;
                    p.P1 = 3.2f;
                    p.P2 = 3f;
                    p.P3 = 2f;
                    p.I0 = 1;
                    break;
                case RigKind.Shoulder:
                    p.I0 = 1;
                    p.P0 = 0.35f;
                    p.P1 = 1.45f;
                    p.P2 = 0f;
                    p.P3 = 0f;
                    break;
                case RigKind.Handheld:
                    p.P0 = 0.02f;
                    p.P1 = 0.7f;
                    p.P2 = 0.25f;
                    p.P3 = 0.15f;
                    break;
                case RigKind.SplineDolly:
                    p.P0 = 0.5f;
                    p.P1 = 1f;
                    p.P2 = 0.4f;
                    p.I0 = 4;
                    break;
            }
            return p;
        }
    }

    [Serializable]
    public struct TargetBinding
    {
        public TargetBindingMode mode;
        public Vector3 lookAtOffset;
        public Vector3 followOffset;
        public float damping;

        public static TargetBinding LookAtDefault
        {
            get
            {
                return new TargetBinding
                {
                    mode = TargetBindingMode.LookAt,
                    lookAtOffset = new Vector3(0f, 1.6f, 0f),
                    followOffset = Vector3.zero,
                    damping = 0f
                };
            }
        }
    }

    [Serializable]
    public struct HandheldLayer
    {
        public bool enabled;
        public RigParams parameters;
    }

    [Serializable]
    public struct MatchPoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public float fov;
        public bool valid;
    }
}
