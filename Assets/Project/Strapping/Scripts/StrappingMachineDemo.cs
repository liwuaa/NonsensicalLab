using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.Strapping
{
    /// <summary>打轧带 Demo：运动 / 写带分离；Space 播放，R 重置，W 开关写带。</summary>
    public class StrappingMachineDemo : MonoBehaviour
    {
        [SerializeField] private StrapBandBuilder m_builder;
        [SerializeField] private StrappingFeedMotion m_motion;
        [SerializeField] private bool m_playOnStart = true;
        [SerializeField] private bool m_buildOnStart = true;
        [SerializeField] private Text m_statusText;
        [SerializeField] private Button m_playButton;
        [SerializeField] private Button m_resetButton;

        private float _uiTimer;

        private void Awake()
        {
            if (m_playButton != null)
                m_playButton.onClick.AddListener(ToggleMotion);
            if (m_resetButton != null)
                m_resetButton.onClick.AddListener(ResetAll);
        }

        private void Start()
        {
            if (m_builder != null)
                m_builder.BuildEnabled = m_buildOnStart;
            if (m_motion != null)
                m_motion.SetPlaying(m_playOnStart);
            RefreshUi();
        }

        private void Update()
        {
            if (WasPressed(KeyCode.Space))
                ToggleMotion();
            if (WasPressed(KeyCode.R))
                ResetAll();
            if (WasPressed(KeyCode.W))
                ToggleBuild();

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

        public void ToggleBuild()
        {
            if (m_builder == null)
                return;
            m_builder.BuildEnabled = !m_builder.BuildEnabled;
            RefreshUi();
        }

        public void ResetAll()
        {
            if (m_motion != null)
                m_motion.ResetAll();
            if (m_builder != null)
                m_builder.BuildEnabled = m_buildOnStart;
            RefreshUi();
        }

        private void RefreshUi()
        {
            if (m_statusText == null)
                return;

            string phase = m_motion != null ? m_motion.Phase.ToString() : "-";
            int band = m_motion != null ? m_motion.BandIndex + 1 : 0;
            float feed = m_motion != null ? m_motion.FeedProgress * 100f : 0f;
            bool building = m_builder != null && m_builder.BuildEnabled;
            int segs = m_builder != null ? m_builder.SegmentCount : 0;

            m_statusText.text =
                "打轧带 Demo（送带 / 写带分离）\n" +
                "阶段: " + phase + "  第" + band + "道  送带" + feed.ToString("0") + "%\n" +
                "写带: " + (building ? "开" : "关") + "  段数=" + segs + "\n" +
                "[Space] 播放/暂停  [W] 写带开关  [R] 重置";
        }

        private static bool WasPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return key switch
            {
                KeyCode.Space => kb.spaceKey.wasPressedThisFrame,
                KeyCode.R => kb.rKey.wasPressedThisFrame,
                KeyCode.W => kb.wKey.wasPressedThisFrame,
                _ => false
            };
#else
            return Input.GetKeyDown(key);
#endif
        }
    }
}
