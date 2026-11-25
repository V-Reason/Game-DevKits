using System;
using System.Collections.Generic;
using Xunit;

namespace GameDevKit.GridPropagation.Tests
{
    public sealed class GridPropagationTests
    {
        [Fact]
        public void DeferredQueueDeduplicatesUntilSwap()
        {
            var queue = new DeferredCellQueue();
            var cell = new GridPoint(1, 2);

            queue.Schedule(cell);
            queue.Schedule(cell);
            queue.Swap();

            Assert.Equal(new[] { cell }, queue.Current);

            queue.Schedule(cell);
            Assert.Single(queue.Current);
            queue.Swap();
            Assert.Equal(new[] { cell }, queue.Current);
        }

        [Fact]
        public void FloodVisitsEachReachableNodeOnceInDeterministicOrder()
        {
            var fill = new BoundedFloodFill<GridPoint>();
            var visited = new List<GridPoint>();

            FloodResult result = fill.Flood(
                new GridPoint(0, 0),
                VisitSquareNeighbors,
                point => Math.Abs(point.X) <= 1 && Math.Abs(point.Y) <= 1,
                visited.Add,
                64);

            Assert.Equal(9, result.VisitedCount);
            Assert.False(result.Truncated);
            Assert.Equal(new[]
            {
                new GridPoint(0, 0),
                new GridPoint(-1, 0),
                new GridPoint(0, -1),
                new GridPoint(1, 0),
                new GridPoint(0, 1),
                new GridPoint(-1, -1),
                new GridPoint(-1, 1),
                new GridPoint(1, -1),
                new GridPoint(1, 1),
            }, visited);
        }

        [Fact]
        public void BudgetStopsRingWithoutGrowingForever()
        {
            var fill = new BoundedFloodFill<GridPoint>();
            var visited = new List<GridPoint>();

            FloodResult result = fill.Flood(
                new GridPoint(0, 0),
                VisitRingNeighbors,
                _ => true,
                visited.Add,
                3);

            Assert.Equal(3, result.VisitedCount);
            Assert.True(result.Truncated);
            Assert.Equal(3, visited.Count);
        }

        [Fact]
        public void BlockedOriginIsNotVisited()
        {
            var fill = new BoundedFloodFill<int>();
            int visited = 0;

            FloodResult result = fill.Flood(
                1,
                (_, neighbors) => neighbors.Add(2),
                node => node != 1,
                _ => visited++,
                8);

            Assert.Equal(0, result.VisitedCount);
            Assert.Equal(0, visited);
        }

        private static void VisitSquareNeighbors(
            GridPoint node,
            List<GridPoint> neighbors)
        {
            neighbors.Add(new GridPoint(node.X - 1, node.Y));
            neighbors.Add(new GridPoint(node.X, node.Y - 1));
            neighbors.Add(new GridPoint(node.X + 1, node.Y));
            neighbors.Add(new GridPoint(node.X, node.Y + 1));
        }

        private static void VisitRingNeighbors(
            GridPoint node,
            List<GridPoint> neighbors)
        {
            neighbors.Add(new GridPoint(node.X + 1, node.Y));
            neighbors.Add(new GridPoint(node.X, node.Y + 1));
            neighbors.Add(new GridPoint(node.X - 1, node.Y));
            neighbors.Add(new GridPoint(node.X, node.Y - 1));
        }
    }
}
