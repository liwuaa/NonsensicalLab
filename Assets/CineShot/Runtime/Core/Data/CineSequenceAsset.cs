using System;
using System.Collections.Generic;
using UnityEngine;

namespace CineShot.Core
{
    [Serializable]
    public class StoryboardRow
    {
        public string shotGuid;
        public int rowIndex;
        public double duration = 4.0;
        public double gap;
        public bool hasRigOverride;
        public RigKind rigOverride;
        public string targetTag;
        public double blendIn;
        public double blendOut;
        public float timeScale = 1f;
        public bool loop;
        public string note;
        public ShotBlendMode blendMode = ShotBlendMode.Cut;
    }

    [CreateAssetMenu(menuName = "CineShot/Sequence Asset", fileName = "Seq_")]
    public class CineSequenceAsset : ScriptableObject
    {
        public TimelineAssetRef timeline;
        public List<StoryboardRow> rows = new List<StoryboardRow>();

        [Serializable]
        public class TimelineAssetRef
        {
            public UnityEngine.Object asset;
        }
    }
}
