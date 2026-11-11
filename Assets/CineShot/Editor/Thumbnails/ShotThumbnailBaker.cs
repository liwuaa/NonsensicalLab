using System.Collections.Generic;
using System.IO;
using CineShot.Core;
using UnityEditor;
using UnityEngine;

namespace CineShot.Editor
{
    public static class ShotThumbnailBaker
    {
        const string CacheRoot = "Library/CineShotThumbs";
        const int BakerVersion = 1;
        static readonly Queue<ShotAsset> s_Queue = new Queue<ShotAsset>();
        static readonly HashSet<string> s_Queued = new HashSet<string>();
        static bool s_Hooked;
        static double m_BudgetMs = 8.0;

        public static Texture2D GetOrRequest(ShotAsset shot, int w, int h)
        {
            if (shot == null)
                return null;
            EnsureHook();
            string key = CacheKey(shot, w, h);
            string path = Path.Combine(CacheRoot, key + ".png");
            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(bytes);
                return tex;
            }
            Enqueue(shot);
            return Placeholder(shot, w, h);
        }

        public static void Enqueue(ShotAsset shot)
        {
            if (shot == null || string.IsNullOrEmpty(shot.Id))
                return;
            if (!s_Queued.Add(shot.Id))
                return;
            s_Queue.Enqueue(shot);
            EnsureHook();
        }

        public static void RebakeAll()
        {
            ShotLibrary.EnsureCache();
            var all = ShotLibrary.All;
            for (int i = 0; i < all.Count; i++)
                Enqueue(all[i]);
        }

        static void EnsureHook()
        {
            if (s_Hooked)
                return;
            EditorApplication.update += Tick;
            s_Hooked = true;
        }

        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            if (s_Queue.Count == 0)
                return;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int started = 0;
            while (s_Queue.Count > 0 && started < 2 && sw.Elapsed.TotalMilliseconds < m_BudgetMs)
            {
                var shot = s_Queue.Dequeue();
                s_Queued.Remove(shot != null ? shot.Id : null);
                BakeOne(shot, 192, 108);
                started++;
            }
        }

        static void BakeOne(ShotAsset shot, int w, int h)
        {
            if (shot == null)
                return;
            if (!Directory.Exists(CacheRoot))
                Directory.CreateDirectory(CacheRoot);

            PreviewRenderUtility pru = null;
            try
            {
                pru = new PreviewRenderUtility();
                pru.cameraFieldOfView = shot.fov;
                var rect = new Rect(0, 0, w, h);
                pru.BeginPreview(rect, GUIStyle.none);
                pru.camera.transform.position = new Vector3(0f, 1.6f, -4f);
                pru.camera.transform.LookAt(Vector3.up * 1.2f);
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.hideFlags = HideFlags.HideAndDontSave;
                pru.AddSingleGO(cube);
                pru.Render(true);
                var tex = pru.EndPreview() as Texture2D;
                Object.DestroyImmediate(cube);
                if (tex == null)
                    return;
                string path = Path.Combine(CacheRoot, CacheKey(shot, w, h) + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[CineShot] thumbnail bake failed: " + ex.Message);
            }
            finally
            {
                if (pru != null)
                    pru.Cleanup();
            }
        }

        static string CacheKey(ShotAsset shot, int w, int h)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (shot.Id != null ? shot.Id.GetHashCode() : 0);
                hash = hash * 31 + (int)shot.rigKind;
                hash = hash * 31 + shot.fov.GetHashCode();
                hash = hash * 31 + BakerVersion;
                hash = hash * 31 + w;
                hash = hash * 31 + h;
                return shot.Id + "_" + (hash & 0x7fffffff).ToString("x8");
            }
        }

        static Texture2D Placeholder(ShotAsset shot, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var cols = new Color[w * h];
            for (int i = 0; i < cols.Length; i++)
                cols[i] = new Color(0.2f, 0.2f, 0.22f, 1f);
            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }
    }
}
