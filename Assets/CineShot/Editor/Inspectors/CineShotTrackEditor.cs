using CineShot.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CineShot.Editor
{
    [CustomEditor(typeof(CineShotTrack))]
    public sealed class CineShotTrackEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var track = (CineShotTrack)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Open Storyboard"))
                StoryboardWindow.Open();
            if (GUILayout.Button("Lay Out Tracks"))
            {
                var director = FindDirector(track);
                var seq = FindSequence();
                if (director != null && seq != null)
                    ShotTrackLayer.Reconcile(director, seq, ConflictPolicy.Ask);
                else
                    EditorUtility.DisplayDialog("CineShot", "需要场景中的 PlayableDirector 与 CineSequenceAsset。", "OK");
            }
            if (GUILayout.Button("Bake Thumbs"))
                ShotThumbnailBaker.RebakeAll();
            if (GUILayout.Button("Validate Bindings"))
                Validate(track);
        }

        static PlayableDirector FindDirector(TrackAsset track)
        {
            var timeline = track.timelineAsset;
            if (timeline == null)
                return null;
            var directors = Resources.FindObjectsOfTypeAll<PlayableDirector>();
            for (int i = 0; i < directors.Length; i++)
            {
                if (directors[i] != null && directors[i].playableAsset == timeline)
                    return directors[i];
            }
            return null;
        }

        static CineSequenceAsset FindSequence()
        {
            string[] guids = AssetDatabase.FindAssets("t:CineSequenceAsset");
            if (guids.Length == 0)
                return null;
            return AssetDatabase.LoadAssetAtPath<CineSequenceAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        static void Validate(CineShotTrack track)
        {
            int missing = 0;
            foreach (var c in track.GetClips())
            {
                var asset = c.asset as CineShotClip;
                if (asset == null || asset.shot == null)
                    missing++;
            }
            EditorUtility.DisplayDialog("CineShot", "Missing shot refs: " + missing, "OK");
        }
    }
}
