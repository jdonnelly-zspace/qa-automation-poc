// =============================================================================
// TextToSpeechServiceTests.cs - Edit Mode Unit Tests for TextToSpeechService
// =============================================================================
// TARGET CLASS: TextToSpeechService
//   Real file: Assets/CommonA3/zSpace/Scripts/TextToSpeech/TextToSpeechService.cs
//
// WHAT IT TESTS:
//   Service that converts text labels into spoken audio for accessibility.
//   Tests validate volume clamping (0-1 range mapped to 0-100 native),
//   autoplay toggle with change events, playback handle management, and
//   graceful stop behavior.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real TextToSpeechService wraps a native TTS engine. The stub
//      here exercises property clamping, event dispatch, and playback
//      state tracking without native dependencies.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Represents an in-flight TTS playback session, allowing callers to
    /// stop a specific utterance rather than all audio.
    /// </summary>
    public class AudioPlaybackHandle
    {
        private static int _nextId = 1;

        public int Id { get; }
        public bool IsPlaying { get; set; }

        public AudioPlaybackHandle()
        {
            Id = _nextId++;
            IsPlaying = true;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Event args carrying old and new values for property-change events.
    /// </summary>
    public class ValueChangedEventArgs<T> : EventArgs
    {
        public T OldValue { get; }
        public T NewValue { get; }

        public ValueChangedEventArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the TTS property, event, and playback portions of the real
    /// TextToSpeechService without requiring the native TTS engine.
    /// </summary>
    public class TextToSpeechServiceStub
    {
        private float _volume = 0.5f;
        private bool _autoplayEnabled = true;
        private readonly List<AudioPlaybackHandle> _activeHandles =
            new List<AudioPlaybackHandle>();

        public event EventHandler<ValueChangedEventArgs<float>> VolumeChanged;
        public event EventHandler<ValueChangedEventArgs<bool>> AutoplayChanged;

        public bool IsTextToSpeechAutoplayEnabled
        {
            get { return _autoplayEnabled; }
            set
            {
                if (_autoplayEnabled != value)
                {
                    bool old = _autoplayEnabled;
                    _autoplayEnabled = value;
                    AutoplayChanged?.Invoke(this,
                        new ValueChangedEventArgs<bool>(old, value));
                }
            }
        }

        public float TextToSpeechAudioVolume
        {
            get { return _volume; }
            set
            {
                float clamped = Math.Max(0f, Math.Min(1f, value));
                if (Math.Abs(_volume - clamped) > float.Epsilon)
                {
                    float old = _volume;
                    _volume = clamped;
                    VolumeChanged?.Invoke(this,
                        new ValueChangedEventArgs<float>(old, clamped));
                }
            }
        }

        public AudioPlaybackHandle PlayTextToSpeechAudio(string text, bool isAutoplay = false)
        {
            if (isAutoplay && !_autoplayEnabled)
            {
                return null;
            }

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var handle = new AudioPlaybackHandle();
            _activeHandles.Add(handle);
            return handle;
        }

        public void StopTextToSpeechAudio()
        {
            foreach (var handle in _activeHandles)
            {
                handle.IsPlaying = false;
            }

            _activeHandles.Clear();
        }

        public void StopTextToSpeechAudio(AudioPlaybackHandle handle)
        {
            if (handle == null)
            {
                return;
            }

            handle.IsPlaying = false;
            _activeHandles.Remove(handle);
        }

        public bool IsPlaying()
        {
            return _activeHandles.Count > 0;
        }

        public AudioPlaybackHandle TryPlayTextToSpeechAudio(string text,
            bool isAutoplay = false)
        {
            try
            {
                return PlayTextToSpeechAudio(text, isAutoplay);
            }
            catch
            {
                return null;
            }
        }

        public bool TryStopTextToSpeechAudio()
        {
            if (_activeHandles.Count == 0)
            {
                return false;
            }

            StopTextToSpeechAudio();
            return true;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class TextToSpeechServiceTests
    {
        private TextToSpeechServiceStub _ttsService;

        [SetUp]
        public void SetUp()
        {
            _ttsService = new TextToSpeechServiceStub();
        }

        [TearDown]
        public void TearDown()
        {
            _ttsService = null;
        }

        [Test]
        public void VolumeSet_ClampsToZeroOneRange_WhenOutOfBounds()
        {
            // WHY: The native TTS engine expects 0-100; the 0-1 wrapper must
            // clamp to prevent out-of-range values that distort or mute audio.

            // Act
            _ttsService.TextToSpeechAudioVolume = 1.5f;
            float aboveMax = _ttsService.TextToSpeechAudioVolume;

            _ttsService.TextToSpeechAudioVolume = -0.3f;
            float belowMin = _ttsService.TextToSpeechAudioVolume;

            // Assert
            Assert.AreEqual(1.0f, aboveMax, 0.001f,
                "Volume should be clamped to 1.0 when set above maximum.");
            Assert.AreEqual(0.0f, belowMin, 0.001f,
                "Volume should be clamped to 0.0 when set below minimum.");
        }

        [Test]
        public void VolumeSet_FiresChangedEvent_WithOldAndNewValues()
        {
            // WHY: The volume slider UI subscribes to this event to update its
            // visual position; missing events leave the UI out of sync.

            // Arrange
            float capturedOld = -1f;
            float capturedNew = -1f;
            _ttsService.VolumeChanged += (sender, args) =>
            {
                capturedOld = args.OldValue;
                capturedNew = args.NewValue;
            };

            // Act
            _ttsService.TextToSpeechAudioVolume = 0.8f;

            // Assert
            Assert.AreEqual(0.5f, capturedOld, 0.001f,
                "OldValue should be the default volume (0.5).");
            Assert.AreEqual(0.8f, capturedNew, 0.001f,
                "NewValue should be the newly set volume.");
        }

        [Test]
        public void AutoplayToggle_FiresEvent_WhenValueChanges()
        {
            // WHY: The settings panel observes autoplay changes to update the
            // toggle switch; a silent change leaves the UI stale.

            // Arrange
            bool eventFired = false;
            bool capturedOld = false;
            bool capturedNew = false;
            _ttsService.AutoplayChanged += (sender, args) =>
            {
                eventFired = true;
                capturedOld = args.OldValue;
                capturedNew = args.NewValue;
            };

            // Act
            _ttsService.IsTextToSpeechAutoplayEnabled = false;

            // Assert
            Assert.IsTrue(eventFired,
                "AutoplayChanged event should fire when the value changes.");
            Assert.IsTrue(capturedOld,
                "OldValue should be true (the default).");
            Assert.IsFalse(capturedNew,
                "NewValue should be false after disabling autoplay.");
        }

        [Test]
        public void PlayAudio_ReturnsHandle_WhenAutoplayEnabled()
        {
            // WHY: Callers use the handle to stop a specific utterance later;
            // a null handle would prevent targeted stop.

            // Arrange
            _ttsService.IsTextToSpeechAutoplayEnabled = true;

            // Act
            AudioPlaybackHandle handle =
                _ttsService.PlayTextToSpeechAudio("The heart has four chambers.", true);

            // Assert
            Assert.IsNotNull(handle,
                "PlayTextToSpeechAudio should return a handle when autoplay is enabled.");
            Assert.IsTrue(handle.IsPlaying,
                "New handle should be in the IsPlaying state.");
        }

        [Test]
        public void PlayAudio_ReturnsNull_WhenAutoplayDisabledAndIsAutoplay()
        {
            // WHY: Students who disable autoplay should not hear unsolicited
            // narration when navigating between labels.

            // Arrange
            _ttsService.IsTextToSpeechAutoplayEnabled = false;

            // Act
            AudioPlaybackHandle handle =
                _ttsService.PlayTextToSpeechAudio("Left ventricle", true);

            // Assert
            Assert.IsNull(handle,
                "Autoplay requests should return null when autoplay is disabled.");
        }

        [Test]
        public void StopAll_StopsAllActiveHandles()
        {
            // WHY: The "stop all" button must silence every active utterance
            // immediately so the teacher can address the class.

            // Arrange
            var handle1 = _ttsService.PlayTextToSpeechAudio("Aorta");
            var handle2 = _ttsService.PlayTextToSpeechAudio("Pulmonary artery");

            // Act
            _ttsService.StopTextToSpeechAudio();

            // Assert
            Assert.IsFalse(handle1.IsPlaying,
                "First handle should be stopped after StopAll.");
            Assert.IsFalse(handle2.IsPlaying,
                "Second handle should be stopped after StopAll.");
            Assert.IsFalse(_ttsService.IsPlaying(),
                "IsPlaying should return false after stopping all audio.");
        }

        [Test]
        public void StopSpecific_StopsOnlyTargetHandle()
        {
            // WHY: When a student navigates away from one label while another is
            // still speaking, only the old label's audio should stop.

            // Arrange
            var handle1 = _ttsService.PlayTextToSpeechAudio("Superior vena cava");
            var handle2 = _ttsService.PlayTextToSpeechAudio("Inferior vena cava");

            // Act
            _ttsService.StopTextToSpeechAudio(handle1);

            // Assert
            Assert.IsFalse(handle1.IsPlaying,
                "Targeted handle should be stopped.");
            Assert.IsTrue(_ttsService.IsPlaying(),
                "Service should still report IsPlaying because handle2 is active.");
        }

        [Test]
        public void TryStop_ReturnsFalse_WhenNothingIsPlaying()
        {
            // WHY: UI code calls TryStop defensively; a false return lets it
            // skip unnecessary visual state changes.

            // Act
            bool result = _ttsService.TryStopTextToSpeechAudio();

            // Assert
            Assert.IsFalse(result,
                "TryStop should return false when there are no active playback handles.");
        }
    }
}
