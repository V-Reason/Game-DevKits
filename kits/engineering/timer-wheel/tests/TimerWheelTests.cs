using System;
using Xunit;

namespace GameDevKit.TimerWheel.Tests
{
    public sealed class TimerWheelTests
    {
        [Fact]
        public void OneShotTimerFiresAtDelayAndIsRemoved()
        {
            var wheel = new TimerWheel(0.1, 16);
            int fired = 0;
            wheel.Schedule(TimerOptions.Once(0.2), () => fired++);

            wheel.Advance(0.19);
            Assert.Equal(0, fired);

            wheel.Advance(0.02);
            Assert.Equal(1, fired);
            Assert.Equal(0, wheel.Count);
        }

        [Fact]
        public void RepeatingTimerRunsEachPeriod()
        {
            var wheel = new TimerWheel(0.1, 16);
            int fired = 0;
            wheel.Schedule(TimerOptions.Loop(0.1), () => fired++);

            wheel.Advance(0.1);
            wheel.Advance(0.1);

            Assert.Equal(2, fired);
            Assert.Equal(1, wheel.Count);
        }

        [Fact]
        public void TimerCanCancelItselfFromCallback()
        {
            var wheel = new TimerWheel(0.1, 16);
            int fired = 0;
            TimerHandle handle = TimerHandle.Invalid;
            handle = wheel.Schedule(TimerOptions.Loop(0.1), () =>
            {
                fired++;
                wheel.Cancel(handle);
            });

            wheel.Advance(0.5);

            Assert.Equal(1, fired);
            Assert.Equal(0, wheel.Count);
        }

        [Fact]
        public void TimerBeyondOneWheelRotationStillFires()
        {
            var wheel = new TimerWheel(0.1, 4);
            int fired = 0;
            wheel.Schedule(TimerOptions.Once(0.5), () => fired++);

            wheel.Advance(0.5);

            Assert.Equal(1, fired);
        }

        [Fact]
        public void ServiceUsesSeparateScaledAndUnscaledClocks()
        {
            var service = new TimerService(0.1, 16);
            int scaled = 0;
            int unscaled = 0;
            service.Schedule(0.1, () => scaled++);
            service.ScheduleUnscaled(0.1, () => unscaled++);

            service.Advance(0, 0.1);

            Assert.Equal(0, scaled);
            Assert.Equal(1, unscaled);
        }

        [Fact]
        public void LargeFrameDoesNotReplayRepeatingTimerManyTimes()
        {
            var service = new TimerService(0.1, 16);
            int fired = 0;
            service.ScheduleRepeating(0.1, () => fired++);

            service.Advance(2, 2);

            Assert.Equal(1, fired);
        }

        [Fact]
        public void CancelRejectsInvalidAndUnknownHandles()
        {
            var wheel = new TimerWheel(0.1, 16);

            Assert.False(wheel.Cancel(TimerHandle.Invalid));
            Assert.False(wheel.Cancel(new TimerHandle(999)));
        }

        [Fact]
        public void ServiceHandlesDoNotCollideAcrossTimeAxes()
        {
            var service = new TimerService(0.1, 16);
            int scaledFired = 0;
            int unscaledFired = 0;
            service.Schedule(0.1, () => scaledFired++);
            TimerHandle unscaled = service.ScheduleUnscaled(0.1, () => unscaledFired++);

            Assert.True(service.Cancel(unscaled));
            service.Advance(0.1, 0.1);

            Assert.Equal(1, scaledFired);
            Assert.Equal(0, unscaledFired);
            Assert.Equal(0, service.Count);
        }
    }
}
