// =============================================================================
// CameraBarControllerTests.cs - Edit Mode Unit Tests for CameraBarController
// =============================================================================
// TARGET CLASS: CameraBarController
//   Real file: Assets/CommonA3/zSpace/Scripts/Tools/CameraTool/CameraBarController.cs
//
// WHAT IT TESTS:
//   Maps camera toolbar buttons to navigation actions (ZoomIn, ZoomOut, Reset)
//   and formats the current zoom level as a human-readable percentage string
//   for the HUD overlay. Tests validate formatting at key zoom levels, correct
//   button-to-action mapping, and safe handling of unknown button names.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace the using directives with the real namespaces.
//   3. The real CameraBarController is a MonoBehaviour attached to the camera
//      toolbar UI. The stub here exercises only the formatting and mapping
//      logic without Unity runtime or UI dependencies.
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
    /// Lightweight stand-in for the real CameraBarController. Handles zoom
    /// level formatting and button-to-action mapping without UI components.
    /// </summary>
    public class CameraBarControllerStub
    {
        public float ZoomLevel { get; set; } = 1.0f;
        public string LastAction { get; private set; }

        public string FormatZoomLevel(float level)
        {
            int percentage = GetZoomPercentage(level);
            return $"{percentage}%";
        }

        public void HandleButtonClicked(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName))
            {
                LastAction = null;
                return;
            }

            switch (buttonName)
            {
                case "ZoomIn":
                    LastAction = "ZoomIn";
                    break;
                case "ZoomOut":
                    LastAction = "ZoomOut";
                    break;
                case "Reset":
                    LastAction = "Reset";
                    break;
                default:
                    LastAction = null;
                    break;
            }
        }

        public int GetZoomPercentage(float level)
        {
            return (int)Math.Round(level * 100.0f);
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class CameraBarControllerTests
    {
        private CameraBarControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new CameraBarControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void FormatZoomLevel_At1Point0_Returns100Percent()
        {
            // WHY: 1.0 is the default zoom; the HUD must display "100%" so students
            // know they are viewing the anatomy model at its natural scale.

            // Act
            string result = _controller.FormatZoomLevel(1.0f);

            // Assert
            Assert.AreEqual("100%", result,
                "A zoom level of 1.0 should format as '100%' for the default view.");
        }

        [Test]
        public void FormatZoomLevel_At0Point5_Returns50Percent()
        {
            // WHY: Zooming out to 50% gives students an overview of larger anatomical
            // systems; the display must reflect the halved magnification.

            // Act
            string result = _controller.FormatZoomLevel(0.5f);

            // Assert
            Assert.AreEqual("50%", result,
                "A zoom level of 0.5 should format as '50%' for the zoomed-out view.");
        }

        [Test]
        public void FormatZoomLevel_At2Point0_Returns200Percent()
        {
            // WHY: 200% zoom lets students inspect fine structures like capillaries;
            // the label must clearly communicate the magnification level.

            // Act
            string result = _controller.FormatZoomLevel(2.0f);

            // Assert
            Assert.AreEqual("200%", result,
                "A zoom level of 2.0 should format as '200%' for the magnified view.");
        }

        [Test]
        public void FormatZoomLevel_At0Point0_Returns0Percent()
        {
            // WHY: Edge case: a zero zoom level should not crash or produce garbage;
            // it must format predictably even if the value is logically invalid.

            // Act
            string result = _controller.FormatZoomLevel(0.0f);

            // Assert
            Assert.AreEqual("0%", result,
                "A zoom level of 0.0 should format as '0%' without errors.");
        }

        [Test]
        public void HandleButtonClicked_ZoomIn_MapsCorrectly()
        {
            // WHY: The ZoomIn button on the camera toolbar must trigger the correct
            // navigation action so the camera moves toward the model.

            // Act
            _controller.HandleButtonClicked("ZoomIn");

            // Assert
            Assert.AreEqual("ZoomIn", _controller.LastAction,
                "Clicking the ZoomIn button should set LastAction to 'ZoomIn'.");
        }

        [Test]
        public void HandleButtonClicked_ZoomOut_MapsCorrectly()
        {
            // WHY: The ZoomOut button must reliably pull the camera back so students
            // can see the full skeleton or organ system in context.

            // Act
            _controller.HandleButtonClicked("ZoomOut");

            // Assert
            Assert.AreEqual("ZoomOut", _controller.LastAction,
                "Clicking the ZoomOut button should set LastAction to 'ZoomOut'.");
        }

        [Test]
        public void HandleButtonClicked_Reset_MapsCorrectly()
        {
            // WHY: The Reset button returns the camera to its default position so
            // students can start a new exploration without reloading the lesson.

            // Act
            _controller.HandleButtonClicked("Reset");

            // Assert
            Assert.AreEqual("Reset", _controller.LastAction,
                "Clicking the Reset button should set LastAction to 'Reset'.");
        }

        [Test]
        public void HandleButtonClicked_UnknownButton_HandledSafely()
        {
            // WHY: If a UI element sends an unrecognized button name (e.g., due to a
            // layout change), the controller must not crash or execute a wrong action.

            // Arrange - set a known action first
            _controller.HandleButtonClicked("ZoomIn");

            // Act
            _controller.HandleButtonClicked("FlyThrough");

            // Assert
            Assert.IsNull(_controller.LastAction,
                "An unknown button name should set LastAction to null rather than retaining the previous action.");
        }

        [Test]
        public void GetZoomPercentage_RoundsCorrectly()
        {
            // WHY: Floating-point zoom levels like 0.755 must round to clean integers
            // for display; truncation would show misleading values to students.

            // Act & Assert
            Assert.AreEqual(76, _controller.GetZoomPercentage(0.755f),
                "0.755 should round to 76% (nearest integer) for a clean display.");
            Assert.AreEqual(33, _controller.GetZoomPercentage(0.333f),
                "0.333 should round to 33% for display.");
            Assert.AreEqual(150, _controller.GetZoomPercentage(1.5f),
                "1.5 should produce 150% for display.");
        }
    }
}
