namespace CineShot.Core
{
    public enum RigKind
    {
        Static = 0,
        DollyTrack = 1,
        Orbit = 2,
        PushPull = 3,
        Crane = 4,
        Shoulder = 5,
        Handheld = 6,
        SplineDolly = 7
    }

    public enum FocusMode
    {
        None = 0,
        Target = 1,
        FixedDistance = 2,
        Hyperfocal = 3
    }

    public enum ShotBlendMode
    {
        Cut = 0,
        Blend = 1,
        MatchFrame = 2
    }

    public enum TargetBindingMode
    {
        None = 0,
        LookAt = 1,
        Follow = 2,
        LookAtAndFollow = 3
    }

    [System.Flags]
    public enum ShotOverrideMask
    {
        None = 0,
        RigParams = 1 << 0,
        Composition = 1 << 1,
        Optics = 1 << 2,
        Focus = 1 << 3,
        Easing = 1 << 4,
        PriorityTags = 1 << 5
    }
}
