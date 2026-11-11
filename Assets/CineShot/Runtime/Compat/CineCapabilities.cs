namespace CineShot.Compat
{
    public readonly struct CineCapabilities
    {
        public readonly bool CinemachinePresent;
        public readonly bool CinemachineV3;
        public readonly bool CameraLensWrite;
        public readonly bool BrainActiveCamera;
        public readonly bool UrpVolumeDof;
        public readonly bool ImpulseModule;
        public readonly bool TimelineCmTrack;
        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;

        public CineCapabilities(
            bool cm, bool v3, bool lens, bool brain, bool dof, bool impulse, bool track,
            int major, int minor, int patch)
        {
            CinemachinePresent = cm;
            CinemachineV3 = v3;
            CameraLensWrite = lens;
            BrainActiveCamera = brain;
            UrpVolumeDof = dof;
            ImpulseModule = impulse;
            TimelineCmTrack = track;
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public string Version
        {
            get { return Major == 0 ? "未知" : Major + "." + Minor + "." + Patch; }
        }
    }
}
