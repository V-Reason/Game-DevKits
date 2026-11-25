using System;

namespace GameDevKit.FlowField
{
    public readonly struct FlowCell : IEquatable<FlowCell>
    {
        public FlowCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public bool Equals(FlowCell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object? obj)
        {
            return obj is FlowCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (X * 397) ^ Y;
        }

        public static bool operator ==(FlowCell left, FlowCell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FlowCell left, FlowCell right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }

    public interface IFlowCostPolicy
    {
        int GetStepCost(FlowCell cell);
    }

    public sealed class UniformFlowCostPolicy : IFlowCostPolicy
    {
        private readonly int _stepCost;

        public UniformFlowCostPolicy(int stepCost = 100)
        {
            if (stepCost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stepCost));
            }

            _stepCost = stepCost;
        }

        public int GetStepCost(FlowCell cell)
        {
            return _stepCost;
        }
    }

    public sealed class FlowGrid
    {
        private readonly bool[] _blocked;

        public FlowGrid(int width, int height, IFlowCostPolicy? costPolicy = null)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            CostPolicy = costPolicy ?? new UniformFlowCostPolicy();
            _blocked = new bool[width * height];
        }

        public int Width { get; }

        public int Height { get; }

        public int TerrainVersion { get; private set; }

        public IFlowCostPolicy CostPolicy { get; }

        public bool IsInside(FlowCell cell)
        {
            return cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;
        }

        public bool IsBlocked(FlowCell cell)
        {
            return !IsInside(cell) || _blocked[Index(cell)];
        }

        public void SetBlocked(FlowCell cell, bool blocked)
        {
            if (!IsInside(cell))
            {
                throw new ArgumentOutOfRangeException(nameof(cell));
            }

            int index = Index(cell);

            if (_blocked[index] == blocked)
            {
                return;
            }

            _blocked[index] = blocked;
            TerrainVersion++;
        }

        private int Index(FlowCell cell)
        {
            return cell.Y * Width + cell.X;
        }
    }

    public sealed class FlowFieldOptions
    {
        public int NormalStepCost { get; set; } = 100;

        public int MaxDetourSteps { get; set; } = 2;
    }

    public sealed class FlowField
    {
        private readonly FlowGrid _grid;
        private readonly FlowFieldOptions _options;
        private readonly int[] _distance;
        private readonly int[] _cost;
        private readonly int[] _queue;
        private readonly int[] _heapNodes;
        private readonly int[] _heapCosts;
        private readonly int[] _heapPositions;
        private int _heapCount;

        public FlowField(FlowGrid grid, FlowCell goal, FlowFieldOptions? options = null)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            Goal = goal;
            _options = options ?? new FlowFieldOptions();

            if (_options.NormalStepCost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "NormalStepCost must be positive.");
            }

            if (_options.MaxDetourSteps < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MaxDetourSteps cannot be negative.");
            }

            int count = grid.Width * grid.Height;
            _distance = new int[count];
            _cost = new int[count];
            _queue = new int[count];
            _heapNodes = new int[count];
            _heapCosts = new int[count];
            _heapPositions = new int[count];

            Build();
        }

        public FlowCell Goal { get; }

        public int GetDistance(FlowCell cell)
        {
            return _grid.IsInside(cell) ? _distance[Index(cell)] : -1;
        }

        public int GetCost(FlowCell cell)
        {
            return _grid.IsInside(cell) ? _cost[Index(cell)] : int.MaxValue;
        }

        public bool TryGetNext(FlowCell from, out FlowCell next)
        {
            next = default;

            if (!_grid.IsInside(from) || from == Goal)
            {
                return false;
            }

            int currentCost = GetCost(from);

            if (currentCost == int.MaxValue)
            {
                return false;
            }

            bool found = false;
            int bestCost = int.MaxValue;
            FlowCell best = default;

            VisitNeighbors(from, (candidate, candidateCost) =>
            {
                if (candidateCost >= currentCost || candidateCost >= bestCost)
                {
                    return;
                }

                bestCost = candidateCost;
                best = candidate;
                found = true;
            });

            if (!found)
            {
                return false;
            }

            next = best;
            return true;
        }

        private void Build()
        {
            Array.Fill(_distance, -1);
            Array.Fill(_cost, int.MaxValue);
            Array.Fill(_heapPositions, -1);
            _heapCount = 0;

            if (!_grid.IsInside(Goal) || _grid.IsBlocked(Goal))
            {
                return;
            }

            int head = 0;
            int tail = 0;
            int goalIndex = Index(Goal);
            _distance[goalIndex] = 0;
            _queue[tail++] = goalIndex;

            while (head < tail)
            {
                int currentIndex = _queue[head++];
                FlowCell current = Cell(currentIndex);
                int nextDistance = _distance[currentIndex] + 1;

                VisitNeighbors(current, (neighbor, _) =>
                {
                    int neighborIndex = Index(neighbor);

                    if (_distance[neighborIndex] >= 0)
                    {
                        return;
                    }

                    _distance[neighborIndex] = nextDistance;
                    _queue[tail++] = neighborIndex;
                });
            }

            int detourBudget = _options.MaxDetourSteps * _options.NormalStepCost;
            _cost[goalIndex] = 0;
            PushOrDecrease(goalIndex);

            while (_heapCount > 0)
            {
                int currentIndex = Pop();
                int currentCost = _cost[currentIndex];
                FlowCell current = Cell(currentIndex);

                VisitNeighbors(current, (neighbor, _) =>
                {
                    int neighborIndex = Index(neighbor);
                    int distance = _distance[neighborIndex];

                    if (distance < 0)
                    {
                        return;
                    }

                    int stepCost = _grid.CostPolicy.GetStepCost(neighbor);

                    if (stepCost <= 0)
                    {
                        stepCost = 1;
                    }

                    int candidateCost = currentCost + stepCost;
                    int detourLimit = distance * _options.NormalStepCost + detourBudget;

                    if (candidateCost > detourLimit || candidateCost >= _cost[neighborIndex])
                    {
                        return;
                    }

                    _cost[neighborIndex] = candidateCost;
                    PushOrDecrease(neighborIndex);
                });
            }
        }

        private void VisitNeighbors(FlowCell cell, Action<FlowCell, int> visit)
        {
            TryVisit(cell.X - 1, cell.Y);
            TryVisit(cell.X, cell.Y - 1);
            TryVisit(cell.X + 1, cell.Y);
            TryVisit(cell.X, cell.Y + 1);

            void TryVisit(int x, int y)
            {
                var neighbor = new FlowCell(x, y);

                if (!_grid.IsInside(neighbor) || _grid.IsBlocked(neighbor))
                {
                    return;
                }

                visit(neighbor, _cost[Index(neighbor)]);
            }
        }

        private void PushOrDecrease(int node)
        {
            int nodeCost = _cost[node];
            int position = _heapPositions[node];

            if (position < 0)
            {
                position = _heapCount++;
                _heapPositions[node] = position;
            }

            while (position > 0)
            {
                int parentPosition = (position - 1) / 2;
                int parentNode = _heapNodes[parentPosition];

                if (_heapCosts[parentPosition] <= nodeCost)
                {
                    break;
                }

                _heapNodes[position] = parentNode;
                _heapCosts[position] = _heapCosts[parentPosition];
                _heapPositions[parentNode] = position;
                position = parentPosition;
            }

            _heapNodes[position] = node;
            _heapCosts[position] = nodeCost;
            _heapPositions[node] = position;
        }

        private int Pop()
        {
            int node = _heapNodes[0];
            _heapPositions[node] = -1;
            _heapCount--;

            if (_heapCount == 0)
            {
                return node;
            }

            int lastNode = _heapNodes[_heapCount];
            int lastCost = _heapCosts[_heapCount];
            int position = 0;

            while (true)
            {
                int left = position * 2 + 1;

                if (left >= _heapCount)
                {
                    break;
                }

                int right = left + 1;
                int child = right < _heapCount && _heapCosts[right] < _heapCosts[left]
                    ? right
                    : left;

                if (_heapCosts[child] >= lastCost)
                {
                    break;
                }

                _heapNodes[position] = _heapNodes[child];
                _heapCosts[position] = _heapCosts[child];
                _heapPositions[_heapNodes[position]] = position;
                position = child;
            }

            _heapNodes[position] = lastNode;
            _heapCosts[position] = lastCost;
            _heapPositions[lastNode] = position;
            return node;
        }

        private int Index(FlowCell cell)
        {
            return cell.Y * _grid.Width + cell.X;
        }

        private FlowCell Cell(int index)
        {
            return new FlowCell(index % _grid.Width, index / _grid.Width);
        }
    }

    public sealed class FlowFieldCache
    {
        private readonly FlowGrid _grid;
        private readonly FlowFieldOptions? _options;
        private FlowField? _field;
        private FlowCell _goal;
        private int _terrainVersion = -1;

        public FlowFieldCache(FlowGrid grid, FlowFieldOptions? options = null)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _options = options;
        }

        public int BuildCount { get; private set; }

        public FlowField Get(FlowCell goal)
        {
            if (_field == null || _goal != goal || _terrainVersion != _grid.TerrainVersion)
            {
                _field = new FlowField(_grid, goal, _options);
                _goal = goal;
                _terrainVersion = _grid.TerrainVersion;
                BuildCount++;
            }

            return _field;
        }
    }
}
