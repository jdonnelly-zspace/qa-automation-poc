// =============================================================================
// ToastNotificationControllerTests.cs - Edit Mode Unit Tests for ToastNotificationController
// =============================================================================
// TARGET CLASS: ToastNotificationController
//   Real file: Assets/VivedUpgrades/ToastNotification/Scripts/ToastNotificationController.cs
//
// WHAT IT TESTS:
//   Toast notification UI controller that slides in a message, holds for a
//   configurable duration, then fades out. Validates message assignment,
//   active-state tracking, show/hide lifecycle, and repeated-show behavior
//   (interrupting an already-visible toast with a new message).
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real ToastNotificationController is a ZSingleton<T> MonoBehaviour
//      that uses LeanTween and coroutines. These tests exercise core state
//      logic through a lightweight POCO stub so they compile standalone
//      without a Unity runtime.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API and core state logic of
    /// the real ToastNotificationController singleton, without requiring
    /// MonoBehaviour, LeanTween, or coroutines.
    /// </summary>
    public class ToastNotificationControllerStub
    {
        private string _labelText = "";
        private float _canvasGroupAlpha = 0f;
        private bool _isActive = false;
        private float _targetYPosition = 24f;
        private float _fadeDuration = 0.3f;
        private bool _hideScheduled = false;
        private float _lastHideDuration = 0f;

        public string LabelText { get { return _labelText; } }
        public float CanvasGroupAlpha { get { return _canvasGroupAlpha; } }
        public bool IsActive { get { return _isActive; } }
        public float TargetYPosition { get { return _targetYPosition; } }
        public float FadeDuration { get { return _fadeDuration; } }
        public bool IsHideScheduled { get { return _hideScheduled; } }
        public float LastHideDuration { get { return _lastHideDuration; } }

        /// <summary>
        /// Mirrors Show(string message, float duration). Sets the label text,
        /// marks the toast as active, and schedules a hide.
        /// In the real class, this triggers LeanTween animations and a coroutine.
        /// </summary>
        public void Show(string message, float duration = 2f)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _labelText = message;

            if (_isActive)
            {
                // Already visible — restart hide timer with new duration
                _hideScheduled = true;
                _lastHideDuration = duration;
            }
            else
            {
                _isActive = true;
                _canvasGroupAlpha = 1f;
                _hideScheduled = true;
                _lastHideDuration = duration;
            }
        }

        /// <summary>
        /// Simulates the Hide coroutine completing. In the real class this is
        /// an IEnumerator that waits, then animates out.
        /// </summary>
        public void SimulateHideComplete()
        {
            _canvasGroupAlpha = 0f;
            _hideScheduled = false;
            _isActive = false;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ToastNotificationControllerTests
    {
        private ToastNotificationControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new ToastNotificationControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void Show_SetsLabelText_WhenCalledWithMessage()
        {
            // WHY: The toast must display the correct message so users see the
            //      right feedback (e.g., "Model loaded", "Saved successfully").

            // Arrange
            string expected = "Model loaded successfully";

            // Act
            _controller.Show(expected);

            // Assert
            Assert.AreEqual(expected, _controller.LabelText,
                "LabelText should match the message passed to Show().");
        }

        [Test]
        public void Show_SetsIsActiveTrue_WhenToastWasInactive()
        {
            // WHY: The active flag gates animation and prevents redundant
            //      slide-in animations when the toast is already on screen.

            // Arrange
            Assert.IsFalse(_controller.IsActive,
                "Controller should start inactive.");

            // Act
            _controller.Show("Hello");

            // Assert
            Assert.IsTrue(_controller.IsActive,
                "IsActive should be true after Show() is called.");
        }

        [Test]
        public void Show_SchedulesHide_WithSpecifiedDuration()
        {
            // WHY: The hide coroutine must use the caller-specified duration
            //      so different messages can stay visible for different lengths.

            // Act
            _controller.Show("Quick message", 1.5f);

            // Assert
            Assert.IsTrue(_controller.IsHideScheduled,
                "A hide should be scheduled after Show().");
            Assert.AreEqual(1.5f, _controller.LastHideDuration, 0.001f,
                "Scheduled hide duration should match the value passed to Show().");
        }

        [Test]
        public void Show_UsesDefaultDuration_WhenNoDurationSpecified()
        {
            // WHY: The default 2-second duration is the standard UX timing.
            //      Callers should not need to pass a value for the common case.

            // Act
            _controller.Show("Default timing");

            // Assert
            Assert.AreEqual(2f, _controller.LastHideDuration, 0.001f,
                "Default hide duration should be 2 seconds when not specified.");
        }

        [Test]
        public void Show_UpdatesMessageAndRestartsDuration_WhenAlreadyActive()
        {
            // WHY: If a toast is already visible and a new message arrives, the
            //      old hide timer must be replaced so the new message gets its
            //      full display duration — otherwise it could vanish instantly.

            // Arrange
            _controller.Show("First message", 3f);
            Assert.IsTrue(_controller.IsActive,
                "Toast should be active after first Show().");

            // Act
            _controller.Show("Second message", 5f);

            // Assert
            Assert.AreEqual("Second message", _controller.LabelText,
                "Label should update to the new message when shown while active.");
            Assert.AreEqual(5f, _controller.LastHideDuration, 0.001f,
                "Hide duration should be replaced with the new value.");
            Assert.IsTrue(_controller.IsActive,
                "Toast should remain active when shown again.");
        }

        [Test]
        public void Hide_ResetsActiveStateAndAlpha_WhenComplete()
        {
            // WHY: After the hide animation finishes, the toast must be fully
            //      invisible and marked inactive so the next Show() triggers
            //      the slide-in animation instead of just restarting the timer.

            // Arrange
            _controller.Show("Temporary");

            // Act
            _controller.SimulateHideComplete();

            // Assert
            Assert.IsFalse(_controller.IsActive,
                "IsActive should be false after hide completes.");
            Assert.AreEqual(0f, _controller.CanvasGroupAlpha, 0.001f,
                "Canvas alpha should be 0 after hide completes.");
            Assert.IsFalse(_controller.IsHideScheduled,
                "No hide should be scheduled after hide completes.");
        }

        [Test]
        public void Show_SetsAlphaToOne_WhenToastBecomesVisible()
        {
            // WHY: The canvas group alpha controls visibility. When the show
            //      animation completes the alpha must be 1 so the text is
            //      fully readable. A missed alpha update causes invisible toasts.

            // Arrange
            Assert.AreEqual(0f, _controller.CanvasGroupAlpha, 0.001f,
                "Alpha should start at 0 (hidden).");

            // Act
            _controller.Show("Visible now");

            // Assert
            Assert.AreEqual(1f, _controller.CanvasGroupAlpha, 0.001f,
                "Alpha should be 1 after Show() makes the toast visible.");
        }

        [Test]
        public void Show_ThrowsArgumentNullException_WhenMessageIsNull()
        {
            // WHY: Passing null would cause a NullReferenceException deep in
            //      the TMP_Text assignment. Failing fast with a clear exception
            //      helps developers identify the caller with the bad argument.

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _controller.Show(null),
                "Show() should throw ArgumentNullException when message is null.");
        }
    }
}
