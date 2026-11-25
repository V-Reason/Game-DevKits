using System.Collections.Generic;
using Xunit;

namespace GameDevKit.WaveSequencer.Tests
{
    public sealed class WaveSequencerTests
    {
        [Fact]
        public void PrepTransitionsToBattleAndEmitsGrant()
        {
            WaveRunner<string, string> runner = Runner(1, 2, 1);

            runner.Tick(1.05, false);

            Assert.Equal(WavePhase.Battle, runner.Phase);
            Assert.True(runner.TryConsumeGrant(out string grant));
            Assert.Equal("grant-1", grant);
            Assert.Contains(runner.DrainPhaseChanges(), change => change.Phase == WavePhase.Battle);
        }

        [Fact]
        public void BattleSpawnsAtConfiguredInterval()
        {
            WaveRunner<string, string> runner = Runner(0, 5, 0);
            runner.Tick(0, false);

            runner.Tick(0.01, false);
            Assert.True(runner.TryDequeueSpawn(out WaveSpawn<string> first));
            Assert.Equal("enemy-a", first.Enemy);
            Assert.Equal(1, first.Remaining);
            Assert.False(runner.TryDequeueSpawn(out _));

            runner.Tick(0.51, true);
            Assert.True(runner.TryDequeueSpawn(out WaveSpawn<string> second));
            Assert.Equal(0, second.Remaining);
        }

        [Fact]
        public void SettleCanBecomeComplete()
        {
            WaveRunner<string, string> runner = Runner(0, 0.2, 0.2, enemyCount: 1);
            runner.Tick(0, false);
            runner.Tick(0.01, false);
            Assert.True(runner.TryDequeueSpawn(out _));

            runner.Tick(0.25, false);
            Assert.Equal(WavePhase.Settle, runner.Phase);

            runner.Tick(0.25, false);
            Assert.True(runner.IsCompleted);
        }

        [Fact]
        public void LivingEnemyBlocksSettle()
        {
            WaveRunner<string, string> runner = Runner(0, 0.1, 0.2);
            runner.Tick(0, true);
            runner.Tick(0.2, true);

            Assert.Equal(WavePhase.Battle, runner.Phase);
        }

        [Fact]
        public void LoopStartsNextWaveWithoutCompleting()
        {
            WaveRunner<string, string> runner = Runner(0, 0, 0, loop: true);

            runner.Tick(0, false);
            runner.Tick(0, false);
            runner.Tick(0, false);

            Assert.False(runner.IsCompleted);
            Assert.True(runner.WaveIndex >= 1);
        }

        [Fact]
        public void SnapshotRestoresPhaseAndSpawnState()
        {
            WaveRunner<string, string> runner = Runner(0, 5, 0);
            runner.Tick(0, false);
            runner.Tick(0.01, false);
            runner.TryDequeueSpawn(out _);
            WaveSnapshot snapshot = runner.ExportSnapshot();

            WaveRunner<string, string> restored = Runner(0, 5, 0);
            restored.RestoreSnapshot(snapshot);
            restored.Tick(0.6, true);

            Assert.Equal(WavePhase.Battle, restored.Phase);
            Assert.True(restored.TryDequeueSpawn(out WaveSpawn<string> spawn));
            Assert.Equal("enemy-b", spawn.Enemy);
        }

        [Fact]
        public void SnapshotStoresWaveCursor()
        {
            var first = new WavePlan<string, string>(
                0,
                0,
                0,
                0.5,
                "one",
                new[] { new WaveEnemy<string>("a", 0) });
            var second = new WavePlan<string, string>(
                0,
                0,
                0,
                0.5,
                "two",
                new[] { new WaveEnemy<string>("b", 0) });
            var runner = new WaveRunner<string, string>(
                new[] { first, second },
                new WaveRunnerOptions { MaxTransitionsPerTick = 8 });

            runner.Tick(0, false);
            WaveSnapshot snapshot = runner.ExportSnapshot();

            Assert.Equal(2, snapshot.WaveIndex);
            Assert.Equal(1, snapshot.WaveCursor);
        }

        private static WaveRunner<string, string> Runner(
            double prep,
            double battle,
            double settle,
            bool loop = false,
            int enemyCount = 2)
        {
            var enemies = new List<WaveEnemy<string>>
            {
                new WaveEnemy<string>("enemy-a", 1),
            };

            if (enemyCount > 1)
            {
                enemies.Add(new WaveEnemy<string>("enemy-b", 1));
            }

            var waves = new[]
            {
                new WavePlan<string, string>(
                    prep,
                    battle,
                    settle,
                    0.5,
                    "grant-1",
                    enemies),
            };

            return new WaveRunner<string, string>(
                waves,
                new WaveRunnerOptions { Loop = loop, MaxTransitionsPerTick = 4 });
        }
    }
}
