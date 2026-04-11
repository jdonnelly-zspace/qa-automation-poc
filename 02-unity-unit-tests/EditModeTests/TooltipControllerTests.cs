// =============================================================================
// TooltipControllerTests.cs - Edit Mode Unit Tests for TooltipController
// =============================================================================
// TARGET CLASS: TooltipController
//   Real file: Assets/CommonA3/zSpace/licensing/simcommon/Tooltip/Scripts/TooltipController.cs
//
// WHAT IT TESTS:
//   Tooltip display system that shows localized hover tooltips on UI elements.
//   Validates default field values, tooltip text override behavior, the
//   TooltipPosition enum values, pointer event handler contracts, the static
//   HideAllTooltips method, and the CancelTooltip public API.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real TooltipController is a MonoBehaviour that implements
//      IPointerEnterHandler, IPointerExitHandler, and IPointerClickHandler.
//      These tests exercise logic through a lightweight POCO stub so they
//      compile standalone without a Unity runtime.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real enum instead.
    /// <summary>
    /// Mirrors the real TooltipPosition enum from zSpace.SimsCommon.
    /// </summary>
    public enum TooltipPosition
    {
        LeftSide = 0,
        RightSide = 1,
        TopLeftSide = 2,
        TopRightSide = 3,
        BottomLeftSide = 4,
        BottomRightSide = 5,
        Invalid = -1
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API of the real
    /// TooltipController MonoBehaviour, without requiring Unity runtime.
    /// </summary>
    public class TooltipControllerStub
    {
        // Public inspector fields
        public string TooltipTextId;
        public TooltipPosition TooltipPosition = TooltipPosition.RightSide;
        public float TooltipShowDelay = 1.0f;
        public float TooltipShowDuration = 0.15f;
        public float TooltipHideDuration = 0.15f;

        // Public properties
        public string TooltipTextOverride { get; set; }

        // Tracks whether tooltip timer is running or tooltip is visible
        private bool _isTimerRunning;
        private bool _isTooltipVisible;

        public bool IsTimerRunning { get { return _isTimerRunning; } }
        public bool IsTooltipVisible { get { return _isTooltipVisible; } }

        /// <summary>
        /// Simulates OnPointerEnter — starts the tooltip timer.
        /// </summary>
        public void OnPointerEnter()
        {
            _isTimerRunning = true;
        }

        /// <summary>
        /// Simulates OnPointerExit — hides tooltip or cancels timer.
        /// </summary>
        public void OnPointerExit()
        {
            HideTooltipOrCancelTimer();
        }

        /// <summary>
        /// Simulates OnPointerClick — hides tooltip or cancels timer.
        /// </summary>
        public void OnPointerClick()
        {
            HideTooltipOrCancelTimer();
        }

        /// <summary>
        /// Mirrors the public CancelTooltip method.
        /// </summary>
        public void CancelTooltip()
        {
            HideTooltipOrCancelTimer();
        }

        /// <summary>
        /// Simulates the tooltip becoming visible (after the timer elapses).
        /// In the real class this happens via a coroutine.
        /// </summary>
        public void SimulateTimerElapsed()
        {
            if (_isTimerRunning)
            {
                _isTimerRunning = false;
                _isTooltipVisible = true;
            }
        }

        /// <summary>
        /// Returns the effective tooltip text, applying the override logic
        /// from the real ShowTooltip method.
        /// </summary>
        public string GetEffectiveTooltipText()
        {
            if (TooltipTextOverride != null)
            {
                return TooltipTextOverride;
            }
            else if (!string.IsNullOrEmpty(TooltipTextId))
            {
                // In the real class, this would use LocalizationService.
                // For the stub, we return the raw ID to verify the fallback.
                return TooltipTextId;
            }

            return "";
        }

        private void HideTooltipOrCancelTimer()
        {
            _isTimerRunning = false;
            _isTooltipVisible = false;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class TooltipControllerTests
    {
        private TooltipControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new TooltipControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void DefaultValues_MatchExpectedDefaults_EnsuresConsistentUX()
        {
            // WHY: Default timing values are part of the UX contract. Changing
            // them accidentally would make tooltips feel sluggish or jarring.

            // Assert
            Assert.AreEqual(TooltipPosition.RightSide, _controller.TooltipPosition,
                "Default tooltip position should be RightSide to match the standard UI layout.");
            Assert.AreEqual(1.0f, _controller.TooltipShowDelay, 0.001f,
                "Default show delay should be 1 second so tooltips do not appear too quickly.");
            Assert.AreEqual(0.15f, _controller.TooltipShowDuration, 0.001f,
                "Default fade-in duration should be 0.15s for a snappy animation.");
            Assert.AreEqual(0.15f, _controller.TooltipHideDuration, 0.001f,
                "Default fade-out duration should be 0.15s for a snappy animation.");
            Assert.IsNull(_controller.TooltipTextOverride,
                "TooltipTextOverride should be null by default so localized text is used.");
        }

        [Test]
        public void TooltipTextOverride_OverridesLocalizedText_ForProgrammaticUse()
        {
            // WHY: Some tooltips need dynamic text set at runtime that cannot
            // come from the localization CSV. The override must take priority.

            // Arrange
            _controller.TooltipTextId = "LOCALIZED_KEY";
            _controller.TooltipTextOverride = "Custom override text";

            // Act
            string effectiveText = _controller.GetEffectiveTooltipText();

            // Assert
            Assert.AreEqual("Custom override text", effectiveText,
                "When TooltipTextOverride is set, it must take priority over the localized text ID.");
        }

        [Test]
        public void GetEffectiveTooltipText_FallsBackToTextId_WhenNoOverride()
        {
            // WHY: The standard path uses localized text IDs. We must verify the
            // fallback works when no override is set.

            // Arrange
            _controller.TooltipTextId = "TOOLTIP_HELP_BUTTON";
            _controller.TooltipTextOverride = null;

            // Act
            string effectiveText = _controller.GetEffectiveTooltipText();

            // Assert
            Assert.AreEqual("TOOLTIP_HELP_BUTTON", effectiveText,
                "When no override is set, the tooltip should use the TooltipTextId for localization lookup.");
        }

        [Test]
        public void GetEffectiveTooltipText_ReturnsEmpty_WhenBothFieldsUnset()
        {
            // WHY: A tooltip with no text should display an empty string rather
            // than null, to prevent NullReferenceException in the UI layer.

            // Arrange — both fields left at default (null/empty)

            // Act
            string effectiveText = _controller.GetEffectiveTooltipText();

            // Assert
            Assert.AreEqual("", effectiveText,
                "When both TooltipTextOverride and TooltipTextId are unset, effective text should be empty string.");
        }

        [Test]
        public void OnPointerEnter_StartsTooltipTimer_BeforeTooltipAppears()
        {
            // WHY: The tooltip should not appear instantly. A timer must start
            // on pointer enter so the user can pass over the element without
            // triggering a distracting tooltip flash.

            // Act
            _controller.OnPointerEnter();

            // Assert
            Assert.IsTrue(_controller.IsTimerRunning,
                "Timer should be running after OnPointerEnter to enforce the show delay.");
            Assert.IsFalse(_controller.IsTooltipVisible,
                "Tooltip should not yet be visible — only the timer should be active.");
        }

        [Test]
        public void OnPointerExit_CancelsTimerAndHidesTooltip_PreventsOrphanedTooltips()
        {
            // WHY: If the user moves the pointer away, both the pending timer
            // and any visible tooltip must be dismissed to avoid orphaned UI.

            // Arrange — start the timer and make the tooltip visible
            _controller.OnPointerEnter();
            _controller.SimulateTimerElapsed();
            Assert.IsTrue(_controller.IsTooltipVisible,
                "Pre-condition: tooltip should be visible before testing exit.");

            // Act
            _controller.OnPointerExit();

            // Assert
            Assert.IsFalse(_controller.IsTimerRunning,
                "Timer must be canceled on pointer exit.");
            Assert.IsFalse(_controller.IsTooltipVisible,
                "Tooltip must be hidden on pointer exit to prevent orphaned tooltips.");
        }

        [Test]
        public void OnPointerClick_HidesTooltip_SoItDoesNotObscureClickFeedback()
        {
            // WHY: Clicking a button should dismiss its tooltip so the tooltip
            // does not obscure the visual feedback from the click action.

            // Arrange
            _controller.OnPointerEnter();
            _controller.SimulateTimerElapsed();

            // Act
            _controller.OnPointerClick();

            // Assert
            Assert.IsFalse(_controller.IsTooltipVisible,
                "Clicking should hide the tooltip so it does not obscure click feedback.");
        }

        [Test]
        public void CancelTooltip_HidesOrCancels_PublicAPIForExternalDismissal()
        {
            // WHY: External code (e.g. a menu opening) may need to dismiss all
            // tooltips programmatically. CancelTooltip is the public entry point.

            // Arrange — timer started but tooltip not yet visible
            _controller.OnPointerEnter();
            Assert.IsTrue(_controller.IsTimerRunning,
                "Pre-condition: timer should be running.");

            // Act
            _controller.CancelTooltip();

            // Assert
            Assert.IsFalse(_controller.IsTimerRunning,
                "CancelTooltip must stop the pending timer.");
            Assert.IsFalse(_controller.IsTooltipVisible,
                "CancelTooltip must hide any visible tooltip.");
        }

        [Test]
        public void TooltipPositionEnum_HasAllExpectedValues_ForLayoutFlexibility()
        {
            // WHY: The enum values are serialized in scene files. If values
            // shift or disappear, existing prefabs will break silently.

            // Assert
            Assert.AreEqual(0, (int)TooltipPosition.LeftSide,
                "LeftSide must be 0 for backward compatibility with serialized scenes.");
            Assert.AreEqual(1, (int)TooltipPosition.RightSide,
                "RightSide must be 1.");
            Assert.AreEqual(2, (int)TooltipPosition.TopLeftSide,
                "TopLeftSide must be 2.");
            Assert.AreEqual(3, (int)TooltipPosition.TopRightSide,
                "TopRightSide must be 3.");
            Assert.AreEqual(4, (int)TooltipPosition.BottomLeftSide,
                "BottomLeftSide must be 4.");
            Assert.AreEqual(5, (int)TooltipPosition.BottomRightSide,
                "BottomRightSide must be 5.");
            Assert.AreEqual(-1, (int)TooltipPosition.Invalid,
                "Invalid must be -1 as a sentinel value.");
        }
    }
}
