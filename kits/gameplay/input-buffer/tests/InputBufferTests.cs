using System;
using System.Numerics;
using Xunit;

namespace GameDevKit.InputBuffer.Tests
{
    public sealed class InputBufferTests
    {
        private enum Action
        {
            Dash,
        }

        [Fact]
        public void PendingInputCanBeConsumedOnlyOnce()
        {
            var buffer = new InputBuffer<Action>();
            buffer.Push(Action.Dash, 10);

            Assert.True(buffer.CanConsume(Action.Dash, 10, 0.2));
            Assert.True(buffer.TryConsume(Action.Dash, 10, 0.2));
            Assert.False(buffer.CanConsume(Action.Dash, 10, 0.2));
        }

        [Fact]
        public void WindowIncludesBothBoundariesAndRejectsExpiredPress()
        {
            var buffer = new InputBuffer<Action>();
            buffer.Push(Action.Dash, 10);

            Assert.True(buffer.CanConsume(Action.Dash, 10.1, 0.1));
            Assert.False(buffer.CanConsume(Action.Dash, 10.1001, 0.1));
        }

        [Fact]
        public void TimeCannotMoveBackwards()
        {
            var buffer = new InputBuffer<Action>();
            buffer.PushSnapshot(new InputSnapshot(new Vector2(1, 0)), 10);

            Assert.Throws<InvalidOperationException>(
                () => buffer.Push(Action.Dash, 9.9));
        }

        [Fact]
        public void ClearRemovesPendingInputAndLatestSnapshot()
        {
            var buffer = new InputBuffer<Action>();
            buffer.PushSnapshot(new InputSnapshot(new Vector2(1, -1)), 5);
            buffer.Push(Action.Dash, 5);

            buffer.Clear();

            Assert.Equal(Vector2.Zero, buffer.Latest.Move);
            Assert.False(buffer.CanConsume(Action.Dash, 5, 1));
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void WindowMustBeFiniteAndNonNegative(double seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new InputWindow(seconds));
        }
    }
}
