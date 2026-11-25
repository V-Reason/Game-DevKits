using NumericsVector2 = System.Numerics.Vector2;
using UnityVector2 = UnityEngine.Vector2;

namespace GameDevKit.Unity
{
    public static class UnityVectorAdapter
    {
        public static NumericsVector2 ToNumerics(UnityVector2 value)
        {
            return new NumericsVector2(value.x, value.y);
        }

        public static UnityVector2 ToUnity(NumericsVector2 value)
        {
            return new UnityVector2(value.X, value.Y);
        }
    }
}
