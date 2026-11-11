using System.IO;
using System.Text;
using CineShot.Compat;
using CineShot.Core;
using UnityEditor;
using UnityEngine;

namespace CineShot.Editor
{
    public sealed class CineCapabilityWindow : EditorWindow
    {
        [MenuItem("Tools/Cine/兼容性与能力报告")]
        public static void Open()
        {
            GetWindow<CineCapabilityWindow>("Cine 能力报告");
        }

        void OnEnable()
        {
            CineCapabilityProbe.RunOnce();
        }

        void OnGUI()
        {
            var c = CineCapabilityProbe.Current;
            EditorGUILayout.LabelField("Cinemachine " + c.Version + " · " + (c.CinemachineV3 ? "CM3" : "CM2"), EditorStyles.boldLabel);
            DrawRow("版本通道", c.CinemachineV3 ? "CM3" : "CM2", "决定走哪份适配器", "CM2 下按降级清单");
            DrawRow("Camera Lens 写入", c.CameraLensWrite ? "原生" : "降级", "能否直接改 FOV / 裁剪面", "沿用 CM 侧默认镜头");
            DrawRow("URP 景深", c.UrpVolumeDof ? "原生" : "Null", "能否用 Volume 写 DepthOfField", "无景深，焦点子系统空转");
            DrawRow("Impulse", c.ImpulseModule ? "原生" : "缺失", "能否接收震屏脉冲", "仅剩自研抖动");
            DrawRow("Timeline CM 轨道", c.TimelineCmTrack ? "存在" : "缺失", "工程内是否有 CM 轨道资产", "我们不用它，仅共存检测");
            EditorGUILayout.Space();
            if (GUILayout.Button("复制诊断信息"))
                EditorGUIUtility.systemCopyBuffer = BuildReportMarkdown(c);
            if (GUILayout.Button("生成并保存能力报告"))
                CapabilityReportGenerator.Generate();
        }

        static void DrawRow(string name, string state, string plain, string impact)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("[" + state + "] " + name, GUILayout.Width(220));
                EditorGUILayout.LabelField(plain + " → " + impact);
            }
        }

        public static string BuildReportMarkdown(in CineCapabilities c)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# CineShot Capability Report");
            sb.AppendLine();
            sb.AppendLine("- Unity: " + Application.unityVersion);
            sb.AppendLine("- Cinemachine: " + c.Version + (c.CinemachineV3 ? " (CM3)" : " (CM2)"));
            sb.AppendLine("- Pipeline: " + (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
                ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name
                : "Built-in"));
            sb.AppendLine("- LensWrite: " + c.CameraLensWrite);
            sb.AppendLine("- UrpVolumeDof: " + c.UrpVolumeDof);
            sb.AppendLine("- Impulse: " + c.ImpulseModule);
            sb.AppendLine("- TimelineCmTrack: " + c.TimelineCmTrack);
            sb.AppendLine("- Generated: " + System.DateTime.Now.ToString("s"));
            return sb.ToString();
        }
    }

    public static class CapabilityReportGenerator
    {
        [MenuItem("Tools/Cine/生成能力报告")]
        public static void Generate()
        {
            CineCapabilityProbe.RunOnce();
            var c = CineCapabilityProbe.Current;
            string pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? "urp" : "builtin";
            string file = "Assets/CineShot/Docs/capability-report-" + Application.unityVersion.Replace(" ", "_")
                          + "-cm" + c.Version + "-" + pipeline + ".md";
            string dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(file, CineCapabilityWindow.BuildReportMarkdown(in c), Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log("[CineShot] capability report written: " + file);
        }
    }

    public static class CineMenus
    {
        [MenuItem("Tools/Cine/Validate References")]
        public static void ValidateReferences()
        {
            ShotLibrary.EnsureCache();
            int broken = 0;
            var all = ShotLibrary.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null || string.IsNullOrEmpty(all[i].Id))
                    broken++;
            }
            EditorUtility.DisplayDialog("CineShot", "Shot assets: " + all.Count + ", broken: " + broken, "OK");
        }

        [MenuItem("Tools/Cine/Rebake Thumbnails")]
        public static void Rebake()
        {
            ShotThumbnailBaker.RebakeAll();
        }

        [MenuItem("Tools/Cine/Duplicate As Variant %#&d")]
        public static void DuplicateVariant()
        {
            var shot = Selection.activeObject as ShotAsset;
            if (shot == null)
            {
                EditorUtility.DisplayDialog("CineShot", "请先选中 ShotAsset。", "OK");
                return;
            }
            Selection.activeObject = ShotLibrary.DuplicateAsVariant(shot);
        }
    }
}
