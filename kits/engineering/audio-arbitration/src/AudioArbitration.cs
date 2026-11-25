using System;
using System.Collections.Generic;

namespace GameDevKit.AudioArbitration
{
    public enum AudioChannel
    {
        Music,
        Sfx,
        Ui,
        Ambience,
    }

    public readonly struct AudioCue<TClip>
        where TClip : class
    {
        public AudioCue(
            TClip clip,
            AudioChannel channel,
            int priority,
            double duration,
            bool allowDuplicate = false)
        {
            Clip = clip;
            Channel = channel;
            Priority = priority;
            Duration = duration;
            AllowDuplicate = allowDuplicate;
        }

        public TClip Clip { get; }

        public AudioChannel Channel { get; }

        public int Priority { get; }

        public double Duration { get; }

        public bool AllowDuplicate { get; }
    }

    public readonly struct AudioVoiceInfo<TClip>
        where TClip : class
    {
        public AudioVoiceInfo(int id, TClip clip, AudioChannel channel, int priority)
        {
            Id = id;
            Clip = clip;
            Channel = channel;
            Priority = priority;
        }

        public int Id { get; }

        public TClip Clip { get; }

        public AudioChannel Channel { get; }

        public int Priority { get; }
    }

    public readonly struct AudioAdmission
    {
        public AudioAdmission(bool accepted, int replaceVoiceId = 0)
        {
            Accepted = accepted;
            ReplaceVoiceId = replaceVoiceId;
        }

        public bool Accepted { get; }

        public int ReplaceVoiceId { get; }
    }

    public interface IAudioAdmissionPolicy<TClip>
        where TClip : class
    {
        AudioAdmission Admit(
            in AudioCue<TClip> cue,
            IReadOnlyList<AudioVoiceInfo<TClip>> active);
    }

    public sealed class RejectWhenFullPolicy<TClip> : IAudioAdmissionPolicy<TClip>
        where TClip : class
    {
        public AudioAdmission Admit(
            in AudioCue<TClip> cue,
            IReadOnlyList<AudioVoiceInfo<TClip>> active)
        {
            return new AudioAdmission(false);
        }
    }

    public sealed class StealLowestPriorityPolicy<TClip> : IAudioAdmissionPolicy<TClip>
        where TClip : class
    {
        public AudioAdmission Admit(
            in AudioCue<TClip> cue,
            IReadOnlyList<AudioVoiceInfo<TClip>> active)
        {
            int lowestPriority = int.MaxValue;
            int lowestId = 0;

            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].Priority >= lowestPriority)
                {
                    continue;
                }

                lowestPriority = active[i].Priority;
                lowestId = active[i].Id;
            }

            return lowestId != 0 && lowestPriority < cue.Priority
                ? new AudioAdmission(true, lowestId)
                : new AudioAdmission(false);
        }
    }

    public readonly struct AudioVoiceHandle : IEquatable<AudioVoiceHandle>
    {
        internal AudioVoiceHandle(int id, object owner)
        {
            Id = id;
            Owner = owner;
        }

        public int Id { get; }

        internal object? Owner { get; }

        public bool IsValid => Id != 0;

        public static AudioVoiceHandle None => default;

        public bool Equals(AudioVoiceHandle other)
        {
            return Id == other.Id && ReferenceEquals(Owner, other.Owner);
        }

        public override bool Equals(object? obj)
        {
            return obj is AudioVoiceHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }

    public sealed class AudioArbiterOptions
    {
        public int MaxVoices { get; set; } = 16;

        public double MaxDurationSeconds { get; set; } = 1.5;

        public double FadeTailSeconds { get; set; } = 0.12;
    }

    public readonly struct AudioArbiterStats
    {
        public AudioArbiterStats(int active, int accepted, int rejected, int stolen)
        {
            Active = active;
            Accepted = accepted;
            Rejected = rejected;
            Stolen = stolen;
        }

        public int Active { get; }

        public int Accepted { get; }

        public int Rejected { get; }

        public int Stolen { get; }
    }

    public sealed class AudioArbiter<TClip>
        where TClip : class
    {
        private sealed class Voice
        {
            public int Id;
            public TClip Clip = null!;
            public AudioChannel Channel;
            public int Priority;
            public double Remaining;
        }

        private readonly object _owner = new();
        private readonly IAudioAdmissionPolicy<TClip> _policy;
        private readonly AudioArbiterOptions _options;
        private readonly List<Voice> _voices = new();
        private readonly List<AudioVoiceInfo<TClip>> _voiceInfos = new();
        private int _nextId = 1;
        private int _accepted;
        private int _rejected;
        private int _stolen;

        public AudioArbiter(
            IAudioAdmissionPolicy<TClip>? policy = null,
            AudioArbiterOptions? options = null)
        {
            _policy = policy ?? new RejectWhenFullPolicy<TClip>();
            _options = options ?? new AudioArbiterOptions();

            if (_options.MaxVoices <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MaxVoices must be positive.");
            }

            if (_options.MaxDurationSeconds <= 0
                || double.IsNaN(_options.MaxDurationSeconds)
                || double.IsInfinity(_options.MaxDurationSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    "MaxDurationSeconds must be finite and positive.");
            }

            if (_options.FadeTailSeconds < 0
                || double.IsNaN(_options.FadeTailSeconds)
                || double.IsInfinity(_options.FadeTailSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    "FadeTailSeconds must be finite and non-negative.");
            }
        }

        public bool TryPlay(in AudioCue<TClip> cue, out AudioVoiceHandle handle)
        {
            handle = AudioVoiceHandle.None;

            if (cue.Clip == null)
            {
                throw new ArgumentNullException(nameof(cue), "Audio cue clip is required.");
            }

            if (cue.Duration <= 0
                || double.IsNaN(cue.Duration)
                || double.IsInfinity(cue.Duration))
            {
                _rejected++;
                return false;
            }

            if (!cue.AllowDuplicate && HasActiveClip(cue.Clip))
            {
                _rejected++;
                return false;
            }

            if (_voices.Count >= _options.MaxVoices)
            {
                BuildVoiceInfos();
                AudioAdmission admission = _policy.Admit(in cue, _voiceInfos);

                if (!admission.Accepted)
                {
                    _rejected++;
                    return false;
                }

                if (!RemoveVoice(admission.ReplaceVoiceId))
                {
                    _rejected++;
                    return false;
                }

                _stolen++;
            }

            double duration = Math.Min(cue.Duration, _options.MaxDurationSeconds);
            var voice = new Voice
            {
                Id = NextId(),
                Clip = cue.Clip,
                Channel = cue.Channel,
                Priority = cue.Priority,
                Remaining = duration,
            };

            _voices.Add(voice);
            _accepted++;
            handle = new AudioVoiceHandle(voice.Id, _owner);
            return true;
        }

        public void Tick(double deltaTime)
        {
            if (deltaTime < 0 || double.IsNaN(deltaTime) || double.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            for (int i = _voices.Count - 1; i >= 0; i--)
            {
                _voices[i].Remaining -= deltaTime;

                if (_voices[i].Remaining <= 0)
                {
                    _voices.RemoveAt(i);
                }
            }
        }

        public bool Stop(AudioVoiceHandle handle)
        {
            if (!handle.IsValid || !ReferenceEquals(handle.Owner, _owner))
            {
                return false;
            }

            return RemoveVoice(handle.Id);
        }

        public void StopAll()
        {
            _voices.Clear();
        }

        public double GetFadeMultiplier(AudioVoiceHandle handle)
        {
            if (!handle.IsValid || !ReferenceEquals(handle.Owner, _owner))
            {
                return 0;
            }

            for (int i = 0; i < _voices.Count; i++)
            {
                Voice voice = _voices[i];

                if (voice.Id != handle.Id)
                {
                    continue;
                }

                if (voice.Remaining <= 0)
                {
                    return 0;
                }

                if (_options.FadeTailSeconds <= 0 || voice.Remaining >= _options.FadeTailSeconds)
                {
                    return 1;
                }

                return voice.Remaining / _options.FadeTailSeconds;
            }

            return 0;
        }

        public AudioArbiterStats GetStats()
        {
            return new AudioArbiterStats(_voices.Count, _accepted, _rejected, _stolen);
        }

        private bool HasActiveClip(TClip clip)
        {
            for (int i = 0; i < _voices.Count; i++)
            {
                if (ReferenceEquals(_voices[i].Clip, clip))
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildVoiceInfos()
        {
            _voiceInfos.Clear();

            for (int i = 0; i < _voices.Count; i++)
            {
                Voice voice = _voices[i];
                _voiceInfos.Add(new AudioVoiceInfo<TClip>(
                    voice.Id,
                    voice.Clip,
                    voice.Channel,
                    voice.Priority));
            }
        }

        private bool RemoveVoice(int id)
        {
            for (int i = 0; i < _voices.Count; i++)
            {
                if (_voices[i].Id != id)
                {
                    continue;
                }

                _voices.RemoveAt(i);
                return true;
            }

            return false;
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

    public sealed class AudioFader
    {
        private double From { get; set; }
        private double To { get; set; }
        private double Duration { get; set; }
        private double Elapsed { get; set; }

        public bool IsRunning { get; private set; }

        public double Value { get; private set; }

        public void Start(double from, double to, double duration)
        {
            if (double.IsNaN(from) || double.IsInfinity(from)
                || double.IsNaN(to) || double.IsInfinity(to)
                || duration < 0 || double.IsNaN(duration) || double.IsInfinity(duration))
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            From = from;
            To = to;
            Duration = duration;
            Elapsed = 0;
            Value = from;
            IsRunning = duration > 0;

            if (!IsRunning)
            {
                Value = to;
            }
        }

        public void Advance(double deltaTime)
        {
            if (deltaTime < 0 || double.IsNaN(deltaTime) || double.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (!IsRunning)
            {
                return;
            }

            Elapsed += deltaTime;
            double progress = Math.Min(1, Elapsed / Duration);
            Value = From + (To - From) * progress;

            if (progress >= 1)
            {
                Value = To;
                IsRunning = false;
            }
        }
    }

    public static class UiBlipLimiter
    {
        public const double DefaultCooldown = 0.055;

        public static bool TryReserve(
            double now,
            double cooldown,
            ref double nextAvailable)
        {
            if (double.IsNaN(now) || double.IsInfinity(now))
            {
                throw new ArgumentOutOfRangeException(nameof(now));
            }

            if (now < nextAvailable)
            {
                return false;
            }

            double safeCooldown = double.IsNaN(cooldown) || cooldown <= 0
                ? DefaultCooldown
                : cooldown;

            nextAvailable = now + safeCooldown;
            return true;
        }

        public static float Pitch(int sequence)
        {
            int step = sequence < 0 ? -sequence : sequence;
            return 0.98f + step % 3 * 0.02f;
        }
    }
}
