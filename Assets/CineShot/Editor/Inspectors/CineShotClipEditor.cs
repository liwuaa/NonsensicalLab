using CineShot.Core;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CineShot.Editor
{
    [CustomTimelineEditor(typeof(CineShotClip))]
    public sealed class CineShotClipEditor : ClipEditor
    {
        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var asset = clip.asset as CineShotClip;
            if (asset == null || asset.shot == null)
                return;
            var rect = region.position;
            rect.width = 48f;
            GUI.Label(rect, asset.shot.rigKind.ToString().Substring(0, 1));
        }
    }
}
