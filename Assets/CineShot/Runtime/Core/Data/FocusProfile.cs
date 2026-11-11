using UnityEngine;

namespace CineShot.Core
{
    public enum DofMode
    {
        Gaussian = 0,
        Bokeh = 1
    }

    [CreateAssetMenu(menuName = "CineShot/Focus Profile", fileName = "Focus_")]
    public class FocusProfile : ScriptableObject
    {
        public DofMode dofMode = DofMode.Bokeh;
        public float damping = 6f;
        public float maxFocusSpeed = 20f;
        public float pullDuration = 0.5f;
        public float aperture = 5.6f;
        public float focalLength = 50f;
        public int bladeCount = 5;
        public LayerMask focusLayerMask = ~0;
        public float hyperfocalCircleOfConfusion = 0.00003f;
    }
}
