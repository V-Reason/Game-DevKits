using GameDevKit.ActorMotion;
using GameDevKit.Unity;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class RigidbodyMotionMotor : MonoBehaviour, IMotionMotor
{
    private Rigidbody2D _body;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.gravityScale = 0;
        _body.freezeRotation = true;
        _body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    public System.Numerics.Vector2 Velocity
    {
        get => UnityVectorAdapter.ToNumerics(_body.velocity);
        set => _body.velocity = UnityVectorAdapter.ToUnity(value);
    }
}
