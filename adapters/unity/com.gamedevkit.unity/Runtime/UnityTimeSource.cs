using UnityEngine;

namespace GameDevKit.Unity
{
    public sealed class UnityTimeSource
    {
        public double Time => UnityEngine.Time.timeAsDouble;

        public double DeltaTime => UnityEngine.Time.deltaTime;

        public double UnscaledDeltaTime => UnityEngine.Time.unscaledDeltaTime;

        public double FixedDeltaTime => UnityEngine.Time.fixedDeltaTime;

        public double RealtimeSinceStartup => UnityEngine.Time.realtimeSinceStartupAsDouble;
    }
}
