// =============================================================================
// CameraSideButtonControllerTests.cs - Edit Mode Unit Tests for CameraSideButtonController
// =============================================================================
// TARGET CLASS: CameraSideButtonController
//   Real file: Assets/StudioA3/Scripts/UI/CameraSideButtonController.cs
//
// WHAT IT TESTS:
//   Simple hover controller that shows/hides CameraCube side-navigation buttons
//   when the pointer enters and exits the button area. Implements
//   IPointerEnterHandler and IPointerExitHandler. Validates that hover events
//   correctly toggle button visibility on the CameraCube.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real CameraSideButtonController is a MonoBehaviour implementing
//      Unity pointer interfaces. These tests exercise the logic through POCO
//      stubs so they compile standalone in the POC.
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
    /// Minimal stand-in for the CameraCube component that owns the side
    /// navigation buttons toggled by hover events.
    /// </summary>
    public class MockCameraCube
    {
        public bool SideNavigationButtonsVisible { get; private set; }

        public void ShowSideNavigationButtons()
        {
            SideNavigationButtonsVisible = true;
        }

        public void HideSideNavigationButtons()
        {
            SideNavigationButtonsVisible = false;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API of the real
    /// CameraSideButtonController without requiring MonoBehaviour or
    /// IPointerEnterHandler / IPointerExitHandler.
    /// </summary>
    public class CameraSideButtonControllerStub
    {
        private MockCameraCube _cameraCube;

        public CameraSideButtonControllerStub(MockCameraCube cameraCube)
        {
            _cameraCube = cameraCube ?? throw new ArgumentNullException(nameof(cameraCube));
        }

        public MockCameraCube CameraCube
        {
            get { return _cameraCube; }
        }

        /// <summary>
        /// Simulates OnPointerEnter from IPointerEnterHandler.
        /// </summary>
        public void OnPointerEnter()
        {
            _cameraCube.ShowSideNavigationButtons();
        }

        /// <summary>
        /// Simulates OnPointerExit from IPointerExitHandler.
        /// </summary>
        public void OnPointerExit()
        {
            _cameraCube.HideSideNavigationButtons();
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class CameraSideButtonControllerTests
    {
        private MockCameraCube _cameraCube;
        private CameraSideButtonControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _cameraCube = new MockCameraCube();
            _controller = new CameraSideButtonControllerStub(_cameraCube);
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
            _cameraCube = null;
        }

        [Test]
        public void OnPointerEnter_ShowsSideNavigationButtons_SoUserCanNavigateCamera()
        {
            // WHY: Side-navigation buttons let users rotate the camera to preset
            //       angles. They must appear on hover so users know they exist.

            // Act
            _controller.OnPointerEnter();

            // Assert
            Assert.IsTrue(_cameraCube.SideNavigationButtonsVisible,
                "Side navigation buttons should be visible after pointer enters the button area.");
        }

        [Test]
        public void OnPointerExit_HidesSideNavigationButtons_ReducesVisualClutter()
        {
            // WHY: Hiding buttons when the pointer leaves keeps the 3D viewport
            //       uncluttered, which is critical for immersive learning.

            // Arrange - show buttons first
            _controller.OnPointerEnter();
            Assert.IsTrue(_cameraCube.SideNavigationButtonsVisible,
                "Precondition: buttons should be visible before exit.");

            // Act
            _controller.OnPointerExit();

            // Assert
            Assert.IsFalse(_cameraCube.SideNavigationButtonsVisible,
                "Side navigation buttons should be hidden after pointer exits the button area.");
        }

        [Test]
        public void OnPointerEnter_CalledMultipleTimes_RemainsVisible()
        {
            // WHY: Rapid pointer movement might fire multiple enter events.
            //       Buttons must stay visible and not toggle unexpectedly.

            // Act
            _controller.OnPointerEnter();
            _controller.OnPointerEnter();
            _controller.OnPointerEnter();

            // Assert
            Assert.IsTrue(_cameraCube.SideNavigationButtonsVisible,
                "Side navigation buttons should remain visible after multiple pointer-enter events.");
        }

        [Test]
        public void OnPointerExit_WithoutPriorEnter_HidesButtonsSafely()
        {
            // WHY: Edge case where exit fires without a matching enter (e.g., scene
            //       reload). The controller must not crash and buttons should be hidden.

            // Act
            _controller.OnPointerExit();

            // Assert
            Assert.IsFalse(_cameraCube.SideNavigationButtonsVisible,
                "Side navigation buttons should be hidden even if exit fires without a prior enter.");
        }

        [Test]
        public void EnterThenExit_TogglesVisibilityCorrectly_FullHoverCycle()
        {
            // WHY: The most common use case is a single hover in/out. Verifying the
            //       full cycle ensures the CameraCube state is consistent.

            // Initially hidden
            Assert.IsFalse(_cameraCube.SideNavigationButtonsVisible,
                "Buttons should start hidden before any interaction.");

            // Hover in
            _controller.OnPointerEnter();
            Assert.IsTrue(_cameraCube.SideNavigationButtonsVisible,
                "Buttons should be visible after pointer enters.");

            // Hover out
            _controller.OnPointerExit();
            Assert.IsFalse(_cameraCube.SideNavigationButtonsVisible,
                "Buttons should be hidden after pointer exits.");
        }

        [Test]
        public void Constructor_NullCameraCube_ThrowsArgumentNullException()
        {
            // WHY: CameraSideButtonController requires a CameraCube reference.
            //       Failing fast prevents confusing NullReferenceExceptions at runtime.

            Assert.Throws<ArgumentNullException>(() => new CameraSideButtonControllerStub(null),
                "Constructor should throw ArgumentNullException when CameraCube is null.");
        }

        [Test]
        public void CameraCube_Property_ReturnsSameInstanceFromConstructor()
        {
            // WHY: The controller must delegate to the exact CameraCube it was
            //       configured with — not a copy or a different instance.

            Assert.AreSame(_cameraCube, _controller.CameraCube,
                "CameraCube property should return the same instance provided at construction.");
        }
    }
}
