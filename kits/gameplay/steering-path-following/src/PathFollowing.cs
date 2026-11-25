using System;
using System.Collections.Generic;
using System.Numerics;

namespace GameDevKit.SteeringPathFollowing
{
    public interface IPathField
    {
        int Version { get; }

        float SampleSpacing { get; }

        bool TryFindStart(Vector2 position, out int node);

        bool TryGetNext(int node, out int next);

        bool TryGetAlternateNext(int node, out int next);

        Vector2 SamplePosition(int node);

        bool IsPointWalkable(Vector2 point);
    }

    public sealed class PathFollowingOptions
    {
        public float LookAheadDistance { get; set; } = 0.75f;

        public float HoldRadius { get; set; } = 0.15f;

        public float CornerScanDistance { get; set; } = 2.5f;

        public float MinWaypointDistance { get; set; } = 0.1f;

        public float TurnCosine { get; set; } = 0.9f;

        public int MaxScanNodes { get; set; } = 16;
    }

    public readonly struct PathFollowingResult
    {
        public PathFollowingResult(Vector2 direction, bool cornerAhead, float cornerDistance)
        {
            Direction = direction;
            CornerAhead = cornerAhead;
            CornerDistance = cornerDistance;
        }

        public Vector2 Direction { get; }

        public bool CornerAhead { get; }

        public float CornerDistance { get; }
    }

    public sealed class PathFollower
    {
        private readonly PathFollowingOptions _options;
        private Vector2 _waypoint;
        private int _version = -1;
        private bool _hasWaypoint;

        public PathFollower(PathFollowingOptions? options = null)
        {
            _options = options ?? new PathFollowingOptions();

            if (_options.LookAheadDistance <= 0
                || _options.HoldRadius < 0
                || _options.CornerScanDistance < 0
                || _options.MinWaypointDistance < 0
                || _options.MaxScanNodes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options));
            }
        }

        public bool TryResolve(
            IPathField field,
            Vector2 position,
            Vector2 velocity,
            bool jamming,
            out PathFollowingResult result)
        {
            result = default;

            if (field == null || !field.TryFindStart(position, out int node))
            {
                return false;
            }

            int next;
            bool found = jamming
                ? field.TryGetAlternateNext(node, out next) || field.TryGetNext(node, out next)
                : field.TryGetNext(node, out next);

            if (!found || next == node)
            {
                return false;
            }

            if (_version != field.Version)
            {
                _version = field.Version;
                _hasWaypoint = false;
            }

            Vector2 firstCenter = field.SamplePosition(next);

            if (field.Version < 0)
            {
                return false;
            }

            Vector2 leg = firstCenter - field.SamplePosition(node);

            if (leg.LengthSquared() <= 0)
            {
                return false;
            }

            Vector2 direction = Vector2.Normalize(leg);
            Vector2 waypoint = firstCenter;
            float travelled = Vector2.Distance(position, firstCenter);
            int cursor = next;
            bool cornerAhead = false;
            float cornerDistance = 0;

            for (int i = 0; i < _options.MaxScanNodes && !jamming; i++)
            {
                if (!field.TryGetNext(cursor, out int after))
                {
                    break;
                }

                Vector2 from = field.SamplePosition(cursor);
                Vector2 to = field.SamplePosition(after);
                Vector2 step = to - from;

                if (step.LengthSquared() <= 0)
                {
                    break;
                }

                Vector2 stepDirection = Vector2.Normalize(step);
                float alignment = Vector2.Dot(stepDirection, direction);

                if (alignment < _options.TurnCosine)
                {
                    if (travelled <= _options.CornerScanDistance)
                    {
                        cornerAhead = true;
                        cornerDistance = Vector2.Distance(position, from);
                    }

                    break;
                }

                float segment = step.Length();

                if (travelled + segment < _options.LookAheadDistance)
                {
                    waypoint = to;
                    travelled += segment;
                    cursor = after;
                    continue;
                }

                float remaining = _options.LookAheadDistance - travelled;

                if (remaining > 0)
                {
                    waypoint = from + stepDirection * remaining;
                }

                break;
            }

            if (_hasWaypoint
                && Vector2.Distance(waypoint, _waypoint) <= _options.HoldRadius
                && Vector2.Dot(_waypoint - position, velocity) >= 0)
            {
                waypoint = _waypoint;
            }
            else
            {
                _waypoint = waypoint;
                _hasWaypoint = true;
            }

            Vector2 target = IsLineWalkable(field, position, waypoint) ? waypoint : firstCenter;
            Vector2 finalDirection = target - position;
            float minDistance = _options.MinWaypointDistance;

            if (finalDirection.LengthSquared() <= minDistance * minDistance)
            {
                return false;
            }

            result = new PathFollowingResult(
                Vector2.Normalize(finalDirection),
                cornerAhead && !jamming,
                cornerDistance);
            return true;
        }

        private static bool IsLineWalkable(IPathField field, Vector2 from, Vector2 to)
        {
            float distance = Vector2.Distance(from, to);

            if (distance <= 0)
            {
                return true;
            }

            float spacing = field.SampleSpacing <= 0 ? 0.05f : field.SampleSpacing;
            int steps = Math.Max(1, (int)MathF.Ceiling(distance / spacing));

            for (int i = 1; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, i / (float)steps);

                if (!field.IsPointWalkable(point))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class SteeringGate
    {
        private readonly float _maxTurnRate;
        private readonly float _smoothingSeconds;
        private Vector2 _command;

        public SteeringGate(float maxTurnDegreesPerSecond, float smoothingSeconds = 0)
        {
            _maxTurnRate = Math.Max(0, maxTurnDegreesPerSecond);
            _smoothingSeconds = Math.Max(0, smoothingSeconds);
        }

        public Vector2 Step(Vector2 direction, Vector2 velocity, float deltaTime)
        {
            if (direction.LengthSquared() <= 0)
            {
                return _command;
            }

            direction = Vector2.Normalize(direction);
            Vector2 halfPlane = IntoHalfPlane(direction, velocity);

            if (_command.LengthSquared() <= 0 || deltaTime <= 0)
            {
                _command = halfPlane;
                return _command;
            }

            float dot = Math.Clamp(Vector2.Dot(_command, halfPlane), -1, 1);
            float cross = _command.X * halfPlane.Y - _command.Y * halfPlane.X;
            float angle = MathF.Atan2(cross, dot) * 180f / MathF.PI;

            if (_maxTurnRate > 0)
            {
                float maxTurn = _maxTurnRate * deltaTime;
                angle = Math.Clamp(angle, -maxTurn, maxTurn);
            }

            if (_smoothingSeconds > 0)
            {
                angle *= 1 - MathF.Exp(-deltaTime / _smoothingSeconds);
            }

            float radians = angle * MathF.PI / 180f;
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);
            Vector2 rotated = new(
                _command.X * cos - _command.Y * sin,
                _command.X * sin + _command.Y * cos);

            _command = IntoHalfPlane(rotated, velocity);
            return _command;
        }

        public void Reset(Vector2 direction)
        {
            _command = direction.LengthSquared() > 0 ? Vector2.Normalize(direction) : Vector2.Zero;
        }

        private static Vector2 IntoHalfPlane(Vector2 direction, Vector2 velocity)
        {
            if (velocity.LengthSquared() <= 0)
            {
                return direction;
            }

            Vector2 forward = Vector2.Normalize(velocity);

            if (Vector2.Dot(direction, forward) >= 0)
            {
                return direction;
            }

            Vector2 perpendicular = new(-forward.Y, forward.X);
            float side = Vector2.Dot(direction, perpendicular) < 0 ? -1 : 1;
            return Vector2.Normalize(perpendicular * side + forward * 0.06f);
        }
    }

    public readonly struct SteeringBody
    {
        public SteeringBody(Vector2 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        public Vector2 Position { get; }

        public float Radius { get; }
    }

    public static class SeparationResolver
    {
        public static Vector2 Resolve(
            Vector2 self,
            float radius,
            IReadOnlyList<SteeringBody> bodies,
            float margin,
            Func<Vector2, bool> canOccupy)
        {
            if (bodies == null)
            {
                throw new ArgumentNullException(nameof(bodies));
            }

            if (canOccupy == null)
            {
                throw new ArgumentNullException(nameof(canOccupy));
            }

            Vector2 push = Vector2.Zero;

            for (int i = 0; i < bodies.Count; i++)
            {
                Vector2 delta = self - bodies[i].Position;
                float sqr = delta.LengthSquared();
                float reach = radius + bodies[i].Radius + margin;

                if (sqr <= 0 || sqr >= reach * reach)
                {
                    continue;
                }

                float distance = MathF.Sqrt(sqr);
                push += delta / distance * (1 - distance / reach);
            }

            if (push.LengthSquared() <= 0)
            {
                return Vector2.Zero;
            }

            Vector2 probe = self + Vector2.Normalize(push) * 0.5f;

            if (!canOccupy(probe))
            {
                return Vector2.Zero;
            }

            return push.Length() > 1 ? Vector2.Normalize(push) : push;
        }
    }
}
