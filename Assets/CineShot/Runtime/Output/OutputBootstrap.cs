using CineShot.Core;
using UnityEngine;

namespace CineShot.Output
{
    public static class OutputBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            CineOutputFactory.CreateCameraOutput = AdapterCameraOutput.CreateFromComponent;
#if CINE_URP_PRESENT
            // URP factory registered by Output.URP assembly if present
#endif
        }
    }
}
