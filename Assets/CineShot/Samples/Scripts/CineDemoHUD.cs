using CineShot.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CineShot.Samples
{
    public sealed class CineDemoHUD : MonoBehaviour
    {
        [SerializeField] CineShotPlayer m_Player;
        [SerializeField] Text m_Status;
        [SerializeField] float m_InterruptBlend = 0.35f;

        void Reset()
        {
            m_Player = FindObjectOfType<CineShotPlayer>();
        }

        void Update()
        {
            bool play = false, interrupt = false, resume = false, stop = false;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                play = kb.spaceKey.wasPressedThisFrame;
                interrupt = kb.iKey.wasPressedThisFrame;
                resume = kb.rKey.wasPressedThisFrame;
                stop = kb.sKey.wasPressedThisFrame;
            }
#else
            play = Input.GetKeyDown(KeyCode.Space);
            interrupt = Input.GetKeyDown(KeyCode.I);
            resume = Input.GetKeyDown(KeyCode.R);
            stop = Input.GetKeyDown(KeyCode.S);
#endif
            if (m_Player == null)
                return;
            if (play) m_Player.Play(0.0);
            if (interrupt) m_Player.Interrupt(m_InterruptBlend);
            if (resume) m_Player.Resume();
            if (stop) m_Player.Stop(true);

            if (m_Status != null && m_Player.Player != null)
            {
                var p = m_Player.Player;
                string shot = p.CurrentShot != null ? p.CurrentShot.displayName : "-";
                double time = m_Player.Director != null ? m_Player.Director.time : 0.0;
                m_Status.text = p.State + " | " + shot + " | t=" + time.ToString("0.00")
                                + " | P95=" + CinePerfProbe.AverageMs.ToString("0.000");
            }
        }

        public void UiPlay() { if (m_Player != null) m_Player.Play(0.0); }
        public void UiInterrupt() { if (m_Player != null) m_Player.Interrupt(m_InterruptBlend); }
        public void UiResume() { if (m_Player != null) m_Player.Resume(); }
        public void UiStop() { if (m_Player != null) m_Player.Stop(true); }
    }
}
