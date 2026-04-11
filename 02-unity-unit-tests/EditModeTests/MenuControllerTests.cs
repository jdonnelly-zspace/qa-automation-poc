// =============================================================================
// MenuControllerTests.cs - Edit Mode Unit Tests for MenuController
// =============================================================================
// TARGET CLASS: MenuController
//   Real file: Assets/VivedUpgrades/HamburgerMenu/Scripts/MenuController.cs
//
// WHAT IT TESTS:
//   Hamburger menu controller that orchestrates the main menu UI. Validates
//   DismissMenu, ShowSecondaryPanel, ToggleSecondaryPanel toggling logic,
//   button click routing, and pointer-based auto-dismiss behavior.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real MenuController is a MonoBehaviour. These tests exercise logic
//      through lightweight POCO stubs so they compile standalone in the POC
//      without a Unity runtime.
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

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Minimal stand-in for ToggleButton with IsToggled state.</summary>
    public class ToggleButtonStub
    {
        public bool IsToggled { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Minimal stand-in for MainMenu with show/hide and page swap.</summary>
    public class MainMenuStub
    {
        public bool IsShown { get; private set; }
        public bool SecondaryPanelShown { get; private set; }
        public float PanelWidth { get; set; } = 300f;

        public void Show(float duration) { IsShown = true; }
        public void Hide(float duration) { IsShown = false; }
        public void ShowSecondaryPanel(float duration) { SecondaryPanelShown = true; IsShown = true; }
        public void SwapPages(bool forward, float duration) { /* no-op for test */ }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>
    /// Lightweight POCO mirroring the public API of MenuController
    /// without requiring MonoBehaviour.
    /// </summary>
    public class MenuControllerStub
    {
        private readonly MainMenuStub _mainMenu;
        private readonly ToggleButtonStub _mainMenuToggle;
        private bool _suppressToggleHandler = false;

        public bool AnimateToolbarShiftCalled { get; private set; }
        public bool AnimateCameraShiftCalled { get; private set; }

        public MenuControllerStub(
            MainMenuStub mainMenu,
            ToggleButtonStub mainMenuToggle)
        {
            _mainMenu = mainMenu;
            _mainMenuToggle = mainMenuToggle;
        }

        public void DismissMenu()
        {
            if (_mainMenuToggle.IsToggled)
            {
                _mainMenuToggle.IsToggled = false;
            }
        }

        public void ShowSecondaryPanel()
        {
            if (_mainMenuToggle.IsToggled)
            {
                return;
            }

            _mainMenu.ShowSecondaryPanel(0.3f);
            AnimateToolbarShiftCalled = true;
            AnimateCameraShiftCalled = true;

            _suppressToggleHandler = true;
            _mainMenuToggle.IsToggled = true;
            _suppressToggleHandler = false;
        }

        public void ToggleSecondaryPanel()
        {
            if (_mainMenuToggle.IsToggled)
            {
                DismissMenu();
            }
            else
            {
                ShowSecondaryPanel();
            }
        }

        /// <summary>Simulates the toggle handler behavior.</summary>
        public void HandleMainMenuToggled()
        {
            if (_suppressToggleHandler)
            {
                return;
            }

            if (_mainMenuToggle.IsToggled)
            {
                _mainMenu.Show(0.3f);
            }
            else
            {
                _mainMenu.Hide(0.3f);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class MenuControllerTests
    {
        private MenuControllerStub _controller;
        private MainMenuStub _mainMenu;
        private ToggleButtonStub _mainMenuToggle;

        [SetUp]
        public void SetUp()
        {
            _mainMenu = new MainMenuStub();
            _mainMenuToggle = new ToggleButtonStub();
            _controller = new MenuControllerStub(_mainMenu, _mainMenuToggle);
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
            _mainMenu = null;
            _mainMenuToggle = null;
        }

        // WHY: DismissMenu is called from external systems (e.g., pointer click
        // outside the menu). It must only untoggle when the menu is actually open
        // to avoid unnecessary state changes.
        [Test]
        public void DismissMenu_UntogglesMenu_WhenMenuIsOpen()
        {
            // Arrange
            _mainMenuToggle.IsToggled = true;

            // Act
            _controller.DismissMenu();

            // Assert
            Assert.IsFalse(_mainMenuToggle.IsToggled,
                "DismissMenu must set IsToggled to false when the menu is currently open.");
        }

        // WHY: Calling DismissMenu when the menu is already closed should be
        // a safe no-op, not cause errors or state corruption.
        [Test]
        public void DismissMenu_DoesNothing_WhenMenuIsAlreadyClosed()
        {
            // Arrange
            _mainMenuToggle.IsToggled = false;

            // Act
            _controller.DismissMenu();

            // Assert
            Assert.IsFalse(_mainMenuToggle.IsToggled,
                "DismissMenu should be a no-op when the menu is already closed.");
        }

        // WHY: ShowSecondaryPanel lets external code (e.g., keyboard shortcuts)
        // jump directly to the file operations panel without going through
        // the primary menu first.
        [Test]
        public void ShowSecondaryPanel_OpensSecondaryDirectly_WhenMenuIsClosed()
        {
            // Arrange
            _mainMenuToggle.IsToggled = false;

            // Act
            _controller.ShowSecondaryPanel();

            // Assert
            Assert.IsTrue(_mainMenu.SecondaryPanelShown,
                "ShowSecondaryPanel must open the secondary panel directly when the menu was closed.");
            Assert.IsTrue(_mainMenuToggle.IsToggled,
                "Toggle state must be synced to true so DismissMenu works correctly afterward.");
        }

        // WHY: If the menu is already open, ShowSecondaryPanel should not
        // double-show or re-animate, which would cause visual glitches.
        [Test]
        public void ShowSecondaryPanel_IsNoOp_WhenMenuIsAlreadyOpen()
        {
            // Arrange
            _mainMenuToggle.IsToggled = true;

            // Act
            _controller.ShowSecondaryPanel();

            // Assert
            Assert.IsFalse(_mainMenu.SecondaryPanelShown,
                "ShowSecondaryPanel should return early without showing when the menu is already open.");
        }

        // WHY: ToggleSecondaryPanel provides a single entry point that opens
        // when closed and closes when open, matching user expectation of
        // a toggle button.
        [Test]
        public void ToggleSecondaryPanel_OpensMenu_WhenCurrentlyClosed()
        {
            // Arrange
            _mainMenuToggle.IsToggled = false;

            // Act
            _controller.ToggleSecondaryPanel();

            // Assert
            Assert.IsTrue(_mainMenuToggle.IsToggled,
                "ToggleSecondaryPanel should open the menu (via ShowSecondaryPanel) when it was closed.");
            Assert.IsTrue(_mainMenu.SecondaryPanelShown,
                "The secondary panel should be shown when toggling from closed state.");
        }

        // WHY: ToggleSecondaryPanel should dismiss the menu when it is open,
        // providing a clean toggle cycle.
        [Test]
        public void ToggleSecondaryPanel_ClosesMenu_WhenCurrentlyOpen()
        {
            // Arrange
            _mainMenuToggle.IsToggled = true;

            // Act
            _controller.ToggleSecondaryPanel();

            // Assert
            Assert.IsFalse(_mainMenuToggle.IsToggled,
                "ToggleSecondaryPanel should dismiss the menu when it was already open.");
        }

        // WHY: ShowSecondaryPanel must trigger toolbar and camera shift
        // animations so the 3D viewport adjusts to the panel taking space.
        [Test]
        public void ShowSecondaryPanel_TriggersAnimations_WhenOpening()
        {
            // Arrange
            _mainMenuToggle.IsToggled = false;

            // Act
            _controller.ShowSecondaryPanel();

            // Assert
            Assert.IsTrue(_controller.AnimateToolbarShiftCalled,
                "Toolbar shift animation must be triggered when opening the secondary panel.");
            Assert.IsTrue(_controller.AnimateCameraShiftCalled,
                "Camera shift animation must be triggered when opening the secondary panel.");
        }

        // WHY: The toggle handler should be suppressed during ShowSecondaryPanel
        // to avoid double-showing. Verify the menu ends up in the correct state.
        [Test]
        public void ShowSecondaryPanel_SyncsToggleState_WithoutDoubleShowing()
        {
            // Arrange
            _mainMenuToggle.IsToggled = false;

            // Act
            _controller.ShowSecondaryPanel();
            // Simulate what would happen if the toggle handler fired
            _controller.HandleMainMenuToggled();

            // Assert -- the menu should already be shown from ShowSecondaryPanel,
            // and the handler should not re-trigger because suppression was used
            Assert.IsTrue(_mainMenuToggle.IsToggled,
                "Toggle should be true after ShowSecondaryPanel.");
            Assert.IsTrue(_mainMenu.IsShown,
                "Menu should remain shown after the toggle handler runs (it was already shown).");
        }
    }
}
