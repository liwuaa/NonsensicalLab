using System;
using UnityEngine;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.OpenBox
{
    /// <summary>驱动 Pivot 折叠；编辑模式可预览。仅在数值变化时更新。</summary>
    [ExecuteAlways]
    public class FoldableCarton : MonoBehaviour
    {
        [Header("折叠 0=打开/压扁 1=闭合/成型")]
        [Range(0f, 1f)] [SerializeField,Label("侧壁")] private float m_wallFold = 1f;
        [Range(0f, 1f)] [SerializeField,Label("底盖")] private float m_bottomFold = 1f;
        [Range(0f, 1f)] [SerializeField,Label("顶盖")] private float m_topFold = 0f;

        [Tooltip("0 压扁 → 侧壁 → 底盖(短→长) → 顶盖(短→长) → 1 全闭")]
        [Range(0f, 1f)] [SerializeField,Label("主折叠")] private float m_masterFold = 0.55f;

        [SerializeField] private bool m_useMasterFold = true;

        [Header("编辑预览")]
        [SerializeField] private bool m_previewInEditMode = true;

        [Header("折痕绑定")]
        [SerializeField] private CartonFoldBinding[] m_bindings = Array.Empty<CartonFoldBinding>();

        // 热路径缓存（避免每帧查数组字段 / Normalize）
        Transform[] _pivots;
        Vector3[] _axes;
        float[] _flat;
        float[] _closed;
        CartonFoldChannel[] _channels;
        int _count;
        float _lastWall = -1f, _lastBottom = -1f, _lastTop = -1f;

        public float WallFold
        {
            get => m_wallFold;
            set { m_wallFold = Mathf.Clamp01(value); m_useMasterFold = false; ApplyFolds(); }
        }

        public float BottomFold
        {
            get => m_bottomFold;
            set { m_bottomFold = Mathf.Clamp01(value); m_useMasterFold = false; ApplyFolds(); }
        }

        public float TopFold
        {
            get => m_topFold;
            set { m_topFold = Mathf.Clamp01(value); m_useMasterFold = false; ApplyFolds(); }
        }

        public float MasterFold { get => m_masterFold; set => SetMasterFold(value); }
        public bool UseMasterFold { get => m_useMasterFold; set => m_useMasterFold = value; }

        public float BottomShortFold => ShortPhase(m_bottomFold);
        public float BottomLongFold => LongPhase(m_bottomFold);
        public float TopShortFold => ShortPhase(m_topFold);
        public float TopLongFold => LongPhase(m_topFold);
        public int BindingCount => _count > 0 ? _count : (m_bindings != null ? m_bindings.Length : 0);

        private void OnEnable() => RebuildCache(forceApply: true);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!m_previewInEditMode && !Application.isPlaying)
                return;
            EditorApplication.delayCall -= DeferredApply;
            EditorApplication.delayCall += DeferredApply;
        }

        private void DeferredApply()
        {
            EditorApplication.delayCall -= DeferredApply;
            if (this == null)
                return;
            if (!m_previewInEditMode && !Application.isPlaying)
                return;
            RebuildCache(forceApply: true);
            SceneView.RepaintAll();
        }
#endif

        public void SetBindings(CartonFoldBinding[] bindings)
        {
            m_bindings = bindings ?? Array.Empty<CartonFoldBinding>();
            RebuildCache(forceApply: true);
        }

        public void SetFolds(float wall, float bottom, float top)
        {
            m_useMasterFold = false;
            m_wallFold = Mathf.Clamp01(wall);
            m_bottomFold = Mathf.Clamp01(bottom);
            m_topFold = Mathf.Clamp01(top);
            ApplyFolds();
        }

        public void SetMasterFold(float value)
        {
            m_useMasterFold = true;
            m_masterFold = Mathf.Clamp01(value);
            ResolveMaster();
            ApplyFolds();
        }

        public void ApplyFolds()
        {
            if (m_useMasterFold)
                ResolveMaster();

            if (_count == 0)
            {
                RebuildCache(forceApply: false);
                if (_count == 0)
                    return;
            }

            // 无变化则跳过（滑条/键盘连按时减少无意义的 Transform 写入）
            if (Approximately(_lastWall, m_wallFold) &&
                Approximately(_lastBottom, m_bottomFold) &&
                Approximately(_lastTop, m_topFold))
                return;

            _lastWall = m_wallFold;
            _lastBottom = m_bottomFold;
            _lastTop = m_topFold;

            float wall = m_wallFold;
            float bShort = ShortPhase(m_bottomFold);
            float bLong = LongPhase(m_bottomFold);
            float tShort = ShortPhase(m_topFold);
            float tLong = LongPhase(m_topFold);

            for (int i = 0; i < _count; i++)
            {
                Transform pivot = _pivots[i];
                if (pivot == null)
                    continue;

                float t = ChannelT(_channels[i], wall, bShort, bLong, tShort, tLong);
                float angle = Mathf.Lerp(_flat[i], _closed[i], t);
                pivot.localRotation = Quaternion.AngleAxis(angle, _axes[i]);
            }
        }

        private void RebuildCache(bool forceApply)
        {
            _lastWall = _lastBottom = _lastTop = -1f;

            if (m_bindings == null || m_bindings.Length == 0)
            {
                _count = 0;
                return;
            }

            int n = m_bindings.Length;
            if (_pivots == null || _pivots.Length < n)
            {
                _pivots = new Transform[n];
                _axes = new Vector3[n];
                _flat = new float[n];
                _closed = new float[n];
                _channels = new CartonFoldChannel[n];
            }

            int write = 0;
            for (int i = 0; i < n; i++)
            {
                var b = m_bindings[i];
                if (b == null || b.pivot == null)
                    continue;
                _pivots[write] = b.pivot;
                _axes[write] = b.localAxis.sqrMagnitude > 1e-8f ? b.localAxis.normalized : Vector3.up;
                _flat[write] = b.flatAngle;
                _closed[write] = b.closedAngle;
                _channels[write] = b.channel;
                write++;
            }

            _count = write;
            if (forceApply)
                ApplyFolds();
        }

        private void ResolveMaster()
        {
            if (!m_useMasterFold)
                return;
            float m = m_masterFold;
            m_wallFold = Mathf.Clamp01(m / 0.4f);
            m_bottomFold = Mathf.Clamp01((m - 0.4f) / 0.3f);
            m_topFold = Mathf.Clamp01((m - 0.7f) / 0.3f);
        }

        private static float ChannelT(
            CartonFoldChannel channel,
            float wall, float bShort, float bLong, float tShort, float tLong)
        {
            switch (channel)
            {
                case CartonFoldChannel.WallAcute:
                case CartonFoldChannel.WallObtuse:
                    return wall;
                case CartonFoldChannel.BottomShort: return bShort;
                case CartonFoldChannel.BottomLong: return bLong;
                case CartonFoldChannel.TopShort: return tShort;
                case CartonFoldChannel.TopLong: return tLong;
                default: return 0f;
            }
        }

        private static float ShortPhase(float fold) => Mathf.Clamp01(fold * 2f);
        private static float LongPhase(float fold) => Mathf.Clamp01(fold * 2f - 1f);
        private static bool Approximately(float a, float b) => Mathf.Abs(a - b) < 1e-5f;
    }
}
