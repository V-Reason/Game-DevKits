using System;
using System.Numerics;
using Xunit;

namespace GameDevKit.SteeringPathFollowing.Tests
{
    public sealed class PathFollowingTests
    {
        [Fact]
        public void FollowerLooksAheadAlongStraightPath()
        {
            var field = new FakeField(
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(2, 0),
                new Vector2(3, 0));
            var follower = new PathFollower(new PathFollowingOptions
            {
                LookAheadDistance = 1.5f,
                CornerScanDistance = 0.1f,
            });

            Assert.True(follower.TryResolve(field, new Vector2(0.1f, 0), Vector2.UnitX, false, out var result));
            Assert.True(result.Direction.X > 0.9f);
            Assert.False(result.CornerAhead);
        }

        [Fact]
        public void FollowerReportsCornerWithinScanDistance()
        {
            var field = new FakeField(
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(2, 0),
                new Vector2(2, 1));
            var follower = new PathFollower(new PathFollowingOptions
            {
                LookAheadDistance = 2f,
                CornerScanDistance = 5f,
            });

            Assert.True(follower.TryResolve(field, new Vector2(0.5f, 0), Vector2.UnitX, false, out var result));
            Assert.True(result.CornerAhead);
            Assert.True(result.CornerDistance > 0);
        }

        [Fact]
        public void BlockedShortcutFallsBackToNextNode()
        {
            var field = new FakeField(
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(2, 0),
                new Vector2(3, 0))
            {
                BlockedPoint = new Vector2(2.5f, 0),
                BlockLine = true,
            };
            var follower = new PathFollower(new PathFollowingOptions
            {
                LookAheadDistance = 2.5f,
                CornerScanDistance = 0.1f,
            });

            Assert.True(follower.TryResolve(field, new Vector2(0.4f, 0), Vector2.UnitX, false, out var result));
            Assert.True(result.Direction.X > 0);
        }

        [Fact]
        public void MissingPathReturnsFalse()
        {
            var field = new FakeField(new Vector2(0, 0)) { NoStart = true };

            Assert.False(new PathFollower().TryResolve(field, Vector2.Zero, Vector2.Zero, false, out _));
        }

        [Fact]
        public void SteeringGateKeepsOutputInForwardHalfPlane()
        {
            var gate = new SteeringGate(180, 0);
            gate.Reset(Vector2.UnitX);

            Vector2 output = gate.Step(-Vector2.UnitX, Vector2.UnitX, 0.1f);

            Assert.True(Vector2.Dot(output, Vector2.UnitX) >= 0);
        }

        [Fact]
        public void SeparationPushesAwayFromNeighborAndRejectsUnwalkableProbe()
        {
            var bodies = new[]
            {
                new SteeringBody(new Vector2(-0.2f, 0), 0.3f),
            };

            Vector2 push = SeparationResolver.Resolve(
                Vector2.Zero,
                0.3f,
                bodies,
                0.1f,
                _ => true);

            Assert.True(push.X > 0);

            Vector2 blocked = SeparationResolver.Resolve(
                Vector2.Zero,
                0.3f,
                bodies,
                0.1f,
                _ => false);

            Assert.Equal(Vector2.Zero, blocked);
        }

        [Fact]
        public void AlternateNextStepIsUsedWhileJamming()
        {
            var field = new FakeField(
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(2, 0),
                new Vector2(3, 0))
            {
                AlternateIndex = 0,
                StartIndex = 1,
            };
            var follower = new PathFollower(new PathFollowingOptions
            {
                LookAheadDistance = 0.9f,
                CornerScanDistance = 0.1f,
            });

            Assert.True(follower.TryResolve(field, new Vector2(1.1f, 0), Vector2.UnitX, true, out var result));
            Assert.True(result.Direction.X < 0);
        }

        private sealed class FakeField : IPathField
        {
            private readonly Vector2[] _positions;

            public FakeField(params Vector2[] positions)
            {
                _positions = positions;
            }

            public int Version => 1;

            public float SampleSpacing => 0.05f;

            public bool NoStart { get; set; }

            public Vector2 BlockedPoint { get; set; }

            public bool BlockLine { get; set; }

            public int AlternateIndex { get; set; } = -1;

            public int StartIndex { get; set; }

            public bool TryFindStart(Vector2 position, out int node)
            {
                node = NoStart ? -1 : StartIndex;
                return !NoStart;
            }

            public bool TryGetNext(int node, out int next)
            {
                next = node + 1;
                return next < _positions.Length;
            }

            public bool TryGetAlternateNext(int node, out int next)
            {
                next = AlternateIndex;
                return next >= 0 && next < _positions.Length;
            }

            public Vector2 SamplePosition(int node)
            {
                return _positions[node];
            }

            public bool IsPointWalkable(Vector2 point)
            {
                return !BlockLine || Vector2.Distance(point, BlockedPoint) > 0.4f;
            }
        }
    }
}
