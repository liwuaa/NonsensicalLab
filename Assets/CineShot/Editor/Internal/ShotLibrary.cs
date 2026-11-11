using System;
using System.Collections.Generic;
using CineShot.Core;
using UnityEditor;
using UnityEngine;

namespace CineShot.Editor
{
    public struct ShotQuery
    {
        public string text;
        public RigKind? rig;
        public bool favoritesOnly;
        public bool unusedOnly;
        public bool brokenOnly;
    }

    public static class ShotLibrary
    {
        static readonly List<ShotAsset> s_Cache = new List<ShotAsset>();
        static double s_LastRefresh;

        public static IReadOnlyList<ShotAsset> All
        {
            get
            {
                EnsureCache();
                return s_Cache;
            }
        }

        public static void Invalidate()
        {
            s_Cache.Clear();
            s_LastRefresh = 0;
        }

        public static void EnsureCache()
        {
            if (s_Cache.Count > 0 && EditorApplication.timeSinceStartup - s_LastRefresh < 2.0)
                return;
            s_Cache.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ShotAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<ShotAsset>(path);
                if (asset != null)
                    s_Cache.Add(asset);
            }
            s_LastRefresh = EditorApplication.timeSinceStartup;
        }

        public static List<ShotAsset> Query(ShotQuery q)
        {
            EnsureCache();
            var hits = new List<ShotAsset>();
            string text = q.text != null ? q.text.Trim() : string.Empty;
            for (int i = 0; i < s_Cache.Count; i++)
            {
                var s = s_Cache[i];
                if (q.rig.HasValue && s.rigKind != q.rig.Value)
                    continue;
                if (!string.IsNullOrEmpty(text))
                {
                    string name = string.IsNullOrEmpty(s.displayName) ? s.name : s.displayName;
                    if (name.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0
                        && (s.Id == null || s.Id.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0))
                        continue;
                }
                hits.Add(s);
            }
            return hits;
        }

        public static ShotAsset Create(string folder, string fileName, RigKind kind)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }
            var asset = ScriptableObject.CreateInstance<ShotAsset>();
            asset.displayName = fileName;
            asset.rigKind = kind;
            asset.rigParams = RigParams.DefaultFor(kind);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + fileName + ".asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Invalidate();
            return asset;
        }

        public static ShotAsset DuplicateAsVariant(ShotAsset source)
        {
            if (source == null)
                return null;
            string path = AssetDatabase.GetAssetPath(source);
            string dir = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var clone = UnityEngine.Object.Instantiate(source);
            clone.parentGuid = source.Id;
            clone.displayName = source.displayName + "_v";
            string newPath = AssetDatabase.GenerateUniqueAssetPath(dir + "/" + clone.displayName + ".asset");
            AssetDatabase.CreateAsset(clone, newPath);
            AssetDatabase.SaveAssets();
            Invalidate();
            return clone;
        }

        public static int CountReferences(ShotAsset shot)
        {
            if (shot == null || string.IsNullOrEmpty(shot.Id))
                return 0;
            int count = 0;
            string[] guids = AssetDatabase.FindAssets("t:CineSequenceAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                var seq = AssetDatabase.LoadAssetAtPath<CineSequenceAsset>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (seq == null || seq.rows == null)
                    continue;
                for (int r = 0; r < seq.rows.Count; r++)
                {
                    if (seq.rows[r] != null && seq.rows[r].shotGuid == shot.Id)
                        count++;
                }
            }
            return count;
        }
    }

    class ShotLibraryPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] movedTo, string[] movedFrom)
        {
            ShotLibrary.Invalidate();
        }
    }
}
