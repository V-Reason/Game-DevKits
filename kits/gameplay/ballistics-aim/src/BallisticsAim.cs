using System;
using System.Numerics;

namespace GameDevKit.BallisticsAim
{
    public readonly struct ThrowSettings
    {
        public ThrowSettings(
            float minDistance,
            float maxDistance,
            float flightDuration,
            float maxHeight)
        {
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            FlightDuration = flightDuration;
            MaxHeight = maxHeight;
        }

        public float MinDistance { get; }

        public float MaxDistance { get; }

        public float FlightDuration { get; }

        public float MaxHeight { get; }
    }

    public readonly struct BallisticArc
    {
        public BallisticArc(Vector2 origin, Vector2 target, float distance, in ThrowSettings settings)
        {
            if (settings.MaxDistance <= 0
                || settings.MinDistance < 0
                || settings.MinDistance > settings.MaxDistance
                || settings.FlightDuration < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings));
            }

            Vector2 delta = target - origin;
            float actualDistance = delta.Length();
            distance = float.IsNaN(distance) || distance < 0 ? actualDistance : distance;

            Vector2 direction = actualDistance > 1e-6f
                ? delta / actualDistance
                : Vector2.UnitX;

            float clampedDistance = Math.Clamp(
                distance,
                settings.MinDistance,
                settings.MaxDistance);

            Start = origin;
            End = origin + direction * clampedDistance;
            Duration = settings.FlightDuration * (clampedDistance / settings.MaxDistance);
            MaxHeight = settings.MaxHeight;
        }

        public Vector2 Start { get; }

        public Vector2 End { get; }

        public float Duration { get; }

        public float MaxHeight { get; }

        public Vector2 SampleGround(float t)
        {
            return Vector2.Lerp(Start, End, t);
        }

        public Vector2 SampleVisual(float t)
        {
            return SampleGround(t) + Vector2.UnitY * (MaxHeight * Height01(t));
        }

        public static float Height01(float t)
        {
            return 4 * t * (1 - t);
        }
    }

    public static class EightWayDirection
    {
        public static Vector2 Snap(Vector2 input, bool snapToEightDirections)
        {
            if (input.LengthSquared() <= 0)
            {
                return Vector2.Zero;
            }

            if (!snapToEightDirections)
            {
                return Vector2.Normalize(input);
            }

            float degrees = MathF.Atan2(input.Y, input.X) * 180f / MathF.PI;
            int index = (int)MathF.Round(degrees / 45f);
            index = (index % 8 + 8) % 8;

            const float diagonal = 0.70710678f;

            switch (index)
            {
                case 0: return Vector2.UnitX;
                case 1: return new Vector2(diagonal, diagonal);
                case 2: return Vector2.UnitY;
                case 3: return new Vector2(-diagonal, diagonal);
                case 4: return -Vector2.UnitX;
                case 5: return new Vector2(-diagonal, -diagonal);
                case 6: return -Vector2.UnitY;
                default: return new Vector2(diagonal, -diagonal);
            }
        }
    }

    public readonly struct AimSettings
    {
        public AimSettings(
            Vector2 shoulderOffset,
            Vector2 orbitRadius,
            float minForeshortenScale,
            float maxThicknessScale,
            float backAngleThreshold,
            float maxLiftAngle,
            float liftDistanceThreshold,
            Func<float, float>? liftCurve)
        {
            ShoulderOffset = shoulderOffset;
            OrbitRadius = orbitRadius;
            MinForeshortenScale = minForeshortenScale;
            MaxThicknessScale = maxThicknessScale;
            BackAngleThreshold = backAngleThreshold;
            MaxLiftAngle = maxLiftAngle;
            LiftDistanceThreshold = liftDistanceThreshold;
            LiftCurve = liftCurve;
        }

        public Vector2 ShoulderOffset { get; }

        public Vector2 OrbitRadius { get; }

        public float MinForeshortenScale { get; }

        public float MaxThicknessScale { get; }

        public float BackAngleThreshold { get; }

        public float MaxLiftAngle { get; }

        public float LiftDistanceThreshold { get; }

        public Func<float, float>? LiftCurve { get; }
    }

    public readonly struct AimSolution
    {
        public AimSolution(
            Vector2 position,
            float angle,
            float distance,
            float liftAngle,
            bool facingLeft,
            bool isBehind,
            Vector2 scale)
        {
            Position = position;
            Angle = angle;
            Distance = distance;
            LiftAngle = liftAngle;
            FacingLeft = facingLeft;
            IsBehind = isBehind;
            Scale = scale;
        }

        public Vector2 Position { get; }

        public float Angle { get; }

        public float Distance { get; }

        public float LiftAngle { get; }

        public bool FacingLeft { get; }

        public bool IsBehind { get; }

        public Vector2 Scale { get; }
    }

    public static class AimSolver
    {
        public static AimSolution Solve(
            Vector2 characterPosition,
            Vector2 aimTarget,
            in AimSettings settings)
        {
            Vector2 shoulder = characterPosition + settings.ShoulderOffset;
            Vector2 toTarget = aimTarget - shoulder;
            float distance = toTarget.Length();

            if (distance < 0.001f)
            {
                toTarget = Vector2.UnitX;
                distance = 0.001f;
            }

            float rawAngle = MathF.Atan2(toTarget.Y, toTarget.X) * 180f / MathF.PI;
            float radians = rawAngle * MathF.PI / 180f;
            bool facingLeft = MathF.Abs(rawAngle) > 90;
            float cos = MathF.Abs(MathF.Cos(radians));
            float sin = MathF.Abs(MathF.Sin(radians));
            float threshold = Math.Clamp(settings.BackAngleThreshold, 0, 90);
            bool behind = rawAngle > threshold && rawAngle < 180 - threshold;

            Vector2 position = shoulder + new Vector2(
                MathF.Cos(radians) * settings.OrbitRadius.X,
                MathF.Sin(radians) * settings.OrbitRadius.Y);

            float normalizedDistance = Math.Clamp(
                distance / MathF.Max(settings.LiftDistanceThreshold, 0.01f),
                0,
                1);
            float liftFactor = settings.LiftCurve?.Invoke(normalizedDistance) ?? 0;
            float liftAngle = liftFactor * settings.MaxLiftAngle * cos;
            float signedLift = facingLeft ? -liftAngle : liftAngle;
            float visualAngle = NormalizeAngle(rawAngle + signedLift);

            float smoothCos = MathF.Pow(cos, 0.85f);
            float lengthScale = 1 + (Math.Clamp(settings.MinForeshortenScale, 0.7f, 1) - 1) * (1 - smoothCos);
            float thicknessScale = 1 + (MathF.Max(1, settings.MaxThicknessScale) - 1) * sin;

            return new AimSolution(
                position,
                visualAngle,
                distance,
                liftAngle,
                facingLeft,
                behind,
                new Vector2(lengthScale, thicknessScale));
        }

        private static float NormalizeAngle(float degrees)
        {
            while (degrees > 180)
            {
                degrees -= 360;
            }

            while (degrees < -180)
            {
                degrees += 360;
            }

            return degrees;
        }
    }
}
