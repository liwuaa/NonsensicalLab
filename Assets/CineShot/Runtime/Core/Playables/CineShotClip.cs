using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CineShot.Core
{
    [Serializable]
    public class CineShotClip : PlayableAsset, ITimelineClipAsset
    {
        public ShotAsset shot;
        public ShotOverride overrides;
        public ExposedReference<Transform> lookAt;
        public ExposedReference<Transform> follow;
        public EasingAsset easeIn;
        public EasingAsset easeOut;
        public ShotBlendMode blendMode = ShotBlendMode.Blend;
        public CineSpline spline;
        public string matchKey;
        public string sourceHash;
        public double lastAppliedDuration;

        public ClipCaps clipCaps
        {
            get { return ClipCaps.Blending | ClipCaps.SpeedMultiplier; }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CineShotBehaviour>.Create(graph);
            playable.GetBehaviour().Setup(this);
            return playable;
        }

        public override double duration
        {
            get
            {
                if (shot != null && shot.defaultDuration > 0.1f)
                    return shot.defaultDuration;
                return 4.0;
            }
        }
    }
}
