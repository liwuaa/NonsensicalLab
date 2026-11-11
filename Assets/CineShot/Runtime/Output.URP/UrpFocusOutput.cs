using CineShot.Compat;
using CineShot.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CineShot.Output
{
    public sealed class UrpFocusOutput : IFocusOutput
    {
        Volume m_Volume;
        VolumeProfile m_Profile;
        DepthOfField m_Dof;
        bool m_Valid;

        public bool IsValid { get { return m_Valid; } }

        public UrpFocusOutput(Transform host, FocusProfile profile)
        {
            if (!DofChannelProbe.HasWritableDofChannel())
            {
                WarnOnce.Log("urp.dof", "[CineShot] URP DepthOfField 通道不可用，焦点输出为空。");
                return;
            }

            var go = new GameObject("CineShot_RuntimeDOF");
            go.hideFlags = HideFlags.HideAndDontSave;
            if (host != null)
                go.transform.SetParent(host, false);

            m_Volume = go.AddComponent<Volume>();
            m_Volume.isGlobal = true;
            m_Volume.priority = 1000f;

            m_Profile = ScriptableObject.CreateInstance<VolumeProfile>();
            m_Profile.hideFlags = HideFlags.HideAndDontSave;
            m_Volume.sharedProfile = m_Profile;

            m_Dof = m_Profile.Add<DepthOfField>(true);
            m_Dof.active = true;
            m_Dof.mode.overrideState = true;
            m_Dof.focusDistance.overrideState = true;
            m_Dof.aperture.overrideState = true;
            m_Dof.focalLength.overrideState = true;

            ApplyProfileDefaults(profile);
            m_Valid = true;
        }

        void ApplyProfileDefaults(FocusProfile profile)
        {
            if (m_Dof == null)
                return;
            bool bokeh = profile == null || profile.dofMode == DofMode.Bokeh;
            m_Dof.mode.value = bokeh ? DepthOfFieldMode.Bokeh : DepthOfFieldMode.Gaussian;
            if (profile != null)
            {
                m_Dof.aperture.value = profile.aperture;
                m_Dof.focalLength.value = profile.focalLength;
                m_Dof.bladeCount.value = profile.bladeCount;
            }
        }

        public void Apply(in FocusState state, in FocusProfile profile)
        {
            if (!m_Valid || m_Dof == null)
                return;
            m_Dof.focusDistance.value = Mathf.Max(0.1f, state.Distance);
            if (profile != null)
            {
                bool bokeh = profile.dofMode == DofMode.Bokeh;
                m_Dof.mode.value = bokeh ? DepthOfFieldMode.Bokeh : DepthOfFieldMode.Gaussian;
                if (bokeh)
                {
                    m_Dof.aperture.value = profile.aperture;
                    m_Dof.focalLength.value = profile.focalLength;
                }
            }
        }

        public void Dispose()
        {
            m_Valid = false;
            m_Dof = null;
            if (m_Profile != null)
            {
                Object.DestroyImmediate(m_Profile);
                m_Profile = null;
            }
            if (m_Volume != null)
            {
                var go = m_Volume.gameObject;
                m_Volume = null;
                Object.DestroyImmediate(go);
            }
        }
    }

    public sealed class UrpFocusOutputFactory : IFocusOutputFactory
    {
        public IFocusOutput Create(Transform host, FocusProfile profile)
        {
            if (!DofChannelProbe.HasWritableDofChannel())
                return NullFocusOutput.Instance;
            return new UrpFocusOutput(host, profile);
        }
    }
}
