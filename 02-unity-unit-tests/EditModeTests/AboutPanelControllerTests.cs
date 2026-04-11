// =============================================================================
// AboutPanelControllerTests.cs - Edit Mode Unit Tests for AboutPanelController
// =============================================================================
// TARGET CLASS: AboutPanelController
//   Real file: Assets/CommonA3/zSpace/AboutMenu/AboutPanelController.cs
//
// WHAT IT TESTS:
//   The AboutPanelController manages the about/settings panel UI, handling
//   settings toggle visibility, external button routing (About, License),
//   blocking plane click dismissal, and license check state tracking.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real AboutPanelController is a MonoBehaviour with inspector
//      references. These tests exercise the routing and state logic through
//      lightweight POCO stubs so they compile standalone without Unity.
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
    /// Minimal stand-in for CanvasGroup alpha/blocksRaycasts behavior.
    /// </summary>
    public class CanvasGroupStub
    {
        public float Alpha { get; set; }
        public bool BlocksRaycasts { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for BlockingPlaneController show/hide behavior.
    /// </summary>
    public class BlockingPlaneControllerStub
    {
        public bool IsVisible { get; private set; }
        public int ShowCallCount { get; private set; }
        public int HideCallCount { get; private set; }

        public void Show()
        {
            IsVisible = true;
            ShowCallCount++;
        }

        public void Hide()
        {
            IsVisible = false;
            HideCallCount++;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for ToggleButton with IsToggled state.
    /// </summary>
    public class ToggleButtonStub
    {
        public bool IsToggled { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API and key behaviors of
    /// AboutPanelController without requiring MonoBehaviour or Unity runtime.
    /// Covers ExternalButtonPress routing, blocking plane dismissal,
    /// settings panel visibility, and license check state tracking.
    /// </summary>
    public class AboutPanelControllerStub
    {
        public CanvasGroupStub AboutPanelCanvas { get; set; }
        public CanvasGroupStub SettingsPanelCanvas { get; set; }
        public CanvasGroupStub LicenseManagerBlockingPlaneCanvas { get; set; }
        public BlockingPlaneControllerStub BlockingPlane { get; set; }
        public ToggleButtonStub SettingsToggle { get; set; }

        private bool _doNotCloseOnLicenseCheck = false;
        private string _lastRoutedButton = null;

        public bool DoNotCloseOnLicenseCheck
        {
            get { return _doNotCloseOnLicenseCheck; }
        }

        public string LastRoutedButton
        {
            get { return _lastRoutedButton; }
        }

        public AboutPanelControllerStub()
        {
            AboutPanelCanvas = new CanvasGroupStub();
            SettingsPanelCanvas = new CanvasGroupStub();
            LicenseManagerBlockingPlaneCanvas = new CanvasGroupStub();
            BlockingPlane = new BlockingPlaneControllerStub();
            SettingsToggle = new ToggleButtonStub();
        }

        /// <summary>
        /// Routes external button presses to the correct handler based on
        /// button name content. Mirrors the real ExternalButtonPress method.
        /// </summary>
        public void ExternalButtonPress(string buttonName)
        {
            if (buttonName.Contains("About"))
            {
                _lastRoutedButton = "About";
            }
            else if (buttonName.Contains("License"))
            {
                _lastRoutedButton = "LicenseManager";
            }
        }

        /// <summary>
        /// Simulates the blocking plane click handler: if settings panel
        /// is visible, hide blocking plane and untoggle settings.
        /// </summary>
        public void HandleOnBlockingPlaneClicked()
        {
            if (this.SettingsPanelCanvas.Alpha > 0)
            {
                BlockingPlane.Hide();
                this.SettingsToggle.IsToggled = false;
            }
        }

        /// <summary>
        /// Simulates the license check completed handler that sets the
        /// _doNotCloseOnLicenseCheck flag.
        /// </summary>
        public void HandleOnLicenseCheckCompleted(string licensingType, bool rerunAction)
        {
            this._doNotCloseOnLicenseCheck = true;
        }

        /// <summary>
        /// Simulates the cancel action from the license manager popup:
        /// hides the blocking plane canvas without launching the license UI.
        /// </summary>
        public void HandleLicenseManagerPopupCancel()
        {
            this.LicenseManagerBlockingPlaneCanvas.Alpha = 0;
            this.LicenseManagerBlockingPlaneCanvas.BlocksRaycasts = false;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class AboutPanelControllerTests
    {
        private AboutPanelControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new AboutPanelControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        // WHY: ExternalButtonPress is the public entry point that the HamburgerMenu
        // uses to trigger About or License Manager. Correct routing prevents dead buttons.
        [Test]
        public void ExternalButtonPress_RoutesToAbout_WhenButtonNameContainsAbout()
        {
            // Act
            _controller.ExternalButtonPress("AboutButton");

            // Assert
            Assert.AreEqual("About", _controller.LastRoutedButton,
                "ExternalButtonPress should route to 'About' when the button name contains 'About'.");
        }

        // WHY: The License Manager option must also be reachable through external routing.
        [Test]
        public void ExternalButtonPress_RoutesToLicenseManager_WhenButtonNameContainsLicense()
        {
            // Act
            _controller.ExternalButtonPress("LicenseManager");

            // Assert
            Assert.AreEqual("LicenseManager", _controller.LastRoutedButton,
                "ExternalButtonPress should route to 'LicenseManager' when the button name contains 'License'.");
        }

        // WHY: Unrecognized button names should not crash or route anywhere.
        [Test]
        public void ExternalButtonPress_DoesNotRoute_WhenButtonNameIsUnrecognized()
        {
            // Act
            _controller.ExternalButtonPress("Settings");

            // Assert
            Assert.IsNull(_controller.LastRoutedButton,
                "ExternalButtonPress should not route for an unrecognized button name.");
        }

        // WHY: Clicking the blocking plane while settings is visible should dismiss
        // the settings panel, preventing users from being stuck behind an overlay.
        [Test]
        public void HandleOnBlockingPlaneClicked_HidesSettingsAndUntoggles_WhenSettingsVisible()
        {
            // Arrange
            _controller.SettingsPanelCanvas.Alpha = 1;
            _controller.SettingsToggle.IsToggled = true;

            // Act
            _controller.HandleOnBlockingPlaneClicked();

            // Assert
            Assert.IsFalse(_controller.BlockingPlane.IsVisible,
                "Blocking plane should be hidden after clicking it while settings is visible.");
            Assert.IsFalse(_controller.SettingsToggle.IsToggled,
                "Settings toggle should be unset after blocking plane click.");
        }

        // WHY: If settings panel is not visible, blocking plane clicks should be
        // ignored to avoid incorrectly hiding other UI elements.
        [Test]
        public void HandleOnBlockingPlaneClicked_DoesNothing_WhenSettingsNotVisible()
        {
            // Arrange
            _controller.SettingsPanelCanvas.Alpha = 0;
            _controller.SettingsToggle.IsToggled = false;

            // Act
            _controller.HandleOnBlockingPlaneClicked();

            // Assert
            Assert.AreEqual(0, _controller.BlockingPlane.HideCallCount,
                "BlockingPlane.Hide should not be called when settings panel is not visible.");
        }

        // WHY: The license check completed handler must set the flag that prevents
        // the app from closing when the user opens the License Manager in pro/lite mode.
        [Test]
        public void HandleOnLicenseCheckCompleted_SetsDoNotCloseFlag()
        {
            // Arrange
            Assert.IsFalse(_controller.DoNotCloseOnLicenseCheck,
                "DoNotCloseOnLicenseCheck should be false initially.");

            // Act
            _controller.HandleOnLicenseCheckCompleted("Pro", false);

            // Assert
            Assert.IsTrue(_controller.DoNotCloseOnLicenseCheck,
                "DoNotCloseOnLicenseCheck should be true after license check completes.");
        }

        // WHY: Cancelling the license manager popup should hide the blocking plane
        // canvas and restore interactivity without launching the license UI.
        [Test]
        public void HandleLicenseManagerPopupCancel_HidesBlockingPlaneCanvas()
        {
            // Arrange
            _controller.LicenseManagerBlockingPlaneCanvas.Alpha = 1;
            _controller.LicenseManagerBlockingPlaneCanvas.BlocksRaycasts = true;

            // Act
            _controller.HandleLicenseManagerPopupCancel();

            // Assert
            Assert.AreEqual(0, _controller.LicenseManagerBlockingPlaneCanvas.Alpha,
                "LicenseManager blocking plane alpha should be 0 after cancel.");
            Assert.IsFalse(_controller.LicenseManagerBlockingPlaneCanvas.BlocksRaycasts,
                "LicenseManager blocking plane should stop blocking raycasts after cancel.");
        }

        // WHY: ExternalButtonPress prioritizes "About" over "License" when both
        // keywords are present, matching the if/else-if ordering in the real code.
        [Test]
        public void ExternalButtonPress_PrioritizesAbout_WhenBothKeywordsPresent()
        {
            // Act
            _controller.ExternalButtonPress("AboutLicenseButton");

            // Assert
            Assert.AreEqual("About", _controller.LastRoutedButton,
                "When button name contains both 'About' and 'License', 'About' should take priority per the if/else-if ordering.");
        }
    }
}
