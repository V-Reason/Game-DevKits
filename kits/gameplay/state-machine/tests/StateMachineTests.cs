using System;
using System.Collections.Generic;
using Xunit;

namespace GameDevKit.StateMachine.Tests
{
    public sealed class StateMachineTests
    {
        private enum Mode
        {
            Idle,
            Run,
        }

        [Fact]
        public void InvalidTransitionDoesNotInvokeHooks()
        {
            var log = new List<string>();
            var idle = new RecordingState(Mode.Idle, log);
            var machine = new StateMachine<Mode, int>();
            machine.Add(idle);

            bool changed = machine.TryChange(Mode.Run, 1);

            Assert.False(changed);
            Assert.Empty(log);
        }

        [Fact]
        public void TransitionExitsThenEntersAndPublishesChange()
        {
            var log = new List<string>();
            var machine = new StateMachine<Mode, int>();
            var idle = new RecordingState(Mode.Idle, log);
            var run = new RecordingState(Mode.Run, log);
            machine.Add(idle);
            machine.Add(run);
            machine.StateChanged += (next, previous) => log.Add($"{previous}->{next}");

            Assert.True(machine.TryChange(Mode.Idle, 1));
            Assert.True(machine.TryChange(Mode.Run, 2));

            Assert.Equal(new[] { "Idle.Enter", "Idle.Exit", "Run.Enter", "Idle->Run" }, log);
            Assert.Same(run, machine.Current);
        }

        [Fact]
        public void PreemptionRequiresCommitGate()
        {
            var log = new List<string>();
            var group = new TestGroup(log);
            group.Add(new RecordingState(Mode.Idle, log));
            group.Add(new RecordingState(Mode.Run, log));

            group.Tick(1);
            Assert.Equal(Mode.Idle, group.Current);
            Assert.Equal(new[] { "Idle.Enter", "Idle.Tick" }, log);

            log.Clear();
            group.AllowPreemption = true;
            group.Tick(2);

            Assert.Equal(Mode.Run, group.Current);
            Assert.Equal(new[] { "Idle.Exit", "Run.Enter", "Run.Tick" }, log);
        }

        [Fact]
        public void CompletedStateFallsBackBeforeTickingFallback()
        {
            var log = new List<string>();
            var group = new TestGroup(log);
            var idle = new RecordingState(Mode.Idle, log) { Complete = true };
            group.Add(idle);
            group.Add(new RecordingState(Mode.Run, log));

            group.Tick(0);
            log.Clear();
            group.Fallback = Mode.Run;
            group.Tick(1);

            Assert.Equal(Mode.Run, group.Current);
            Assert.Equal(new[] { "Idle.Exit", "Run.Enter", "Run.Tick" }, log);
        }

        private sealed class RecordingState : IState<Mode, int>
        {
            private readonly List<string> _log;

            public RecordingState(Mode key, List<string> log)
            {
                Key = key;
                _log = log;
            }

            public Mode Key { get; }

            public bool Complete { get; set; }

            public void Enter(in int context)
            {
                _log.Add($"{Key}.Enter");
            }

            public void Exit()
            {
                _log.Add($"{Key}.Exit");
            }

            public void Tick(in int context)
            {
                _log.Add($"{Key}.Tick");
            }

            public bool IsComplete(in int context)
            {
                return Complete;
            }
        }

        private sealed class TestGroup : StateGroup<Mode, int>
        {
            private readonly List<string> _log;

            public TestGroup(List<string> log)
            {
                _log = log;
            }

            public bool AllowPreemption { get; set; }

            public Mode Fallback { get; set; } = Mode.Idle;

            public void Add(IState<Mode, int> state)
            {
                AddState(state);
            }

            protected override Mode EmptyKey => Mode.Idle;

            protected override Mode SelectFallback(in int context)
            {
                return Fallback;
            }

            protected override bool TryDecidePreemption(in int context, out Mode target)
            {
                target = Mode.Run;
                return AllowPreemption;
            }

            protected override bool TryCommitPreemption(Mode target, in int context)
            {
                return AllowPreemption;
            }
        }
    }
}
