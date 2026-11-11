using UnityEngine;

namespace CineShot.Core
{
    public interface ICameraOutput
    {
        void Bind(Camera fallbackCamera);
        void Apply(in CameraPose pose);
        void SetControlling(bool controlling);
        int Priority { get; set; }
        bool SupportsDutch { get; }
        bool SupportsPhysicalLens { get; }
        Transform OutputTransform { get; }
        bool IsControlling { get; }
    }

    public sealed class NullCameraOutput : ICameraOutput
    {
        public static readonly NullCameraOutput Instance = new NullCameraOutput();
        public bool SupportsDutch { get { return false; } }
        public bool SupportsPhysicalLens { get { return false; } }
        public Transform OutputTransform { get { return null; } }
        public bool IsControlling { get { return false; } }
        public int Priority { get; set; }
        public void Bind(Camera fallbackCamera) { }
        public void Apply(in CameraPose pose) { }
        public void SetControlling(bool controlling) { }
    }

    public interface IFocusOutputFactory
    {
        IFocusOutput Create(Transform host, FocusProfile profile);
    }
}
