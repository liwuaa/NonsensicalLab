using UnityEngine;

namespace CineShot.Core
{
    public struct FocusRequest
    {
        public float Distance;
        public float Weight;
        public int SourceId;
    }

    public struct FocusState
    {
        public float Distance;
        public float Velocity;
        public bool Pulling;
    }

    public interface IFocusProvider
    {
        bool TryEvaluate(in CameraPose pose, float dt, out FocusRequest request);
    }

    public interface IFocusOutput
    {
        bool IsValid { get; }
        void Apply(in FocusState state, in FocusProfile profile);
        void Dispose();
    }

    public sealed class NullFocusOutput : IFocusOutput
    {
        public static readonly NullFocusOutput Instance = new NullFocusOutput();
        public bool IsValid { get { return false; } }
        public void Apply(in FocusState state, in FocusProfile profile) { }
        public void Dispose() { }
    }
}
