using System;
using System.Collections.Generic;

namespace GameDevKit.TimerWheel
{
    public readonly struct TimerHandle : IEquatable<TimerHandle>
    {
        public static readonly TimerHandle Invalid = new(0);

        public TimerHandle(int id)
        {
            Id = id;
        }

        public int Id { get; }

        public bool IsValid => Id != 0;

        public bool Equals(TimerHandle other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is TimerHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static bool operator ==(TimerHandle left, TimerHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TimerHandle left, TimerHandle right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct TimerOptions
    {
        public TimerOptions(double delay, bool repeat = false, double interval = 0)
        {
            Delay = delay;
            Repeat = repeat;
            Interval = interval;
        }

        public double Delay { get; }

        public bool Repeat { get; }

        public double Interval { get; }

        public static TimerOptions Once(double delay)
        {
            return new TimerOptions(delay);
        }

        public static TimerOptions Loop(double interval)
        {
            return new TimerOptions(interval, true, interval);
        }
    }

    public readonly struct TimerInfo
    {
        public TimerInfo(
            double duration,
            double elapsedTime,
            double remainingTime,
            double progress,
            bool isRepeating)
        {
            Duration = duration;
            ElapsedTime = elapsedTime;
            RemainingTime = remainingTime;
            Progress = progress;
            IsRepeating = isRepeating;
        }

        public double Duration { get; }

        public double ElapsedTime { get; }

        public double RemainingTime { get; }

        public double Progress { get; }

        public bool IsRepeating { get; }
    }

    public sealed class TimerWheel
    {
        private sealed class TimerNode
        {
            public TimerHandle Handle;
            public int TargetSlot;
            public int RemainingRounds;
            public bool Repeat;
            public double Interval;
            public double PeriodStartTime;
            public double ExpireTime;
            public Action? Callback;
            public bool Cancelled;
        }

        private readonly List<TimerNode>[] _slots;
        private readonly Dictionary<int, TimerNode> _timers = new();
        private readonly double _slotDuration;
        private readonly int _slotCount;
        private int _currentSlot;
        private double _nextSlotTime;
        private double _time;
        private int _nextId = 1;

        public TimerWheel(double slotDuration = 0.1, int slotCount = 512)
        {
            if (double.IsNaN(slotDuration)
                || double.IsInfinity(slotDuration)
                || slotDuration <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotDuration));
            }

            if (slotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            }

            _slotDuration = slotDuration;
            _slotCount = slotCount;
            _slots = new List<TimerNode>[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                _slots[i] = new List<TimerNode>();
            }

            _nextSlotTime = slotDuration;
        }

        public double Time => _time;

        public int Count => _timers.Count;

        public TimerHandle Schedule(TimerOptions options, Action callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (double.IsNaN(options.Delay)
                || double.IsInfinity(options.Delay)
                || options.Delay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            if (options.Repeat
                && (double.IsNaN(options.Interval)
                    || double.IsInfinity(options.Interval)
                    || options.Interval <= 0))
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }

            int id = NextId();
            var handle = new TimerHandle(id);
            var node = new TimerNode
            {
                Handle = handle,
                Repeat = options.Repeat,
                Interval = options.Repeat ? options.Interval : 0,
                PeriodStartTime = _time,
                ExpireTime = _time + options.Delay,
                Callback = callback,
            };

            _timers.Add(id, node);
            AddToWheel(node);
            return handle;
        }

        public void Advance(double deltaTime)
        {
            if (double.IsNaN(deltaTime) || double.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (deltaTime < 0)
            {
                deltaTime = 0;
            }

            _time += deltaTime;

            int processedSlots = 0;

            while (_time >= _nextSlotTime && processedSlots < _slotCount)
            {
                _currentSlot = (_currentSlot + 1) % _slotCount;
                ProcessSlot(_currentSlot);
                _nextSlotTime += _slotDuration;
                processedSlots++;
            }

            if (_time >= _nextSlotTime)
            {
                _nextSlotTime = _time + _slotDuration;
            }
        }

        public bool Cancel(TimerHandle handle)
        {
            if (!handle.IsValid || !_timers.TryGetValue(handle.Id, out TimerNode? node))
            {
                return false;
            }

            node.Cancelled = true;
            node.Callback = null;
            _timers.Remove(handle.Id);
            return true;
        }

        public bool TryGetInfo(TimerHandle handle, out TimerInfo info)
        {
            if (!_timers.TryGetValue(handle.Id, out TimerNode? node))
            {
                info = default;
                return false;
            }

            double elapsed = Math.Max(0, _time - node.PeriodStartTime);
            double remaining = Math.Max(0, node.ExpireTime - _time);
            double duration = Math.Max(0, node.ExpireTime - node.PeriodStartTime);
            double progress = duration <= 0 ? 1 : Math.Min(1, elapsed / duration);

            info = new TimerInfo(duration, elapsed, remaining, progress, node.Repeat);
            return true;
        }

        public void Clear()
        {
            _timers.Clear();

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Clear();
            }

            _currentSlot = 0;
            _nextSlotTime = _slotDuration;
            _time = 0;
            _nextId = 1;
        }

        private void ProcessSlot(int slotIndex)
        {
            List<TimerNode> slot = _slots[slotIndex];

            if (slot.Count == 0)
            {
                return;
            }

            TimerNode[] due = slot.ToArray();
            slot.Clear();

            for (int i = 0; i < due.Length; i++)
            {
                TimerNode node = due[i];

                if (node.Cancelled)
                {
                    continue;
                }

                if (node.RemainingRounds > 0)
                {
                    node.RemainingRounds--;
                    Requeue(node);
                    continue;
                }

                if (_time < node.ExpireTime)
                {
                    Requeue(node);
                    continue;
                }

                Execute(node);
            }
        }

        private void Execute(TimerNode node)
        {
            if (node.Cancelled)
            {
                return;
            }

            if (!node.Repeat)
            {
                _timers.Remove(node.Handle.Id);
            }

            Action? callback = node.Callback;

            try
            {
                callback?.Invoke();
            }
            finally
            {
                if (!node.Repeat)
                {
                    node.Callback = null;
                }
            }

            if (node.Cancelled || !node.Repeat || !_timers.ContainsKey(node.Handle.Id))
            {
                return;
            }

            node.PeriodStartTime = node.ExpireTime;
            node.ExpireTime += node.Interval;

            if (node.ExpireTime <= _time)
            {
                node.PeriodStartTime = _time;
                node.ExpireTime = _time + node.Interval;
            }

            AddToWheel(node);
        }

        private void AddToWheel(TimerNode node)
        {
            double remaining = Math.Max(0, node.ExpireTime - _time);
            int ticks = Math.Max(1, (int)Math.Ceiling(remaining / _slotDuration));
            node.TargetSlot = (_currentSlot + ticks) % _slotCount;
            node.RemainingRounds = (ticks - 1) / _slotCount;
            _slots[node.TargetSlot].Add(node);
        }

        private void Requeue(TimerNode node)
        {
            double remaining = node.ExpireTime - _time;

            if (remaining <= 0)
            {
                node.TargetSlot = (_currentSlot + 1) % _slotCount;
                _slots[node.TargetSlot].Add(node);
                return;
            }

            int ticks = Math.Max(1, (int)Math.Ceiling(remaining / _slotDuration));
            node.TargetSlot = (_currentSlot + ticks) % _slotCount;
            node.RemainingRounds = (ticks - 1) / _slotCount;
            _slots[node.TargetSlot].Add(node);
        }

        private int NextId()
        {
            int id = _nextId++;

            if (_nextId == int.MaxValue)
            {
                _nextId = 1;
            }

            return id;
        }
    }

    public sealed class TimerService
    {
        private const double MaxDeltaTime = 0.1;

        private readonly struct Registration
        {
            public Registration(TimerHandle wheelHandle, bool unscaled)
            {
                WheelHandle = wheelHandle;
                Unscaled = unscaled;
            }

            public TimerHandle WheelHandle { get; }

            public bool Unscaled { get; }
        }

        private readonly TimerWheel _scaledWheel;
        private readonly TimerWheel _unscaledWheel;
        private readonly Dictionary<int, Registration> _registrations = new();
        private int _nextHandleId = 1;

        public TimerService(double slotDuration = 0.1, int slotCount = 512)
        {
            _scaledWheel = new TimerWheel(slotDuration, slotCount);
            _unscaledWheel = new TimerWheel(slotDuration, slotCount);
        }

        public int Count => _registrations.Count;

        public void Advance(double scaledDeltaTime, double unscaledDeltaTime)
        {
            _scaledWheel.Advance(Math.Min(MaxDeltaTime, Math.Max(0, scaledDeltaTime)));
            _unscaledWheel.Advance(Math.Min(MaxDeltaTime, Math.Max(0, unscaledDeltaTime)));
        }

        public TimerHandle Schedule(double delay, Action callback)
        {
            return ScheduleOneShot(_scaledWheel, delay, callback, false);
        }

        public TimerHandle ScheduleUnscaled(double delay, Action callback)
        {
            return ScheduleOneShot(_unscaledWheel, delay, callback, true);
        }

        public TimerHandle ScheduleRepeating(double interval, Action callback)
        {
            return ScheduleRepeating(_scaledWheel, interval, callback, false);
        }

        public TimerHandle ScheduleRepeatingUnscaled(double interval, Action callback)
        {
            return ScheduleRepeating(_unscaledWheel, interval, callback, true);
        }

        public bool Cancel(TimerHandle handle)
        {
            if (!handle.IsValid || !_registrations.TryGetValue(handle.Id, out Registration registration))
            {
                return false;
            }

            _registrations.Remove(handle.Id);
            TimerWheel wheel = registration.Unscaled ? _unscaledWheel : _scaledWheel;
            return wheel.Cancel(registration.WheelHandle);
        }

        public void Clear()
        {
            _registrations.Clear();
            _scaledWheel.Clear();
            _unscaledWheel.Clear();
            _nextHandleId = 1;
        }

        private TimerHandle ScheduleOneShot(
            TimerWheel wheel,
            double delay,
            Action callback,
            bool unscaled)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            int serviceId = NextHandleId();
            TimerHandle local = wheel.Schedule(TimerOptions.Once(delay), () =>
            {
                _registrations.Remove(serviceId);
                callback();
            });

            _registrations.Add(serviceId, new Registration(local, unscaled));
            return new TimerHandle(serviceId);
        }

        private TimerHandle ScheduleRepeating(
            TimerWheel wheel,
            double interval,
            Action callback,
            bool unscaled)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            int serviceId = NextHandleId();
            TimerHandle local = wheel.Schedule(TimerOptions.Loop(interval), callback);
            _registrations.Add(serviceId, new Registration(local, unscaled));
            return new TimerHandle(serviceId);
        }

        private int NextHandleId()
        {
            int id = _nextHandleId++;

            if (_nextHandleId == int.MaxValue)
            {
                _nextHandleId = 1;
            }

            return id;
        }
    }
}
