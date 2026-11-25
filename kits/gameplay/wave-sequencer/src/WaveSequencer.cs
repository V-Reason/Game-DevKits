using System;
using System.Collections.Generic;

namespace GameDevKit.WaveSequencer
{
    public enum WavePhase
    {
        Prep,
        Battle,
        Settle,
    }

    public readonly struct WaveEnemy<TEnemy>
    {
        public WaveEnemy(TEnemy enemy, int count)
        {
            Enemy = enemy;
            Count = count;
        }

        public TEnemy Enemy { get; }

        public int Count { get; }
    }

    public sealed class WavePlan<TEnemy, TGrant>
    {
        public WavePlan(
            double prepSeconds,
            double battleSeconds,
            double settleSeconds,
            double spawnInterval,
            TGrant grant,
            IReadOnlyList<WaveEnemy<TEnemy>> enemies)
        {
            PrepSeconds = prepSeconds;
            BattleSeconds = battleSeconds;
            SettleSeconds = settleSeconds;
            SpawnInterval = spawnInterval;
            Grant = grant;
            Enemies = enemies;
        }

        public double PrepSeconds { get; }

        public double BattleSeconds { get; }

        public double SettleSeconds { get; }

        public double SpawnInterval { get; }

        public TGrant Grant { get; }

        public IReadOnlyList<WaveEnemy<TEnemy>> Enemies { get; }
    }

    public sealed class WaveRunnerOptions
    {
        public bool Loop { get; set; }

        public int MaxTransitionsPerTick { get; set; } = 3;
    }

    public readonly struct WaveSpawn<TEnemy>
    {
        public WaveSpawn(TEnemy enemy, int waveIndex, int remaining)
        {
            Enemy = enemy;
            WaveIndex = waveIndex;
            Remaining = remaining;
        }

        public TEnemy Enemy { get; }

        public int WaveIndex { get; }

        public int Remaining { get; }
    }

    public readonly struct WavePhaseChange<TGrant>
    {
        public WavePhaseChange(int waveIndex, WavePhase phase, TGrant grant, double remaining)
        {
            WaveIndex = waveIndex;
            Phase = phase;
            Grant = grant;
            Remaining = remaining;
        }

        public int WaveIndex { get; }

        public WavePhase Phase { get; }

        public TGrant Grant { get; }

        public double Remaining { get; }
    }

    public sealed class WaveSnapshot
    {
        public int WaveCursor { get; set; }

        public int WaveIndex { get; set; }

        public int Phase { get; set; }

        public double Timer { get; set; }

        public double SpawnTimer { get; set; }

        public int SpawnCursor { get; set; }

        public bool Entered { get; set; }

        public bool Completed { get; set; }

        public bool SettleStarted { get; set; }
    }

    public sealed class WaveRunner<TEnemy, TGrant>
        where TEnemy : notnull
    {
        private readonly WavePlan<TEnemy, TGrant>[] _waves;
        private readonly WaveRunnerOptions _options;
        private readonly List<TEnemy> _pendingEnemies = new();
        private readonly Queue<WaveSpawn<TEnemy>> _spawns = new();
        private readonly List<WavePhaseChange<TGrant>> _phaseChanges = new();
        private int _waveCursor;
        private int _waveIndex = 1;
        private int _spawnCursor;
        private WavePhase _phase = WavePhase.Prep;
        private double _timer;
        private double _spawnTimer;
        private bool _entered;
        private bool _completed;
        private bool _settleStarted;
        private TGrant _grant = default!;
        private bool _hasGrant;

        public WaveRunner(
            IReadOnlyList<WavePlan<TEnemy, TGrant>> waves,
            WaveRunnerOptions? options = null)
        {
            if (waves == null)
            {
                throw new ArgumentNullException(nameof(waves));
            }

            if (waves.Count == 0)
            {
                throw new ArgumentException("At least one wave plan is required.", nameof(waves));
            }

            _waves = new WavePlan<TEnemy, TGrant>[waves.Count];

            for (int i = 0; i < waves.Count; i++)
            {
                WavePlan<TEnemy, TGrant> plan = waves[i]
                    ?? throw new ArgumentException("Wave plan cannot be null.", nameof(waves));

                if (plan.Enemies == null)
                {
                    throw new ArgumentException("Wave enemy list cannot be null.", nameof(waves));
                }

                _waves[i] = plan;
            }

            _options = options ?? new WaveRunnerOptions();

            if (_options.MaxTransitionsPerTick <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }
        }

        public WavePhase Phase => _phase;

        public int WaveIndex => _waveIndex;

        public int Remaining => _pendingEnemies.Count - _spawnCursor;

        public double PhaseRemaining => Math.Max(0, _timer);

        public bool IsCompleted => _completed;

        public void Tick(double deltaTime, bool anyEnemyAlive)
        {
            if (deltaTime < 0
                || double.IsNaN(deltaTime)
                || double.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (_completed)
            {
                return;
            }

            if (!_entered)
            {
                EnterPhase(WavePhase.Prep);
            }

            _timer -= deltaTime;

            if (_phase == WavePhase.Battle
                && _spawnCursor >= _pendingEnemies.Count
                && !anyEnemyAlive)
            {
                _timer = 0;
            }

            int transitions = 0;

            while (_timer <= 0 && transitions < _options.MaxTransitionsPerTick)
            {
                if (!AdvancePhase(anyEnemyAlive))
                {
                    break;
                }

                transitions++;

                if (_completed)
                {
                    break;
                }
            }

            if (_phase == WavePhase.Battle)
            {
                SpawnDue(deltaTime);
            }
        }

        public bool TryConsumeGrant(out TGrant grant)
        {
            if (!_hasGrant)
            {
                grant = default!;
                return false;
            }

            grant = _grant;
            _grant = default!;
            _hasGrant = false;
            return true;
        }

        public bool TryDequeueSpawn(out WaveSpawn<TEnemy> spawn)
        {
            return _spawns.TryDequeue(out spawn);
        }

        public IReadOnlyList<WavePhaseChange<TGrant>> DrainPhaseChanges()
        {
            if (_phaseChanges.Count == 0)
            {
                return Array.Empty<WavePhaseChange<TGrant>>();
            }

            WavePhaseChange<TGrant>[] changes = _phaseChanges.ToArray();
            _phaseChanges.Clear();
            return changes;
        }

        public WaveSnapshot ExportSnapshot()
        {
            return new WaveSnapshot
            {
                WaveCursor = _waveCursor,
                WaveIndex = _waveIndex,
                Phase = (int)_phase,
                Timer = PhaseRemaining,
                SpawnTimer = _spawnTimer,
                SpawnCursor = _spawnCursor,
                Entered = _entered,
                Completed = _completed,
                SettleStarted = _settleStarted,
            };
        }

        public void RestoreSnapshot(WaveSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (!Enum.IsDefined(typeof(WavePhase), snapshot.Phase))
            {
                throw new ArgumentException("Snapshot wave phase is invalid.", nameof(snapshot));
            }

            _waveCursor = Math.Clamp(snapshot.WaveCursor, 0, _waves.Length - 1);
            _waveIndex = snapshot.WaveIndex <= 0 ? 1 : snapshot.WaveIndex;
            _phase = (WavePhase)snapshot.Phase;
            _timer = Sanitize(snapshot.Timer);
            _spawnTimer = Sanitize(snapshot.SpawnTimer);
            _entered = snapshot.Entered;
            _completed = snapshot.Completed;
            _settleStarted = snapshot.SettleStarted;
            _grant = default!;
            _hasGrant = false;
            _spawns.Clear();
            _phaseChanges.Clear();
            FillPending(Current);
            _spawnCursor = Math.Clamp(snapshot.SpawnCursor, 0, _pendingEnemies.Count);
        }

        private WavePlan<TEnemy, TGrant> Current => _waves[_waveCursor];

        private bool AdvancePhase(bool anyEnemyAlive)
        {
            switch (_phase)
            {
                case WavePhase.Prep:
                    EnterPhase(WavePhase.Battle);
                    return true;

                case WavePhase.Battle:
                    if (_spawnCursor < _pendingEnemies.Count || anyEnemyAlive)
                    {
                        return false;
                    }

                    EnterPhase(WavePhase.Settle);
                    return true;

                case WavePhase.Settle:
                    if (anyEnemyAlive)
                    {
                        return false;
                    }

                    if (!_options.Loop && _waveCursor == _waves.Length - 1)
                    {
                        _completed = true;
                        _timer = 0;
                        return false;
                    }

                    _waveCursor = (_waveCursor + 1) % _waves.Length;
                    _waveIndex++;
                    EnterPhase(WavePhase.Prep);
                    return true;

                default:
                    return false;
            }
        }

        private void EnterPhase(WavePhase phase)
        {
            _phase = phase;
            _entered = true;
            _timer = DurationOf(phase);

            if (phase == WavePhase.Prep)
            {
                _grant = Current.Grant;
                _hasGrant = true;
                _spawnTimer = 0;
                _settleStarted = false;
            }
            else if (phase == WavePhase.Battle)
            {
                FillPending(Current);
                _spawnTimer = 0;
            }
            else
            {
                _settleStarted = true;
            }

            _phaseChanges.Add(new WavePhaseChange<TGrant>(
                _waveIndex,
                phase,
                _grant,
                PhaseRemaining));
        }

        private void FillPending(WavePlan<TEnemy, TGrant> plan)
        {
            _pendingEnemies.Clear();
            _spawnCursor = 0;

            for (int i = 0; i < plan.Enemies.Count; i++)
            {
                WaveEnemy<TEnemy> entry = plan.Enemies[i];

                if (entry.Count <= 0)
                {
                    continue;
                }

                for (int count = 0; count < entry.Count; count++)
                {
                    _pendingEnemies.Add(entry.Enemy);
                }
            }
        }

        private void SpawnDue(double deltaTime)
        {
            _spawnTimer -= deltaTime;

            if (_spawnTimer > 0 || _spawnCursor >= _pendingEnemies.Count)
            {
                return;
            }

            TEnemy enemy = _pendingEnemies[_spawnCursor++];
            _spawns.Enqueue(new WaveSpawn<TEnemy>(
                enemy,
                _waveIndex,
                _pendingEnemies.Count - _spawnCursor));
            _spawnTimer = Math.Max(0.05, Current.SpawnInterval);
        }

        private double DurationOf(WavePhase phase)
        {
            switch (phase)
            {
                case WavePhase.Prep:
                    return Sanitize(Current.PrepSeconds);
                case WavePhase.Battle:
                    return Sanitize(Current.BattleSeconds);
                default:
                    return Sanitize(Current.SettleSeconds);
            }
        }

        private static double Sanitize(double seconds)
        {
            return double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0
                ? 0
                : seconds;
        }
    }
}
