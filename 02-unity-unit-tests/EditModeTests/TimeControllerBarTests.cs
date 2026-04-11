// =============================================================================
// TimeControllerBarTests.cs - Edit Mode Unit Tests for TimeControllerBar
// =============================================================================
// TARGET CLASS: TimeControllerBar
//   Real file: Assets/CommonA3/zSpace/licensing/Modernization/UI/Scripts/TimeControllerBar.cs
//
// WHAT IT TESTS:
//   TimeControllerBar manages the playback control bar with play/pause,
//   playback direction, speed menu, restart, time scrubber, and save movie
//   buttons. Tests verify the public constants, button interactable state,
//   button toggle state, speed get/set, and time scrubber value management.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real TimeControllerBar implements IPointerEnterHandler and
//      IPointerExitHandler for auto-hide control. These tests exercise
//      the public API through lightweight POCO stubs.
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
    /// Stub for ButtonToggleGroup that tracks toggled state by button ID.
    /// </summary>
    public class ButtonToggleGroupStub
    {
        private Dictionary<string, bool> _toggledStates = new Dictionary<string, bool>();

        public void SetButtonToggled(string id, bool isToggled)
        {
            _toggledStates[id] = isToggled;
        }

        public bool IsButtonToggled(string id)
        {
            bool isToggled;
            if (_toggledStates.TryGetValue(id, out isToggled))
            {
                return isToggled;
            }

            return false;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Stub for TimeControllerSpeedMenu that tracks selected speed.
    /// </summary>
    public class TimeControllerSpeedMenuStub
    {
        private string _selectedSpeed = "speed-1x";

        public void SetSelectedSpeed(string id)
        {
            _selectedSpeed = id;
        }

        public string GetSelectedSpeed()
        {
            return _selectedSpeed;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Stub for TimeScrubber that tracks min, max, value, and playback state.
    /// </summary>
    public class TimeScrubberStub
    {
        private float _minValue = 0f;
        private float _maxValue = 1f;
        private float _value = 0f;
        private bool _isPlaying = false;

        public int StopPlaybackCallCount { get; private set; }

        public void SetMinValue(float minValue) { _minValue = minValue; }
        public float GetMinValue() { return _minValue; }
        public void SetMaxValue(float maxValue) { _maxValue = maxValue; }
        public float GetMaxValue() { return _maxValue; }
        public void SetValue(float value) { _value = value; }
        public float GetValue() { return _value; }
        public void StartPlayback() { _isPlaying = true; }

        public void StopPlayback()
        {
            _isPlaying = false;
            StopPlaybackCallCount++;
        }

        public bool IsPlaying() { return _isPlaying; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API of TimeControllerBar.
    /// Manages button interactable/toggle state, speed, and time scrubber.
    /// </summary>
    public class TimeControllerBarStub
    {
        public const string ExitButtonId = "exit";
        public const string PlayButtonId = "play";
        public const string PlaybackDirectionButtonId = "playback-direction";
        public const string SpeedMenuButtonId = "speed-menu";
        public const string RestartButtonId = "restart";
        public const string SaveMovieButtonId = "save-movie";
        public const string SpeedButtonFullId = "speed-1x";
        public const string SpeedButtonHalfId = "speed-0.5x";
        public const string SpeedButtonQuarterId = "speed-0.25x";
        public const string SpeedButtonEigthId = "speed-0.125x";

        private ButtonToggleGroupStub _buttonToggleGroup = new ButtonToggleGroupStub();
        private TimeControllerSpeedMenuStub _speedMenu = new TimeControllerSpeedMenuStub();
        private TimeScrubberStub _timeScrubber = new TimeScrubberStub();
        private Dictionary<string, bool> _buttonInteractableStates = new Dictionary<string, bool>();

        public TimeScrubberStub TimeScrubber { get { return _timeScrubber; } }

        public TimeControllerBarStub()
        {
            _buttonInteractableStates[PlayButtonId] = true;
            _buttonInteractableStates[RestartButtonId] = true;
            _buttonInteractableStates[SpeedMenuButtonId] = true;
            _buttonInteractableStates[ExitButtonId] = true;
            _buttonInteractableStates[SaveMovieButtonId] = true;
        }

        public void SetButtonInteractable(string id, bool isInteractable)
        {
            if (_buttonInteractableStates.ContainsKey(id))
            {
                _buttonInteractableStates[id] = isInteractable;
            }
        }

        public bool IsButtonInteractable(string id)
        {
            bool isInteractable;
            if (_buttonInteractableStates.TryGetValue(id, out isInteractable))
            {
                return isInteractable;
            }

            return false;
        }

        public void SetButtonToggled(string id, bool isToggled)
        {
            _buttonToggleGroup.SetButtonToggled(id, isToggled);
        }

        public bool IsButtonToggled(string id)
        {
            return _buttonToggleGroup.IsButtonToggled(id);
        }

        public void SetSpeed(string id)
        {
            _speedMenu.SetSelectedSpeed(id);
        }

        public string GetSpeed()
        {
            return _speedMenu.GetSelectedSpeed();
        }

        public void SetTimeScrubberMinValue(float minValue, bool stopPlayback = true)
        {
            if (stopPlayback)
            {
                _timeScrubber.StopPlayback();
            }

            _timeScrubber.SetMinValue(minValue);
        }

        public void SetTimeScrubberMaxValue(float maxValue, bool stopPlayback = true)
        {
            if (stopPlayback)
            {
                _timeScrubber.StopPlayback();
            }

            _timeScrubber.SetMaxValue(maxValue);
        }

        public void SetTimeScrubberValue(float value, bool stopPlayback = true)
        {
            if (stopPlayback)
            {
                _timeScrubber.StopPlayback();
            }

            _timeScrubber.SetValue(value);
        }

        public float GetTimeScrubberValue()
        {
            return _timeScrubber.GetValue();
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class TimeControllerBarTests
    {
        private TimeControllerBarStub _bar;

        [SetUp]
        public void SetUp()
        {
            _bar = new TimeControllerBarStub();
        }

        [TearDown]
        public void TearDown()
        {
            _bar = null;
        }

        // WHY: Button ID constants are used by external systems to reference
        // specific playback controls. Changing them breaks integration.
        [Test]
        public void ButtonIdConstants_HaveExpectedValues()
        {
            Assert.AreEqual("exit", TimeControllerBarStub.ExitButtonId,
                "ExitButtonId must match expected string.");
            Assert.AreEqual("play", TimeControllerBarStub.PlayButtonId,
                "PlayButtonId must match expected string.");
            Assert.AreEqual("playback-direction", TimeControllerBarStub.PlaybackDirectionButtonId,
                "PlaybackDirectionButtonId must match expected string.");
            Assert.AreEqual("speed-menu", TimeControllerBarStub.SpeedMenuButtonId,
                "SpeedMenuButtonId must match expected string.");
            Assert.AreEqual("restart", TimeControllerBarStub.RestartButtonId,
                "RestartButtonId must match expected string.");
            Assert.AreEqual("save-movie", TimeControllerBarStub.SaveMovieButtonId,
                "SaveMovieButtonId must match expected string.");
        }

        // WHY: Speed constants define playback speed tiers. They must be consistent
        // with the SetPlaybackSpeed switch cases in the real implementation.
        [Test]
        public void SpeedButtonIdConstants_HaveExpectedValues()
        {
            Assert.AreEqual("speed-1x", TimeControllerBarStub.SpeedButtonFullId,
                "SpeedButtonFullId must match expected string for 1x speed.");
            Assert.AreEqual("speed-0.5x", TimeControllerBarStub.SpeedButtonHalfId,
                "SpeedButtonHalfId must match expected string for 0.5x speed.");
            Assert.AreEqual("speed-0.25x", TimeControllerBarStub.SpeedButtonQuarterId,
                "SpeedButtonQuarterId must match expected string for 0.25x speed.");
            Assert.AreEqual("speed-0.125x", TimeControllerBarStub.SpeedButtonEigthId,
                "SpeedButtonEigthId must match expected string for 0.125x speed.");
        }

        // WHY: Disabling individual buttons prevents user interaction during
        // invalid states (e.g., save movie disabled when not in playback).
        [Test]
        public void SetButtonInteractable_DisablesSpecificButton()
        {
            // Act
            _bar.SetButtonInteractable(TimeControllerBarStub.PlayButtonId, false);

            // Assert
            Assert.IsFalse(_bar.IsButtonInteractable(TimeControllerBarStub.PlayButtonId),
                "Play button should not be interactable after disabling it.");
            Assert.IsTrue(_bar.IsButtonInteractable(TimeControllerBarStub.RestartButtonId),
                "Restart button should remain interactable when only play was disabled.");
        }

        // WHY: Toggle state drives play/pause and playback direction. Setting and
        // reading toggle state must be consistent.
        [Test]
        public void SetButtonToggled_UpdatesToggleState()
        {
            // Act
            _bar.SetButtonToggled(TimeControllerBarStub.PlayButtonId, true);

            // Assert
            Assert.IsTrue(_bar.IsButtonToggled(TimeControllerBarStub.PlayButtonId),
                "Play button should be toggled after SetButtonToggled(true).");

            // Act
            _bar.SetButtonToggled(TimeControllerBarStub.PlayButtonId, false);

            // Assert
            Assert.IsFalse(_bar.IsButtonToggled(TimeControllerBarStub.PlayButtonId),
                "Play button should not be toggled after SetButtonToggled(false).");
        }

        // WHY: Speed selection controls the playback rate of animations.
        // GetSpeed must return whatever SetSpeed configured.
        [Test]
        public void SetSpeed_GetSpeed_RoundTrips_Correctly()
        {
            // Act
            _bar.SetSpeed(TimeControllerBarStub.SpeedButtonHalfId);

            // Assert
            Assert.AreEqual(TimeControllerBarStub.SpeedButtonHalfId, _bar.GetSpeed(),
                "GetSpeed should return the speed ID set by SetSpeed.");
        }

        // WHY: Setting the time scrubber value by default stops playback to prevent
        // conflicts between manual scrubbing and automated playback.
        [Test]
        public void SetTimeScrubberValue_StopsPlayback_ByDefault()
        {
            // Arrange
            int initialStopCount = _bar.TimeScrubber.StopPlaybackCallCount;

            // Act
            _bar.SetTimeScrubberValue(0.5f);

            // Assert
            Assert.AreEqual(0.5f, _bar.GetTimeScrubberValue(), 0.001f,
                "Time scrubber value should be updated to 0.5.");
            Assert.AreEqual(initialStopCount + 1, _bar.TimeScrubber.StopPlaybackCallCount,
                "StopPlayback should be called when stopPlayback parameter defaults to true.");
        }

        // WHY: Some callers need to update the scrubber value during active playback
        // (e.g., syncing to external time). The stopPlayback=false flag supports this.
        [Test]
        public void SetTimeScrubberValue_DoesNotStopPlayback_WhenFlagIsFalse()
        {
            // Arrange
            int initialStopCount = _bar.TimeScrubber.StopPlaybackCallCount;

            // Act
            _bar.SetTimeScrubberValue(0.75f, stopPlayback: false);

            // Assert
            Assert.AreEqual(0.75f, _bar.GetTimeScrubberValue(), 0.001f,
                "Time scrubber value should be updated to 0.75.");
            Assert.AreEqual(initialStopCount, _bar.TimeScrubber.StopPlaybackCallCount,
                "StopPlayback should not be called when stopPlayback is false.");
        }

        // WHY: Min/max values define the animation timeline range. Setting them
        // must also stop playback by default to reset to a known state.
        [Test]
        public void SetTimeScrubberMinMax_StopsPlayback_ByDefault()
        {
            // Arrange
            int initialStopCount = _bar.TimeScrubber.StopPlaybackCallCount;

            // Act
            _bar.SetTimeScrubberMinValue(0.0f);
            _bar.SetTimeScrubberMaxValue(10.0f);

            // Assert
            Assert.AreEqual(initialStopCount + 2, _bar.TimeScrubber.StopPlaybackCallCount,
                "StopPlayback should be called once each for SetTimeScrubberMinValue and SetTimeScrubberMaxValue.");
        }
    }
}
