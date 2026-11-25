using System;
using System.Collections.Generic;

namespace GameDevKit.EffectOrchestrator
{
    public readonly struct EffectHandle : IEquatable<EffectHandle>
    {
        public EffectHandle(int id, int generation, object owner)
        {
            Id = id;
            Generation = generation;
            Owner = owner;
        }

        public int Id { get; }

        public int Generation { get; }

        public object? Owner { get; }

        public bool IsValid => Id != 0 && Owner != null;

        public static EffectHandle None => default;

        public bool Equals(EffectHandle other)
        {
            return Id == other.Id
                && Generation == other.Generation
                && ReferenceEquals(Owner, other.Owner);
        }

        public override bool Equals(object? obj)
        {
            return obj is EffectHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Id * 397) ^ Generation) ^ (Owner == null ? 0 : Owner.GetHashCode());
            }
        }
    }

    public readonly struct EffectDriverStats
    {
        public EffectDriverStats(int active, int pooled)
        {
            Active = active;
            Pooled = pooled;
        }

        public int Active { get; }

        public int Pooled { get; }
    }

    public readonly struct EffectIssue
    {
        public EffectIssue(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }

        public string Message { get; }
    }

    public interface IEffectDriver<TEffectId, TContext>
        where TContext : struct
    {
        bool IsReady { get; }

        bool IsHandleValid(EffectHandle handle);

        EffectHandle Play(TEffectId id, in TContext context);

        void Update(EffectHandle handle, in TContext context);

        void Stop(EffectHandle handle);

        void Tick(double deltaTime);

        void ClearAll();

        EffectDriverStats GetStats();
    }

    public sealed class EffectRouterOptions
    {
        public int MaxPendingPerEffect { get; set; } = 128;
    }

    public readonly struct EffectRouterStats
    {
        public EffectRouterStats(int drivers, int active, int pooled, int pending)
        {
            Drivers = drivers;
            Active = active;
            Pooled = pooled;
            Pending = pending;
        }

        public int Drivers { get; }

        public int Active { get; }

        public int Pooled { get; }

        public int Pending { get; }
    }

    public sealed class EffectRouter<TEffectId, TContext>
        where TEffectId : notnull
        where TContext : struct
    {
        private readonly EffectRouterOptions _options;
        private readonly Action<EffectIssue>? _reportIssue;
        private readonly Dictionary<TEffectId, IEffectDriver<TEffectId, TContext>> _drivers = new();
        private readonly Dictionary<TEffectId, Queue<TContext>> _pending = new();
        private readonly List<TEffectId> _scratch = new();

        public EffectRouter(
            EffectRouterOptions? options = null,
            Action<EffectIssue>? reportIssue = null)
        {
            _options = options ?? new EffectRouterOptions();
            _reportIssue = reportIssue;

            if (_options.MaxPendingPerEffect <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    "MaxPendingPerEffect must be positive.");
            }
        }

        public void Register(TEffectId id, IEffectDriver<TEffectId, TContext> driver)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            if (_drivers.ContainsKey(id))
            {
                throw new ArgumentException($"Driver already registered for '{id}'.", nameof(id));
            }

            _drivers.Add(id, driver);
        }

        public EffectHandle Play(TEffectId id, in TContext context)
        {
            if (!_drivers.TryGetValue(id, out IEffectDriver<TEffectId, TContext>? driver))
            {
                _reportIssue?.Invoke(new EffectIssue(
                    "unknown-driver",
                    $"No effect driver registered for '{id}'."));
                return EffectHandle.None;
            }

            if (!driver.IsReady)
            {
                EnqueuePending(id, in context);
                return EffectHandle.None;
            }

            return driver.Play(id, in context);
        }

        public bool Update(EffectHandle handle, in TContext context)
        {
            if (!TryFindDriver(handle, out IEffectDriver<TEffectId, TContext>? driver))
            {
                return false;
            }

            driver!.Update(handle, in context);
            return true;
        }

        public void Stop(EffectHandle handle)
        {
            if (TryFindDriver(handle, out IEffectDriver<TEffectId, TContext>? driver))
            {
                driver!.Stop(handle);
            }
        }

        public void Tick(double deltaTime)
        {
            if (deltaTime < 0 || double.IsNaN(deltaTime) || double.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            _scratch.Clear();
            _scratch.AddRange(_drivers.Keys);

            for (int i = 0; i < _scratch.Count; i++)
            {
                TEffectId id = _scratch[i];
                IEffectDriver<TEffectId, TContext> driver = _drivers[id];
                driver.Tick(deltaTime);

                if (!driver.IsReady || !_pending.TryGetValue(id, out Queue<TContext>? queue))
                {
                    continue;
                }

                _pending.Remove(id);

                while (queue.Count > 0)
                {
                    TContext context = queue.Dequeue();
                    driver.Play(id, in context);
                }
            }
        }

        public void ClearAll()
        {
            foreach (IEffectDriver<TEffectId, TContext> driver in _drivers.Values)
            {
                driver.ClearAll();
            }

            _pending.Clear();
        }

        public EffectRouterStats GetStats()
        {
            int active = 0;
            int pooled = 0;
            int pending = 0;

            foreach (IEffectDriver<TEffectId, TContext> driver in _drivers.Values)
            {
                EffectDriverStats stats = driver.GetStats();
                active += stats.Active;
                pooled += stats.Pooled;
            }

            foreach (Queue<TContext> queue in _pending.Values)
            {
                pending += queue.Count;
            }

            return new EffectRouterStats(_drivers.Count, active, pooled, pending);
        }

        private void EnqueuePending(TEffectId id, in TContext context)
        {
            if (!_pending.TryGetValue(id, out Queue<TContext>? queue))
            {
                queue = new Queue<TContext>();
                _pending.Add(id, queue);
            }

            if (queue.Count >= _options.MaxPendingPerEffect)
            {
                queue.Dequeue();
                _reportIssue?.Invoke(new EffectIssue(
                    "pending-overflow",
                    $"Pending effect queue for '{id}' dropped its oldest request."));
            }

            queue.Enqueue(context);
        }

        private bool TryFindDriver(
            EffectHandle handle,
            out IEffectDriver<TEffectId, TContext>? driver)
        {
            if (!handle.IsValid)
            {
                driver = null;
                return false;
            }

            foreach (IEffectDriver<TEffectId, TContext> candidate in _drivers.Values)
            {
                if (!candidate.IsHandleValid(handle))
                {
                    continue;
                }

                driver = candidate;
                return true;
            }

            driver = null;
            return false;
        }
    }
}
