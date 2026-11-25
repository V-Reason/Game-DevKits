using System;
using UnityEngine;

namespace GameDevKit.Unity
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Game DevKit/Unity Runtime Driver")]
    public sealed class UnityRuntimeDriver : MonoBehaviour
    {
        [SerializeField]
        private bool driveFrame = true;

        [SerializeField]
        private bool drivePhysics = true;

        public event Action<double, double>? FrameTick;

        public event Action<double>? PhysicsTick;

        private void Update()
        {
            if (!driveFrame)
            {
                return;
            }

            FrameTick?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            if (!drivePhysics)
            {
                return;
            }

            PhysicsTick?.Invoke(Time.fixedDeltaTime);
        }
    }
}
