using System.Numerics;
using Xunit;

namespace GameDevKit.ActorMotion.Tests
{
    public sealed class ActorMotionTests
    {
        [Fact]
        public void ZeroInertiaMoveUsesNormalizedDirection()
        {
            var motor = new Motor();
            var ledger = new MotionLedger(motor, new MotionParams(8, 0, 0));
            ledger.BeginStep(0, 0.02);
            ledger.MoveTowards(new Vector2(1, 1), 8);
            ledger.Commit();

            Assert.Equal(8, motor.Velocity.Length(), 4);
        }

        [Fact]
        public void ImpulseIsOneShotAndForceUsesDeltaTime()
        {
            var motor = new Motor();
            var ledger = new MotionLedger(motor, new MotionParams(8, 0, 0));
            ledger.BeginStep(0, 0.5);
            ledger.AddImpulse(new Vector2(2, 0));
            ledger.AddForce(new Vector2(4, 0));
            ledger.Commit();

            Assert.Equal(4, motor.Velocity.X, 5);
        }

        [Fact]
        public void ReverseTurnRespectsAcceleration()
        {
            var motor = new Motor { Velocity = new Vector2(10, 0) };
            var ledger = new MotionLedger(motor, new MotionParams(5, 10, 30));
            ledger.BeginStep(0, 0.1);
            ledger.MoveTowards(Vector2.UnitX * -1, 5);
            ledger.Commit();

            Assert.Equal(9, motor.Velocity.X, 5);
        }

        [Fact]
        public void ForcedGateOverridesMovementSubmission()
        {
            var motor = new Motor();
            var ledger = new MotionLedger(motor, new MotionParams(8, 0, 0));
            ledger.BeginStep(0, 0.02);
            ledger.SetGates(MotionGates.Forced(new Vector2(-3, 0)));
            ledger.MoveTowards(Vector2.UnitX, 8);
            ledger.Commit();

            Assert.Equal(new Vector2(-3, 0), motor.Velocity);
        }

        [Fact]
        public void SpeedScaleAppliesToTargetSpeed()
        {
            var motor = new Motor();
            var ledger = new MotionLedger(motor, new MotionParams(8, 0, 0));
            ledger.BeginStep(0, 0.02);
            ledger.SetGates(MotionGates.Scaled(0.5f));
            ledger.MoveTowards(Vector2.UnitX, 8);
            ledger.Commit();

            Assert.Equal(4, motor.Velocity.X, 5);
        }

        [Fact]
        public void StunOverridesHurtAndSlow()
        {
            var status = new MotionStatus();
            status.ApplySlow(0.5f, 2);
            status.ApplyHurt(new Vector2(5, 0), 1);
            status.ApplyStun(0.2);

            MotionGates gates = status.Gates;

            Assert.True(gates.HasForcedVelocity);
            Assert.Equal(Vector2.Zero, gates.ForcedVelocity);
        }

        [Fact]
        public void SlowRenewalExtendsWindowWithoutStacking()
        {
            var status = new MotionStatus();
            status.ApplySlow(0.5f, 0.1);
            status.Tick(0.08);
            status.ApplySlow(0.5f, 0.1);
            status.Tick(0.08);

            Assert.Equal(0.5f, status.Gates.SpeedScale, 5);

            status.Tick(0.03);
            Assert.Equal(1f, status.Gates.SpeedScale, 5);
        }

        private sealed class Motor : IMotionMotor
        {
            public Vector2 Velocity { get; set; }
        }
    }
}
