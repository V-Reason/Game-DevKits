using System;
using System.Numerics;
using Xunit;

namespace GameDevKit.BallisticsAim.Tests
{
    public sealed class BallisticsAimTests
    {
        private static readonly ThrowSettings Throw = new(1, 10, 2, 3);

        [Fact]
        public void ZeroDistanceThrowPushesToMinimumAlongRight()
        {
            var arc = new BallisticArc(Vector2.Zero, Vector2.Zero, 0, Throw);

            Assert.Equal(new Vector2(1, 0), arc.End);
            Assert.Equal(0.2f, arc.Duration, 5);
        }

        [Fact]
        public void FarThrowClampsToMaximum()
        {
            var arc = new BallisticArc(Vector2.Zero, new Vector2(25, 0), 25, Throw);

            Assert.Equal(new Vector2(10, 0), arc.End);
            Assert.Equal(2f, arc.Duration, 5);
        }

        [Fact]
        public void ArcSamplesGroundAndVisualHeight()
        {
            var arc = new BallisticArc(Vector2.Zero, new Vector2(10, 0), 10, Throw);

            Assert.Equal(new Vector2(5, 0), arc.SampleGround(0.5f));
            Assert.Equal(new Vector2(5, 3), arc.SampleVisual(0.5f));
            Assert.Equal(0, BallisticArc.Height01(0), 5);
            Assert.Equal(1, BallisticArc.Height01(0.5f), 5);
            Assert.Equal(0, BallisticArc.Height01(1), 5);
        }

        [Fact]
        public void EightWaySnapAlwaysReturnsUnitVector()
        {
            Vector2 diagonal = EightWayDirection.Snap(new Vector2(1, 1), true);
            Vector2 light = EightWayDirection.Snap(new Vector2(0.2f, 0), true);
            Vector2 unsnapped = EightWayDirection.Snap(new Vector2(1, 1), false);

            Assert.Equal(1f, diagonal.Length(), 3);
            Assert.True(Vector2.Dot(diagonal, Vector2.Normalize(Vector2.One)) > 0.999f);
            Assert.Equal(Vector2.UnitX, light);
            Assert.Equal(1f, unsnapped.Length(), 3);
            Assert.Equal(Vector2.Zero, EightWayDirection.Snap(Vector2.Zero, true));
        }

        [Fact]
        public void AimRightUsesEllipseOrbitAndFullScale()
        {
            AimSolution solution = AimSolver.Solve(Vector2.Zero, new Vector2(20, 0), Settings());

            Assert.Equal(new Vector2(0.25f, 0), solution.Position);
            Assert.Equal(0, solution.Angle, 3);
            Assert.False(solution.IsBehind);
            Assert.False(solution.FacingLeft);
            Assert.Equal(Vector2.One, solution.Scale);
        }

        [Fact]
        public void AimUpIsBehindAndForeshortened()
        {
            AimSolution solution = AimSolver.Solve(Vector2.Zero, new Vector2(0, 10), Settings());

            Assert.True(solution.IsBehind);
            Assert.Equal(90, solution.Angle, 3);
            Assert.Equal(0.7f, solution.Scale.X, 3);
            Assert.Equal(1f, solution.Scale.Y, 3);
            Assert.Equal(0, solution.LiftAngle, 3);
        }

        [Fact]
        public void BehindThresholdUsesRawAngle()
        {
            AimSolution front = AimSolver.Solve(Vector2.Zero, AtAngle(27, 10), Settings());
            AimSolution back = AimSolver.Solve(Vector2.Zero, AtAngle(29, 10), Settings());

            Assert.False(front.IsBehind);
            Assert.True(back.IsBehind);
        }

        [Fact]
        public void NearHorizontalAimAddsLift()
        {
            AimSolution solution = AimSolver.Solve(Vector2.Zero, new Vector2(0.5f, 0), Settings());

            Assert.Equal(43.125f, solution.LiftAngle, 3);
            Assert.Equal(43.125f, solution.Angle, 3);
        }

        [Fact]
        public void DeadZoneAndNullCurveDoNotProduceNaN()
        {
            AimSolution dead = AimSolver.Solve(Vector2.Zero, Vector2.Zero, Settings());
            AimSettings noCurve = new AimSettings(
                Vector2.Zero,
                new Vector2(0.25f, 0.2f),
                0.7f,
                1,
                28,
                45,
                12,
                null);
            AimSolution noLift = AimSolver.Solve(Vector2.Zero, new Vector2(0.5f, 0), noCurve);

            Assert.False(float.IsNaN(dead.Position.X));
            Assert.False(float.IsNaN(dead.Scale.X));
            Assert.Equal(0, noLift.LiftAngle, 3);
        }

        private static AimSettings Settings(Func<float, float>? curve = null)
        {
            curve ??= t => 1 - t;
            return new AimSettings(
                Vector2.Zero,
                new Vector2(0.25f, 0.2f),
                0.7f,
                1,
                28,
                45,
                12,
                curve);
        }

        private static Vector2 AtAngle(float degrees, float distance)
        {
            float radians = degrees * MathF.PI / 180;
            return new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * distance;
        }
    }
}
