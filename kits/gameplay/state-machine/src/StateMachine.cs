using System;
using System.Collections.Generic;

namespace GameDevKit.StateMachine
{
    public interface IState<TKey, TContext>
        where TKey : struct, Enum
    {
        TKey Key { get; }

        void Enter(in TContext context);

        void Exit();

        void Tick(in TContext context);

        bool IsComplete(in TContext context);
    }

    public sealed class StateMachine<TKey, TContext>
        where TKey : struct, Enum
    {
        private readonly Dictionary<TKey, IState<TKey, TContext>> _states = new();
        private Action<TKey, TKey>? _stateChanged;

        public IState<TKey, TContext>? Current { get; private set; }

        public event Action<TKey, TKey>? StateChanged
        {
            add => _stateChanged += value;
            remove => _stateChanged -= value;
        }

        public void Add(IState<TKey, TContext> state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _states[state.Key] = state;
        }

        public bool TryChange(TKey key, in TContext context)
        {
            if (!_states.TryGetValue(key, out IState<TKey, TContext>? next))
            {
                return false;
            }

            if (ReferenceEquals(Current, next))
            {
                return false;
            }

            IState<TKey, TContext>? previous = Current;
            TKey previousKey = previous == null ? default : previous.Key;

            previous?.Exit();
            Current = next;
            next.Enter(in context);

            if (previous != null)
            {
                _stateChanged?.Invoke(next.Key, previousKey);
            }

            return true;
        }

        public void Tick(in TContext context)
        {
            Current?.Tick(in context);
        }
    }

    public abstract class StateGroup<TKey, TContext>
        where TKey : struct, Enum
    {
        private readonly StateMachine<TKey, TContext> _machine = new();

        protected abstract TKey EmptyKey { get; }

        public TKey Current
        {
            get => _machine.Current == null ? EmptyKey : _machine.Current.Key;
        }

        protected abstract TKey SelectFallback(in TContext context);

        protected virtual bool TryDecidePreemption(in TContext context, out TKey target)
        {
            target = EmptyKey;
            return false;
        }

        protected virtual bool TryCommitPreemption(TKey target, in TContext context)
        {
            return false;
        }

        protected void AddState(IState<TKey, TContext> state)
        {
            _machine.Add(state);
        }

        public void Tick(in TContext context)
        {
            IState<TKey, TContext>? current = _machine.Current;

            if (TryDecidePreemption(in context, out TKey target)
                && (current == null || !EqualityComparer<TKey>.Default.Equals(target, current.Key))
                && TryCommitPreemption(target, in context))
            {
                _machine.TryChange(target, in context);
            }
            else if (current == null || current.IsComplete(in context))
            {
                _machine.TryChange(SelectFallback(in context), in context);
            }

            _machine.Tick(in context);
        }
    }
}
