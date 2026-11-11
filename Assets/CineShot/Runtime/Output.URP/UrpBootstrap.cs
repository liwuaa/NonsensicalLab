using CineShot.Core;
using UnityEngine;

namespace CineShot.Output
{
    public static class UrpBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            CineOutputFactory.CreateFocusOutput = (host, profile) =>
            {
                if (!CineShot.Compat.DofChannelProbe.HasWritableDofChannel())
                    return NullFocusOutput.Instance;
                return new UrpFocusOutput(host, profile);
            };
        }
    }
}
