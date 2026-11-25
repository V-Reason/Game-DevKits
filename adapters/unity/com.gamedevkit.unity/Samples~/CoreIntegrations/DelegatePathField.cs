using System;
using System.Numerics;
using GameDevKit.SteeringPathFollowing;

public sealed class DelegatePathField : IPathField
{
    private readonly Func<Vector2, int> _findStart;
    private readonly Func<int, int> _next;
    private readonly Func<int, int> _alternateNext;
    private readonly Func<int, Vector2> _position;
    private readonly Func<Vector2, bool> _walkable;
    private readonly Func<int> _version;

    public DelegatePathField(
        Func<int> version,
        float sampleSpacing,
        Func<Vector2, int> findStart,
        Func<int, int> next,
        Func<int, int> alternateNext,
        Func<int, Vector2> position,
        Func<Vector2, bool> walkable)
    {
        _version = version;
        SampleSpacing = sampleSpacing;
        _findStart = findStart;
        _next = next;
        _alternateNext = alternateNext;
        _position = position;
        _walkable = walkable;
    }

    public int Version => _version();

    public float SampleSpacing { get; }

    public bool TryFindStart(Vector2 position, out int node)
    {
        node = _findStart(position);
        return node >= 0;
    }

    public bool TryGetNext(int node, out int next)
    {
        next = _next(node);
        return next >= 0;
    }

    public bool TryGetAlternateNext(int node, out int next)
    {
        next = _alternateNext(node);
        return next >= 0;
    }

    public Vector2 SamplePosition(int node)
    {
        return _position(node);
    }

    public bool IsPointWalkable(Vector2 point)
    {
        return _walkable(point);
    }
}
