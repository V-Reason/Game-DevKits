using Xunit;

namespace GameDevKit.AudioArbitration.Tests
{
    public sealed class AudioArbitrationTests
    {
        [Fact]
        public void VoiceExpiresAndFreesCapacity()
        {
            var arbiter = new AudioArbiter<Clip>(
                options: new AudioArbiterOptions { MaxVoices = 1 });
            var clip = new Clip("a");

            Assert.True(arbiter.TryPlay(new AudioCue<Clip>(clip, AudioChannel.Sfx, 10, 0.2), out _));
            Assert.False(arbiter.TryPlay(new AudioCue<Clip>(clip, AudioChannel.Sfx, 10, 0.2), out _));

            arbiter.Tick(0.21);
            Assert.True(arbiter.TryPlay(new AudioCue<Clip>(clip, AudioChannel.Sfx, 10, 0.2), out _));
        }

        [Fact]
        public void DuplicateClipIsRejectedWhileActive()
        {
            var arbiter = new AudioArbiter<Clip>();
            var clip = new Clip("a");

            Assert.True(arbiter.TryPlay(new AudioCue<Clip>(clip, AudioChannel.Sfx, 10, 1), out _));
            Assert.False(arbiter.TryPlay(new AudioCue<Clip>(clip, AudioChannel.Sfx, 10, 1), out _));

            Assert.Equal(1, arbiter.GetStats().Rejected);
        }

        [Fact]
        public void LongCueIsCappedAndFadesNearTheEnd()
        {
            var clip = new Clip("death");
            var options = new AudioArbiterOptions { MaxDurationSeconds = 1.5, FadeTailSeconds = 0.25 };
            var arbiter = new AudioArbiter<Clip>(options: options);

            Assert.True(arbiter.TryPlay(new AudioCue<Clip>(clip, AudioChannel.Sfx, 5, 6), out AudioVoiceHandle handle));
            Assert.Equal(1, arbiter.GetFadeMultiplier(handle), 6);

            arbiter.Tick(1.3);
            Assert.InRange(arbiter.GetFadeMultiplier(handle), 0.79, 0.81);
        }

        [Fact]
        public void StealPolicyReplacesLowestPriorityVoice()
        {
            var arbiter = new AudioArbiter<Clip>(
                new StealLowestPriorityPolicy<Clip>(),
                new AudioArbiterOptions { MaxVoices = 1 });
            var low = new Clip("low");
            var high = new Clip("high");

            Assert.True(arbiter.TryPlay(new AudioCue<Clip>(low, AudioChannel.Sfx, 5, 1), out AudioVoiceHandle old));
            Assert.True(arbiter.TryPlay(new AudioCue<Clip>(high, AudioChannel.Sfx, 20, 1), out _));
            Assert.False(arbiter.Stop(old));
            Assert.Equal(1, arbiter.GetStats().Stolen);
            Assert.Equal(1, arbiter.GetStats().Active);
        }

        [Fact]
        public void StopAllClearsVoices()
        {
            var arbiter = new AudioArbiter<Clip>();
            Assert.True(arbiter.TryPlay(new AudioCue<Clip>(new Clip("a"), AudioChannel.Sfx, 1, 1), out _));
            Assert.True(arbiter.TryPlay(new AudioCue<Clip>(new Clip("b"), AudioChannel.Ui, 1, 1), out _));

            arbiter.StopAll();

            Assert.Equal(0, arbiter.GetStats().Active);
        }

        [Fact]
        public void AudioFaderClampsAndCompletes()
        {
            var fader = new AudioFader();
            fader.Start(0, 1, 2);
            fader.Advance(1);
            Assert.Equal(0.5, fader.Value, 6);

            fader.Advance(2);
            Assert.Equal(1, fader.Value, 6);
            Assert.False(fader.IsRunning);
        }

        [Fact]
        public void UiBlipLimiterThrottlesAndBoundsPitch()
        {
            double next = 0;
            Assert.True(UiBlipLimiter.TryReserve(1, 0, ref next));
            Assert.Equal(1.055, next, 6);
            Assert.False(UiBlipLimiter.TryReserve(1.02, 0.055, ref next));
            Assert.InRange(UiBlipLimiter.Pitch(8), 0.98f, 1.02f);
        }

        private sealed class Clip
        {
            public Clip(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }
    }
}
