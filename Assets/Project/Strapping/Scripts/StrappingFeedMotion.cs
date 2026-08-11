using UnityEngine;
using NaughtyAttributes;

namespace Project.Strapping
{
    /// <summary>
    /// 打轧带送带运动：模拟「送带 → 张紧 → 热熔 → 切断」周期。
    /// 仅驱动 FeedTip / 机头位姿；带体网格由 StrapBandBuilder 跟踪生成。
    /// </summary>
    public class StrappingFeedMotion : MonoBehaviour
    {
        public enum StrapPhase
        {
            Idle,
            Feeding,    // 送带：沿矩形回路绕货一圈
            Tensioning, // 张紧：带体略向中心收
            Sealing,    // 热熔：接头处停留
            Cutting,    // 切断
            Complete
        }

        [Header("驱动对象")]
        [Label("货物")]
        [SerializeField] private Transform m_cargo;
        [Label("机头")]
        [SerializeField] private Transform m_head;
        [Label("送带跟踪点")]
        [SerializeField] private Transform m_feedTip;
        [Label("带体生成器")]
        [SerializeField] private StrapBandBuilder m_builder;

        [Header("回路（水平轧带）")]
        [Label("带高(相对货物中心Y)")]
        [SerializeField] private float m_bandHeight = 0.55f;
        [Label("回路外扩")]
        [SerializeField] private float m_loopMargin = 0.06f;
        [Label("机头侧(+X)")]
        [SerializeField] private bool m_headOnPositiveX = true;

        [Header("时序(秒)")]
        [Label("送带")]
        [SerializeField] private float m_feedDuration = 2.8f;
        [Label("张紧")]
        [SerializeField] private float m_tensionDuration = 0.6f;
        [Label("热熔")]
        [SerializeField] private float m_sealDuration = 0.45f;
        [Label("切断")]
        [SerializeField] private float m_cutDuration = 0.25f;

        [Header("多道带")]
        [Label("各道高度")]
        [SerializeField] private float[] m_bandHeights = { 0.35f, 0.75f, 1.15f };
        [Label("循环打带")]
        [SerializeField] private bool m_loopBands = true;

        [Header("播放")]
        [SerializeField] private bool m_playOnStart = true;

        private StrapPhase _phase = StrapPhase.Idle;
        private float _phaseTime;
        private float _feedT;
        private int _bandIndex;
        private bool _playing;
        private Vector2 _halfExtents;
        private float _tensionScale = 1f;

        public StrapPhase Phase => _phase;
        public float FeedProgress => _feedT;
        public int BandIndex => _bandIndex;
        public bool IsPlaying => _playing;
        public float TensionScale => _tensionScale;

        private void Start()
        {
            RefreshBounds();
            _playing = m_playOnStart;
            if (_playing)
                BeginBand(0);
            else
                SnapHeadToFeedStart();
        }

        private void Update()
        {
            if (!_playing || m_cargo == null)
                return;

            _phaseTime += Time.deltaTime;

            switch (_phase)
            {
                case StrapPhase.Feeding:
                    UpdateFeeding();
                    break;
                case StrapPhase.Tensioning:
                    UpdateTimedPhase(m_tensionDuration, StrapPhase.Sealing, OnTensionTick);
                    break;
                case StrapPhase.Sealing:
                    UpdateTimedPhase(m_sealDuration, StrapPhase.Cutting);
                    break;
                case StrapPhase.Cutting:
                    UpdateTimedPhase(m_cutDuration, StrapPhase.Complete, OnCutComplete);
                    break;
                case StrapPhase.Complete:
                    OnBandComplete();
                    break;
            }
        }

        public void SetPlaying(bool playing)
        {
            _playing = playing;
            if (playing && _phase == StrapPhase.Idle)
                BeginBand(_bandIndex);
        }

        public void TogglePlay() => SetPlaying(!_playing);

        public void ResetAll()
        {
            _bandIndex = 0;
            _phase = StrapPhase.Idle;
            _phaseTime = 0f;
            _feedT = 0f;
            _tensionScale = 1f;
            if (m_builder != null)
                m_builder.ResetBand();
            SnapHeadToFeedStart();
            _playing = m_playOnStart;
            if (_playing)
                BeginBand(0);
        }

        private void BeginBand(int index)
        {
            RefreshBounds();
            _bandIndex = index;
            float h = m_bandHeights != null && m_bandHeights.Length > 0
                ? m_bandHeights[Mathf.Clamp(index, 0, m_bandHeights.Length - 1)]
                : m_bandHeight;
            m_bandHeight = h;

            _phase = StrapPhase.Feeding;
            _phaseTime = 0f;
            _feedT = 0f;
            _tensionScale = 1f;

            if (m_builder != null)
            {
                m_builder.BuildEnabled = true;
                m_builder.ResetBand();
            }

            SnapHeadToFeedStart();
        }

        private void UpdateFeeding()
        {
            float dur = m_feedDuration > 0.01f ? m_feedDuration : 0.01f;
            _feedT = Mathf.Clamp01(_phaseTime / dur);
            ApplyFeedPose(_feedT);

            if (_phaseTime >= dur)
            {
                _phase = StrapPhase.Tensioning;
                _phaseTime = 0f;
            }
        }

        private void UpdateTimedPhase(float duration, StrapPhase next, System.Action<float> tick = null)
        {
            float t = duration > 0.01f ? Mathf.Clamp01(_phaseTime / duration) : 1f;
            tick?.Invoke(t);
            if (_phaseTime >= duration)
            {
                _phase = next;
                _phaseTime = 0f;
            }
        }

        private void OnTensionTick(float t)
        {
            // 张紧：回路略向中心收（模拟拉力）
            _tensionScale = Mathf.Lerp(1f, 0.96f, t);
            ApplyFeedPose(1f);
        }

        private void OnCutComplete(float t)
        {
            if (m_builder != null)
                m_builder.BuildEnabled = false;
        }

        private void OnBandComplete()
        {
            if (m_builder != null)
            {
                m_builder.SaveBandToCargo();
                m_builder.CreateNewBand();
            }

            int next = _bandIndex + 1;
            bool hasNext = m_bandHeights != null && next < m_bandHeights.Length;
            if (hasNext)
            {
                BeginBand(next);
                return;
            }

            if (m_loopBands && m_bandHeights != null && m_bandHeights.Length > 0)
            {
                BeginBand(0);
                return;
            }

            _phase = StrapPhase.Idle;
            _playing = false;
        }

        private void RefreshBounds()
        {
            _halfExtents = new Vector2(0.65f, 0.55f);
            if (m_cargo == null)
                return;

            var renderers = m_cargo.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
                return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            _halfExtents = new Vector2(b.extents.x + m_loopMargin, b.extents.z + m_loopMargin);
        }

        /// <summary>矩形回路参数 t∈[0,1]，从机头侧中点逆时针绕货。</summary>
        private Vector3 EvaluateLoop(float t)
        {
            Vector3 c = m_cargo.position;
            float y = c.y + m_bandHeight;
            float hx = _halfExtents.x * _tensionScale;
            float hz = _halfExtents.y * _tensionScale;

            // 机头在 +X 边中点；路径：+X → -Z → -X → +Z → 回 +X
            float sideX = m_headOnPositiveX ? hx : -hx;
            float perimeter = 2f * (hx + hz) * 2f;
            float dist = t * perimeter;

            float seg0 = hx * 2f;
            float seg1 = seg0 + hz * 2f;
            float seg2 = seg1 + hx * 2f;

            if (dist <= seg0)
            {
                float u = dist / (hx * 2f);
                return new Vector3(Mathf.Lerp(sideX, -sideX, u), y, hz);
            }

            if (dist <= seg1)
            {
                float u = (dist - seg0) / (hz * 2f);
                return new Vector3(-sideX, y, Mathf.Lerp(hz, -hz, u));
            }

            if (dist <= seg2)
            {
                float u = (dist - seg1) / (hx * 2f);
                return new Vector3(Mathf.Lerp(-sideX, sideX, u), y, -hz);
            }

            float uLast = (dist - seg2) / (hz * 2f);
            return new Vector3(sideX, y, Mathf.Lerp(-hz, hz, uLast));
        }

        private void SnapHeadToFeedStart()
        {
            Vector3 p = EvaluateLoop(0f);
            if (m_head != null)
                m_head.position = p + (m_headOnPositiveX ? Vector3.right : Vector3.left) * 0.08f;
            if (m_feedTip != null)
                m_feedTip.position = p;
        }

        private void ApplyFeedPose(float t)
        {
            Vector3 p = EvaluateLoop(t);
            if (m_feedTip != null)
                m_feedTip.position = p;
            if (m_head != null)
            {
                Vector3 headPos = p + (m_headOnPositiveX ? Vector3.right : Vector3.left) * 0.08f;
                m_head.position = headPos;
                Vector3 toCargo = m_cargo.position - headPos;
                toCargo.y = 0f;
                if (toCargo.sqrMagnitude > 1e-6f)
                    m_head.rotation = Quaternion.LookRotation(toCargo.normalized, Vector3.up);
            }
        }
    }
}
