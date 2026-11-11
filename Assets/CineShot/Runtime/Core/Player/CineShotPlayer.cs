using UnityEngine;
using UnityEngine.Playables;

namespace CineShot.Core
{
    [DisallowMultipleComponent]
    public sealed class CineShotPlayer : MonoBehaviour
    {
        [SerializeField] PlayableDirector m_Director;
        [SerializeField] CineShotRig m_Rig;
        [SerializeField] float m_DefaultInterruptBlend = 0.35f;

        CinePlayer m_Player;

        public CinePlayer Player { get { return m_Player; } }
        public PlayableDirector Director { get { return m_Director; } }
        public CineShotRig Rig { get { return m_Rig; } }

        void Awake()
        {
            if (m_Director == null)
                m_Director = GetComponent<PlayableDirector>();
            if (m_Rig == null)
                m_Rig = GetComponent<CineShotRig>();
            EnsurePlayer();
        }

        void EnsurePlayer()
        {
            if (m_Player != null)
                return;
            ICameraOutput output = m_Rig != null ? m_Rig.Output : NullCameraOutput.Instance;
            m_Player = new CinePlayer(output, m_Director, m_Rig);
        }

        void Update()
        {
            if (m_Player != null)
                m_Player.Tick(Time.deltaTime);
        }

        public bool Play(double startTime = 0.0)
        {
            EnsurePlayer();
            if (m_Rig != null)
                m_Rig.RebuildOutputs();
            EnsurePlayer();
            // recreate with latest output
            m_Player = new CinePlayer(m_Rig != null ? m_Rig.Output : NullCameraOutput.Instance, m_Director, m_Rig);
            return m_Player.Play(null, startTime);
        }

        public void Stop(bool immediate = true)
        {
            EnsurePlayer();
            m_Player.Stop(immediate);
        }

        public bool Interrupt(float blendSeconds = -1f)
        {
            EnsurePlayer();
            if (blendSeconds < 0f)
                blendSeconds = m_DefaultInterruptBlend;
            return m_Player.Interrupt(blendSeconds);
        }

        public bool Resume()
        {
            EnsurePlayer();
            return m_Player.Resume();
        }
    }
}
