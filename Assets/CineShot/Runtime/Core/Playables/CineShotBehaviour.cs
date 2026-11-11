using UnityEngine;
using UnityEngine.Playables;

namespace CineShot.Core
{
    public class CineShotBehaviour : PlayableBehaviour
    {
        CineShotClip m_Clip;
        SolveDelegate m_Solve;
        RigKind m_Kind;

        public CineShotClip Clip { get { return m_Clip; } }
        public SolveDelegate Solve { get { return m_Solve; } }
        public RigKind Kind { get { return m_Kind; } }

        public void Setup(CineShotClip clip)
        {
            m_Clip = clip;
            if (clip != null && clip.shot != null)
            {
                m_Kind = clip.shot.rigKind;
                if ((clip.overrides.mask & ShotOverrideMask.RigParams) != 0)
                {
                    // kind still from shot; params overridden at solve time
                }
                m_Solve = SolverDispatch.Get(m_Kind);
            }
            else
            {
                m_Kind = RigKind.Static;
                m_Solve = StaticSolver.SolveStatic;
            }
        }
    }
}
