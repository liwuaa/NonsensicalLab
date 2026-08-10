using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.FilmWrapping
{
    /// <summary>
    /// Demo 面板：分别控制「运动」与「缠膜」。
    /// 运动可换成自定义脚本；缠膜只跟随喷嘴，喷嘴不动则停止生成。
    /// </summary>
    public class FilmWrappingMachineDemo : MonoBehaviour
    {
        [Header("核心引用")]
        [Label("膜带生成器")]
        [Tooltip("只负责根据喷嘴位移生成膜，不驱动运动")]
        [SerializeField] private FilmRibbonBuilder m_ribbon;
        [Label("喷嘴运动(示例)")]
        [Tooltip("示例螺旋运动；可禁用后改用自己的运动/Timeline")]
        [SerializeField] private FilmWrappingNozzleMotion m_motion;

        [Header("播放默认")]
        [Label("开始时播放运动")]
        [SerializeField] private bool m_playMotionOnStart = true;
        [Label("开始时启用缠膜")]
        [SerializeField] private bool m_wrapOnStart = true;

        [Header("界面 UI")]
        [Label("状态文本")]
        [SerializeField] private Text m_statusText;
        [Label("进度滑条")]
        [Tooltip("仅控制示例运动进度；自定义运动时可不用")]
        [SerializeField] private Slider m_progressSlider;
        [Label("运动 播放/暂停")]
        [SerializeField] private Button m_playButton;
        [Label("重置")]
        [SerializeField] private Button m_resetButton;

        private bool _ignoreSliderCallback;
        private float _uiTimer;

        private void Awake()
        {
            if (m_playButton != null)
                m_playButton.onClick.AddListener(ToggleMotion);
            if (m_resetButton != null)
                m_resetButton.onClick.AddListener(ResetAll);
            if (m_progressSlider != null)
            {
                m_progressSlider.minValue = 0f;
                m_progressSlider.maxValue = 1f;
                m_progressSlider.onValueChanged.AddListener(OnSliderChanged);
            }
        }

        private void Start()
        {
            if (m_ribbon != null)
                m_ribbon.WrappingEnabled = m_wrapOnStart;

            if (m_motion != null)
                m_motion.SetPlaying(m_playMotionOnStart);

            RefreshUi();
        }

        private void Update()
        {
            if (WasPressed(KeyCode.Space))
                ToggleMotion();
            if (WasPressed(KeyCode.R))
                ResetAll();
            if (WasPressed(KeyCode.W))
                ToggleWrapping();

            if (m_progressSlider != null && m_motion != null)
                m_progressSlider.SetValueWithoutNotify(m_motion.Progress);

            // UI 文字约 5Hz 刷新，避免每帧字符串分配
            _uiTimer += Time.unscaledDeltaTime;
            if (_uiTimer >= 0.2f)
            {
                _uiTimer = 0f;
                RefreshUi();
            }
        }

        public void ToggleMotion()
        {
            if (m_motion != null)
                m_motion.TogglePlay();
            RefreshUi();
        }

        public void ToggleWrapping()
        {
            if (m_ribbon == null)
                return;
            m_ribbon.WrappingEnabled = !m_ribbon.WrappingEnabled;
            RefreshUi();
        }

        public void ResetAll()
        {
            if (m_motion != null)
                m_motion.ResetMotion();
            else if (m_ribbon != null)
                m_ribbon.ResetWrap();

            if (m_ribbon != null)
                m_ribbon.WrappingEnabled = m_wrapOnStart;
            RefreshUi();
        }

        private void OnSliderChanged(float value)
        {
            if (_ignoreSliderCallback || m_motion == null)
                return;

            m_motion.SetPlaying(false);
            m_motion.SetProgress(value);
            RefreshUi();
        }

        private void RefreshUi()
        {
            if (m_statusText == null)
                return;

            bool motionPlaying = m_motion != null && m_motion.IsPlaying;
            bool wrapping = m_ribbon != null && m_ribbon.WrappingEnabled;
            bool nozzleMoving = m_ribbon != null && m_ribbon.IsNozzleMoving;
            float motionProgress = m_motion != null ? m_motion.Progress : 0f;
            int rings = m_ribbon != null ? m_ribbon.RingCount : 0;

            string genState = !wrapping
                ? "缠膜关闭"
                : (nozzleMoving ? "生成中" : "喷头静止·已停止生成");

            m_statusText.text =
                "缠膜机 Demo（运动 / 缠膜分离）\n" +
                "运动: " + (motionPlaying ? "播放" : "暂停") +
                "  " + (motionProgress * 100f).ToString("0.0") + "%\n" +
                "缠膜: " + genState + "  段数=" + rings + "\n" +
                "[Space] 运动播放/暂停   [W] 开/关缠膜   [R] 重置\n" +
                "可用自定义运动替换 FilmWrappingNozzleMotion；\n" +
                "喷头不动时不会继续生成膜带";
        }

        private static bool WasPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return false;
            switch (key)
            {
                case KeyCode.Space: return keyboard.spaceKey.wasPressedThisFrame;
                case KeyCode.R: return keyboard.rKey.wasPressedThisFrame;
                case KeyCode.W: return keyboard.wKey.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetKeyDown(key);
#endif
        }
    }
}
