using System;
using System.Numerics;

namespace GameDevKit.ActorMotion
{
    public interface IMotionMotor
    {
        Vector2 Velocity { get; set; }
    }

    public readonly struct MotionParams
    {
        public MotionParams(float moveSpeed, float acceleration, float turnDecay)
        {
            MoveSpeed = moveSpeed;
            Acceleration = acceleration;
            TurnDecay = turnDecay;
        }

        public float MoveSpeed { get; }

        public float Acceleration { get; }

        public float TurnDecay { get; }
    }

    public readonly struct MotionGates
    {
        public MotionGates(bool hasForcedVelocity, Vector2 forcedVelocity, float speedScale)
        {
            HasForcedVelocity = hasForcedVelocity;
            ForcedVelocity = forcedVelocity;
            SpeedScale = speedScale;
        }

        public bool HasForcedVelocity { get; }

        public Vector2 ForcedVelocity { get; }

        public float SpeedScale { get; }

        public static MotionGates None => new(false, Vector2.Zero, 1);

        public static MotionGates Forced(Vector2 velocity) => new(true, velocity, 1);

        public static MotionGates Scaled(float scale) => new(false, Vector2.Zero, scale);
    }

    public sealed class MotionStatus
    {
        private float _slowScale = 1;
        private double _slowRemaining;
        private double _stunRemaining;
        private Vector2 _hurtVelocity;
        private double _hurtRemaining;

        public void ApplySlow(float scale, double seconds)
        {
            _slowScale = float.IsNaN(scale) ? 1 : Math.Clamp(scale, 0, 1);

            if (seconds > _slowRemaining)
            {
                _slowRemaining = seconds;
            }
        }

        public void ApplyStun(double seconds)
        {
            if (seconds > _stunRemaining)
            {
                _stunRemaining = seconds;
            }
        }

        public void ApplyHurt(Vector2 forcedVelocity, double seconds)
        {
            if (seconds <= 0)
            {
                return;
            }

            _hurtVelocity = forcedVelocity;
            _hurtRemaining = Math.Max(_hurtRemaining, seconds);
        }

        public void Tick(double deltaTime)
        {
            if (deltaTime <= 0 || double.IsNaN(deltaTime) || double.IsInfinity(deltaTime))
            {
                return;
            }

            _slowRemaining = Math.Max(0, _slowRemaining - deltaTime);
            _stunRemaining = Math.Max(0, _stunRemaining - deltaTime);
            _hurtRemaining = Math.Max(0, _hurtRemaining - deltaTime);
        }

        public MotionGates Gates
        {
            get
            {
                if (_stunRemaining > 0)
                {
                    return MotionGates.Forced(Vector2.Zero);
                }

                if (_hurtRemaining > 0)
                {
                    return MotionGates.Forced(_hurtVelocity);
                }

                return _slowRemaining > 0 && _slowScale < 1
                    ? MotionGates.Scaled(_slowScale)
                    : MotionGates.None;
            }
        }
    }

    public sealed class MotionLedger
    {
        private readonly IMotionMotor _motor;
        private readonly MotionParams _parameters;
        private Vector2 _frameStart;
        private Vector2 _delta;
        private Vector2 _acceleration;
        private double _deltaTime;
        private float _speedScale = 1;
        private bool _hasForcedVelocity;
        private Vector2 _forcedVelocity;

        public MotionLedger(IMotionMotor motor, MotionParams parameters)
        {
            _motor = motor ?? throw new ArgumentNullException(nameof(motor));
            _parameters = parameters;
        }

        public Vector2 Velocity => _frameStart + _delta + _acceleration * (float)_deltaTime;

        public void BeginStep(double now, double deltaTime)
        {
            if (deltaTime < 0 || double.IsNaN(deltaTime) || double.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            _frameStart = _motor.Velocity;
            _delta = Vector2.Zero;
            _acceleration = Vector2.Zero;
            _deltaTime = deltaTime;
            _speedScale = 1;
            _hasForcedVelocity = false;
            _forcedVelocity = Vector2.Zero;
        }

        public void SetGates(in MotionGates gates)
        {
            _speedScale = float.IsNaN(gates.SpeedScale)
                ? 1
                : Math.Clamp(gates.SpeedScale, 0, 1);

            _hasForcedVelocity = gates.HasForcedVelocity;
            _forcedVelocity = gates.ForcedVelocity;
        }

        public void MoveTowards(Vector2 direction, float speed)
        {
            Vector2 desired = direction.LengthSquared() <= 0 || speed <= 0
                ? Vector2.Zero
                : Vector2.Normalize(direction) * speed * _speedScale;

            ApplyTargetVelocity(desired);
        }

        public void Brake()
        {
            ApplyTargetVelocity(Vector2.Zero);
        }

        public void AddImpulse(Vector2 deltaVelocity)
        {
            _delta += deltaVelocity;
        }

        public void AddForce(Vector2 acceleration)
        {
            _acceleration += acceleration;
        }

        public void Commit()
        {
            if (_hasForcedVelocity)
            {
                _motor.Velocity = _forcedVelocity;
                return;
            }

            if (_delta.LengthSquared() <= 0 && _acceleration.LengthSquared() <= 0)
            {
                return;
            }

            _motor.Velocity = _frameStart + _delta + _acceleration * (float)_deltaTime;
        }

        private void ApplyTargetVelocity(Vector2 desired)
        {
            Vector2 current = Velocity;
            Vector2 delta = desired - current;

            if (_parameters.Acceleration <= 0)
            {
                _delta += delta;
                return;
            }

            float maxStep = _parameters.Acceleration * (float)_deltaTime;

            if (delta.LengthSquared() > maxStep * maxStep)
            {
                delta = Vector2.Normalize(delta) * maxStep;
            }

            _delta += delta;
        }
    }
}
