using System;
using System.Collections.Generic;
using Xunit;

namespace GameDevKit.CompositionRoot.Tests
{
    public sealed class RuntimeBootstrapTests
    {
        [Fact]
        public void ServicesRunInOrderAndStopInReverse()
        {
            var log = new List<string>();
            var bootstrap = new RuntimeBootstrap();
            bootstrap.Add(new RecordingService("late", 20, log));
            bootstrap.Add(new RecordingService("early", 10, log));

            bootstrap.Start();
            bootstrap.Tick(0.016, 0.02);
            bootstrap.Stop();

            Assert.Equal(new[]
            {
                "early.Start",
                "late.Start",
                "early.Tick",
                "late.Tick",
                "late.Stop",
                "early.Stop",
            }, log);
            Assert.False(bootstrap.IsRunning);
        }

        [Fact]
        public void DuplicateOrderIsRejected()
        {
            var bootstrap = new RuntimeBootstrap();
            bootstrap.Add(new RecordingService("first", 10, new List<string>()));

            Assert.Throws<ArgumentException>(
                () => bootstrap.Add(new RecordingService("second", 10, new List<string>())));
        }

        [Fact]
        public void FailedStartupRollsBackAlreadyStartedServices()
        {
            var log = new List<string>();
            var bootstrap = new RuntimeBootstrap();
            bootstrap.Add(new RecordingService("good", 10, log));
            bootstrap.Add(new FailingService(20, log));

            Assert.Throws<InvalidOperationException>(() => bootstrap.Start());

            Assert.False(bootstrap.IsRunning);
            Assert.Equal(new[] { "good.Start", "failing.Start", "good.Stop" }, log);
        }

        [Fact]
        public void ServicesCannotBeAddedAfterStart()
        {
            var bootstrap = new RuntimeBootstrap();
            bootstrap.Add(new RecordingService("first", 10, new List<string>()));
            bootstrap.Start();

            Assert.Throws<InvalidOperationException>(
                () => bootstrap.Add(new RecordingService("second", 20, new List<string>())));

            bootstrap.Stop();
        }

        private class RecordingService : IRuntimeService
        {
            private readonly string _name;
            private readonly List<string> _log;

            public RecordingService(string name, int order, List<string> log)
            {
                _name = name;
                Order = order;
                _log = log;
            }

            public int Order { get; }

            public virtual void Start()
            {
                _log.Add($"{_name}.Start");
            }

            public virtual void Tick(double deltaTime, double unscaledDeltaTime)
            {
                _log.Add($"{_name}.Tick");
            }

            public virtual void Stop()
            {
                _log.Add($"{_name}.Stop");
            }
        }

        private sealed class FailingService : RecordingService
        {
            public FailingService(int order, List<string> log)
                : base("failing", order, log)
            {
            }

            public override void Start()
            {
                base.Start();
                throw new InvalidOperationException("startup failed");
            }
        }
    }
}
