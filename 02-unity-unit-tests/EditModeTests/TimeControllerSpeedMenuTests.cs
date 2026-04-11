// =============================================================================
// TimeControllerSpeedMenuTests.cs - Edit Mode Unit Tests for TimeControllerSpeedMenu
// =============================================================================
// TARGET CLASS: TimeControllerSpeedMenu
//   Real file: Assets/CommonA3/zSpace/licensing/Modernization/UI/Scripts/TimeControllerSpeedMenu.cs
//
// WHAT IT TESTS:
//   TimeControllerSpeedMenu extends Hideable and manages the speed selection
//   dropdown for the time controller. Tests verify speed selection get/set,
//   show/hide anchored position calculations, and the auto-hide interaction
//   with button toggle events.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real TimeControllerSpeedMenu inherits from Hideable (MonoBehaviour)
//      and uses RectTransform for positioning. These tests use POCO stubs
//      that simulate the show/hide position logic.
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
    /// Stub for Vector2 to avoid Unity dependency.
    /// </summary>
    public struct Vector2Stub
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2Stub(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector2Stub))
            {
                return false;
            }

            var other = (Vector2Stub)obj;
            return Math.Abs(X - other.X) < 0.001f && Math.Abs(Y - other.Y) < 0.001f;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Stub for ButtonToggleGroup used by the speed menu to track selected speed.
    /// </summary>
    public class SpeedMenuToggleGroupStub
    {
        private string _selectedId = null;
        private Dictionary<string, bool> _toggledStates = new Dictionary<string, bool>();

        public void SetSelectedId(string id)
        {
            // Untoggle previously selected
            if (_selectedId != null)
            {
                _toggledStates[_selectedId] = false;
            }

            _selectedId = id;
            _toggledStates[id] = true;
        }

        public string GetSelectedId()
        {
            return _selectedId;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API and show/hide position
    /// logic of TimeControllerSpeedMenu. Simulates the Hideable base class
    /// show/hide state and the anchored position calculations.
    /// </summary>
    public class TimeControllerSpeedMenuStubFull
    {
        private SpeedMenuToggleGroupStub _buttonToggleGroup = new SpeedMenuToggleGroupStub();
        private Vector2Stub _initialAnchoredPosition;
        private float _rectHeight;
        private bool _isVisible = false;
        private bool _isAutoHideEnabled = true;
        private int _resetAutoHideDelayCallCount = 0;

        public bool EnableAutoHide
        {
            get { return _isAutoHideEnabled; }
            set { _isAutoHideEnabled = value; }
        }

        public int ResetAutoHideDelayCallCount
        {
            get { return _resetAutoHideDelayCallCount; }
        }

        public bool IsVisible
        {
            get { return _isVisible; }
        }

        public int ShowStartedCallCount { get; private set; }
        public int HideStartedCallCount { get; private set; }

        public TimeControllerSpeedMenuStubFull(
            Vector2Stub initialAnchoredPosition, float rectHeight)
        {
            _initialAnchoredPosition = initialAnchoredPosition;
            _rectHeight = rectHeight;
        }

        public void SetSelectedSpeed(string id)
        {
            _buttonToggleGroup.SetSelectedId(id);
        }

        public string GetSelectedSpeed()
        {
            return _buttonToggleGroup.GetSelectedId();
        }

        public void Show()
        {
            _isVisible = true;
            ShowStartedCallCount++;
        }

        public void Hide()
        {
            _isVisible = false;
            HideStartedCallCount++;
        }

        public void ResetAutoHideDelay()
        {
            _resetAutoHideDelayCallCount++;
        }

        /// <summary>
        /// Mirrors the position calculation: show position moves the menu
        /// down by its own height from the initial anchored position.
        /// </summary>
        public Vector2Stub GetShowAnchoredPosition()
        {
            return new Vector2Stub(
                _initialAnchoredPosition.X,
                _initialAnchoredPosition.Y - _rectHeight);
        }

        /// <summary>
        /// Mirrors the position calculation: hide position returns to
        /// the initial anchored position (menu slides back up).
        /// </summary>
        public Vector2Stub GetHideAnchoredPosition()
        {
            return new Vector2Stub(
                _initialAnchoredPosition.X,
                _initialAnchoredPosition.Y);
        }

        /// <summary>
        /// Simulates the button toggled handler: hides the menu and
        /// forwards the event (speed was selected).
        /// </summary>
        public void HandleOnButtonToggled(string buttonId)
        {
            this.Hide();
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class TimeControllerSpeedMenuTests
    {
        private TimeControllerSpeedMenuStubFull _speedMenu;

        [SetUp]
        public void SetUp()
        {
            _speedMenu = new TimeControllerSpeedMenuStubFull(
                new Vector2Stub(100f, 200f), rectHeight: 50f);
        }

        [TearDown]
        public void TearDown()
        {
            _speedMenu = null;
        }

        // WHY: Speed selection is the core purpose of this menu. SetSelectedSpeed
        // and GetSelectedSpeed must round-trip correctly for speed changes to apply.
        [Test]
        public void SetGetSelectedSpeed_RoundTrips_Correctly()
        {
            // Act
            _speedMenu.SetSelectedSpeed("speed-0.5x");

            // Assert
            Assert.AreEqual("speed-0.5x", _speedMenu.GetSelectedSpeed(),
                "GetSelectedSpeed should return the speed set by SetSelectedSpeed.");
        }

        // WHY: Changing the selected speed must update the stored value, replacing
        // any previous selection.
        [Test]
        public void SetSelectedSpeed_OverwritesPreviousSelection()
        {
            // Arrange
            _speedMenu.SetSelectedSpeed("speed-1x");

            // Act
            _speedMenu.SetSelectedSpeed("speed-0.25x");

            // Assert
            Assert.AreEqual("speed-0.25x", _speedMenu.GetSelectedSpeed(),
                "GetSelectedSpeed should return the most recently set speed, not the previous one.");
        }

        // WHY: The show position drops the menu downward by its height, making it
        // visible below the speed button. Incorrect positioning hides the menu.
        [Test]
        public void GetShowAnchoredPosition_OffsetsDownByRectHeight()
        {
            // Act
            var showPos = _speedMenu.GetShowAnchoredPosition();

            // Assert
            Assert.AreEqual(100f, showPos.X, 0.001f,
                "Show position X should match the initial X position.");
            Assert.AreEqual(150f, showPos.Y, 0.001f,
                "Show position Y should be initialY minus rectHeight (200 - 50 = 150).");
        }

        // WHY: The hide position returns the menu to its original spot, effectively
        // sliding it back behind the toolbar.
        [Test]
        public void GetHideAnchoredPosition_ReturnsInitialPosition()
        {
            // Act
            var hidePos = _speedMenu.GetHideAnchoredPosition();

            // Assert
            Assert.AreEqual(100f, hidePos.X, 0.001f,
                "Hide position X should match the initial X position.");
            Assert.AreEqual(200f, hidePos.Y, 0.001f,
                "Hide position Y should match the initial Y position.");
        }

        // WHY: When a speed button is toggled, the menu must auto-hide so it
        // does not obstruct the playback view after the user makes a selection.
        [Test]
        public void HandleOnButtonToggled_HidesMenu()
        {
            // Arrange
            _speedMenu.Show();
            Assert.IsTrue(_speedMenu.IsVisible,
                "Menu should be visible after Show().");

            // Act
            _speedMenu.HandleOnButtonToggled("speed-1x");

            // Assert
            Assert.IsFalse(_speedMenu.IsVisible,
                "Menu should be hidden after a button is toggled (speed selected).");
        }

        // WHY: Show and Hide must track visibility state so external code
        // (like the TimeControllerBar) can query whether the menu is open.
        [Test]
        public void ShowHide_TracksVisibilityState()
        {
            // Initially hidden
            Assert.IsFalse(_speedMenu.IsVisible,
                "Menu should start hidden.");

            // Show
            _speedMenu.Show();
            Assert.IsTrue(_speedMenu.IsVisible,
                "Menu should be visible after Show().");
            Assert.AreEqual(1, _speedMenu.ShowStartedCallCount,
                "ShowStartedCallCount should be 1 after one Show() call.");

            // Hide
            _speedMenu.Hide();
            Assert.IsFalse(_speedMenu.IsVisible,
                "Menu should be hidden after Hide().");
            Assert.AreEqual(1, _speedMenu.HideStartedCallCount,
                "HideStartedCallCount should be 1 after one Hide() call.");
        }

        // WHY: Auto-hide is temporarily disabled when the pointer is over the
        // time controller bar to prevent the menu from disappearing while the
        // user is interacting with it.
        [Test]
        public void EnableAutoHide_CanBeToggledExternally()
        {
            // Default state
            Assert.IsTrue(_speedMenu.EnableAutoHide,
                "Auto-hide should be enabled by default.");

            // Act - disable (simulating pointer enter on the bar)
            _speedMenu.EnableAutoHide = false;

            // Assert
            Assert.IsFalse(_speedMenu.EnableAutoHide,
                "Auto-hide should be disabled after setting to false.");

            // Act - re-enable and reset delay (simulating pointer exit)
            _speedMenu.ResetAutoHideDelay();
            _speedMenu.EnableAutoHide = true;

            // Assert
            Assert.IsTrue(_speedMenu.EnableAutoHide,
                "Auto-hide should be re-enabled after setting to true.");
            Assert.AreEqual(1, _speedMenu.ResetAutoHideDelayCallCount,
                "ResetAutoHideDelay should have been called once.");
        }
    }
}
