using System.Collections.Generic;
using Xunit;

namespace GameDevKit.FlowField.Tests
{
    public sealed class FlowFieldTests
    {
        [Fact]
        public void OpenCorridorProvidesDistanceAndNextStep()
        {
            var grid = new FlowGrid(4, 1);
            var field = new FlowField(grid, new FlowCell(3, 0));

            Assert.Equal(3, field.GetDistance(new FlowCell(0, 0)));
            Assert.Equal(300, field.GetCost(new FlowCell(0, 0)));
            Assert.True(field.TryGetNext(new FlowCell(0, 0), out FlowCell next));
            Assert.Equal(new FlowCell(1, 0), next);
            Assert.False(field.TryGetNext(new FlowCell(3, 0), out _));
        }

        [Fact]
        public void BlockedBarrierMakesTargetUnreachable()
        {
            var grid = new FlowGrid(3, 2);

            for (int y = 0; y < 2; y++)
            {
                grid.SetBlocked(new FlowCell(1, y), true);
            }

            var field = new FlowField(grid, new FlowCell(2, 0));

            Assert.Equal(-1, field.GetDistance(new FlowCell(0, 0)));
            Assert.Equal(int.MaxValue, field.GetCost(new FlowCell(0, 0)));
            Assert.False(field.TryGetNext(new FlowCell(0, 0), out _));
        }

        [Fact]
        public void PreferredCorridorCanWinWithinDetourBudget()
        {
            var policy = new DictionaryCostPolicy();

            for (int x = 0; x < 5; x++)
            {
                policy.SetCost(new FlowCell(x, 0), 50);
            }

            var grid = new FlowGrid(5, 3, policy);
            var field = new FlowField(
                grid,
                new FlowCell(4, 1),
                new FlowFieldOptions { NormalStepCost = 100, MaxDetourSteps = 2 });

            Assert.Equal(350, field.GetCost(new FlowCell(0, 1)));
            Assert.True(field.TryGetNext(new FlowCell(0, 1), out FlowCell next));
            Assert.Equal(new FlowCell(0, 0), next);
        }

        [Fact]
        public void CacheRebuildsOnlyForGoalOrTerrainChanges()
        {
            var grid = new FlowGrid(5, 3);
            var cache = new FlowFieldCache(grid);

            FlowField first = cache.Get(new FlowCell(4, 1));
            Assert.Same(first, cache.Get(new FlowCell(4, 1)));
            Assert.Equal(1, cache.BuildCount);

            grid.SetBlocked(new FlowCell(2, 1), true);
            cache.Get(new FlowCell(4, 1));
            Assert.Equal(2, cache.BuildCount);

            cache.Get(new FlowCell(3, 2));
            Assert.Equal(3, cache.BuildCount);
        }

        private sealed class DictionaryCostPolicy : IFlowCostPolicy
        {
            private readonly Dictionary<FlowCell, int> _costs = new();

            public void SetCost(FlowCell cell, int cost)
            {
                _costs[cell] = cost;
            }

            public int GetStepCost(FlowCell cell)
            {
                return _costs.TryGetValue(cell, out int cost) ? cost : 100;
            }
        }
    }
}
