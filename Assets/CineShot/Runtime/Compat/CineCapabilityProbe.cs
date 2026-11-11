using System;
using UnityEngine;

namespace CineShot.Compat
{
    public static class CineCapabilityProbe
    {
        static CineCapabilities s_Current;
        static bool s_Ran;

        public static CineCapabilities Current
        {
            get
            {
                if (!s_Ran)
                    RunOnce();
                return s_Current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeInit()
        {
            RunOnce();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void EditorInit()
        {
            UnityEditor.EditorApplication.delayCall += RunOnce;
        }
#endif

        public static void RunOnce()
        {
            try
            {
                s_Current = Probe();
                s_Ran = true;
                EmitSummary(s_Current);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Cine 兼容层] 能力探针失败，已降级为保守能力位: " + ex.Message);
                s_Current = new CineCapabilities(false, false, false, false, false, false, false, 0, 0, 0);
                s_Ran = true;
            }
        }

        static CineCapabilities Probe()
        {
#if CM_V3_OR_NEWER
            bool cm = true;
            bool v3 = true;
            int major = 3, minor = 1, patch = 0;
#elif CM_2_8_OR_NEWER
            bool cm = true;
            bool v3 = false;
            int major = 2, minor = 8, patch = 0;
#else
            bool cm = false;
            bool v3 = false;
            int major = 0, minor = 0, patch = 0;
#endif
            bool lens = cm;
            bool brain = TypeExists("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine")
                         || TypeExists("Cinemachine.CinemachineBrain, Cinemachine");
            bool impulse = TypeExists("Unity.Cinemachine.CinemachineImpulseSource, Unity.Cinemachine")
                           || TypeExists("Cinemachine.CinemachineImpulseSource, Cinemachine");
            bool track = TypeExists("Unity.Cinemachine.CinemachineTrack, Unity.Cinemachine")
                         || TypeExists("Cinemachine.Timeline.CinemachineTrack, Cinemachine");
            bool dof = DofChannelProbe.HasWritableDofChannel();

            return new CineCapabilities(cm, v3, lens, brain, dof, impulse, track, major, minor, patch);
        }

        static bool TypeExists(string fullName)
        {
            return Type.GetType(fullName) != null;
        }

        static void EmitSummary(in CineCapabilities c)
        {
            if (!c.CinemachinePresent)
            {
                WarnOnce.Log("cm.missing", "[Cine 兼容层] 未检测到 Cinemachine，运镜输出将走空实现。");
                return;
            }

            if (!c.CinemachineV3)
            {
                WarnOnce.Log("cm.v2",
                    "[Cine 兼容层] 检测到 Cinemachine 2.8（未检测到 3.x）。部分能力已自动降级，运镜与预览不受影响。\n" +
                    "  · 相机目标结构（CameraTarget） → 改为直接读取 Transform\n" +
                    "  · 相机组件扩展（Extension 管道） → 本框架不使用，忽略\n" +
                    "  · CM3 镜头轨道绑定 → 已跳过，使用自研 Shot Track\n" +
                    "打开 Tools/Cine/兼容性与能力报告 可查看完整清单。");
            }

            if (!c.UrpVolumeDof)
            {
                WarnOnce.Log("dof.null",
                    "[Cine 兼容层] 当前渲染管线无可写 URP DepthOfField 通道，焦点子系统走 Null。");
            }
        }
    }
}
