using System;
using System.Collections.Generic;

namespace GameDevKit.ReactionRules
{
    public readonly struct RuleKey<TLeft, TRight> : IEquatable<RuleKey<TLeft, TRight>>
    {
        public RuleKey(TLeft left, TRight right)
        {
            Left = left;
            Right = right;
        }

        public TLeft Left { get; }

        public TRight Right { get; }

        public bool Equals(RuleKey<TLeft, TRight> other)
        {
            return EqualityComparer<TLeft>.Default.Equals(Left, other.Left)
                && EqualityComparer<TRight>.Default.Equals(Right, other.Right);
        }

        public override bool Equals(object? obj)
        {
            return obj is RuleKey<TLeft, TRight> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int left = Left == null ? 0 : EqualityComparer<TLeft>.Default.GetHashCode(Left);
                int right = Right == null ? 0 : EqualityComparer<TRight>.Default.GetHashCode(Right);
                return (left * 397) ^ right;
            }
        }
    }

    public readonly struct RuleEntry<TLeft, TRight, TResult>
    {
        public RuleEntry(TLeft left, TRight right, TResult result)
        {
            Left = left;
            Right = right;
            Result = result;
        }

        public TLeft Left { get; }

        public TRight Right { get; }

        public TResult Result { get; }
    }

    public sealed class RuleCatalog<TLeft, TRight, TResult>
    {
        private readonly Dictionary<RuleKey<TLeft, TRight>, TResult> _rules;
        private readonly Func<TLeft, TRight, TResult>? _fallback;

        public RuleCatalog(
            IEnumerable<RuleEntry<TLeft, TRight, TResult>> rules,
            Func<TLeft, TRight, TResult>? fallback = null)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            _rules = new Dictionary<RuleKey<TLeft, TRight>, TResult>();
            _fallback = fallback;

            foreach (RuleEntry<TLeft, TRight, TResult> entry in rules)
            {
                var key = new RuleKey<TLeft, TRight>(entry.Left, entry.Right);

                if (_rules.ContainsKey(key))
                {
                    throw new ArgumentException(
                        $"Duplicate rule for ({entry.Left}, {entry.Right}).",
                        nameof(rules));
                }

                _rules.Add(key, entry.Result);
            }
        }

        public int Count => _rules.Count;

        public bool TryGet(TLeft left, TRight right, out TResult result)
        {
            return _rules.TryGetValue(new RuleKey<TLeft, TRight>(left, right), out result!);
        }

        public TResult Resolve(TLeft left, TRight right)
        {
            if (_rules.TryGetValue(new RuleKey<TLeft, TRight>(left, right), out TResult? result))
            {
                return result;
            }

            if (_fallback != null)
            {
                return _fallback(left, right);
            }

            throw new KeyNotFoundException($"No rule for ({left}, {right}).");
        }
    }
}
