using System;
using System.Collections.Generic;

namespace GameDevKit.GridPropagation
{
    public readonly struct GridPoint : IEquatable<GridPoint>
    {
        public GridPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public bool Equals(GridPoint other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object? obj)
        {
            return obj is GridPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (X * 397) ^ Y;
        }

        public static bool operator ==(GridPoint left, GridPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPoint left, GridPoint right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }

    public sealed class DeferredCellQueue
    {
        private List<GridPoint> _current = new();
        private List<GridPoint> _next = new();
        private readonly HashSet<GridPoint> _scheduled = new();

        public IReadOnlyList<GridPoint> Current => _current;

        public void Schedule(GridPoint cell)
        {
            if (_scheduled.Add(cell))
            {
                _next.Add(cell);
            }
        }

        public void Swap()
        {
            (_current, _next) = (_next, _current);
            _next.Clear();
            _scheduled.Clear();
        }
    }

    public delegate void NeighborVisitor<TNode>(TNode node, List<TNode> neighbors);

    public readonly struct FloodResult
    {
        public FloodResult(int visitedCount, bool truncated)
        {
            VisitedCount = visitedCount;
            Truncated = truncated;
        }

        public int VisitedCount { get; }

        public bool Truncated { get; }
    }

    public sealed class BoundedFloodFill<TNode>
        where TNode : notnull
    {
        private readonly Queue<TNode> _queue;
        private readonly HashSet<TNode> _visited;
        private readonly List<TNode> _neighbors = new();

        public BoundedFloodFill(IEqualityComparer<TNode>? comparer = null)
        {
            _queue = new Queue<TNode>();
            _visited = new HashSet<TNode>(comparer ?? EqualityComparer<TNode>.Default);
        }

        public FloodResult Flood(
            TNode origin,
            NeighborVisitor<TNode> neighborVisitor,
            Func<TNode, bool> canVisit,
            Action<TNode> visit,
            int maxVisits)
        {
            if (neighborVisitor == null)
            {
                throw new ArgumentNullException(nameof(neighborVisitor));
            }

            if (canVisit == null)
            {
                throw new ArgumentNullException(nameof(canVisit));
            }

            if (visit == null)
            {
                throw new ArgumentNullException(nameof(visit));
            }

            if (maxVisits <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxVisits));
            }

            _queue.Clear();
            _visited.Clear();

            if (!canVisit(origin))
            {
                return new FloodResult(0, false);
            }

            _queue.Enqueue(origin);
            _visited.Add(origin);

            int visitedCount = 0;
            bool truncated = false;

            while (_queue.Count > 0)
            {
                TNode current = _queue.Dequeue();
                visit(current);
                visitedCount++;

                _neighbors.Clear();
                neighborVisitor(current, _neighbors);

                for (int i = 0; i < _neighbors.Count; i++)
                {
                    TNode next = _neighbors[i];

                    if (!canVisit(next) || !_visited.Add(next))
                    {
                        continue;
                    }

                    _queue.Enqueue(next);
                }

                if (visitedCount >= maxVisits)
                {
                    truncated = _queue.Count > 0;
                    break;
                }
            }

            return new FloodResult(visitedCount, truncated);
        }
    }
}
