// =============================================================================
// HamburgerMenuControllerTests.cs - Edit Mode Unit Tests for HamburgerMenuController
// =============================================================================
// TARGET CLASS: HamburgerMenuController
//   Real file: Assets/StudioA3/Scripts/UI/HamburgerMenuController.cs
//
// WHAT IT TESTS:
//   Hamburger menu toggle behavior and device-specific button visibility.
//   The controller toggles the menu open/closed and hides or shows buttons
//   (such as ZView) based on the connected zSpace device type. Tests validate
//   toggle state transitions, device detection from model codes, and
//   per-device button enablement rules.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real HamburgerMenuController is a MonoBehaviour that manages
//      Unity UI GameObjects. The stub here exercises only the toggle logic,
//      device mapping, and button visibility rules without Unity runtime.
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

    // TODO: DELETE this stub when integrating into the Unity project — use the real enum instead.
    /// <summary>
    /// Represents the type of zSpace device connected.
    /// </summary>
    public enum DeviceType
    {
        Desktop,
        Mako,
        Inspire,
        Unknown
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the toggle and device-visibility logic of the real
    /// HamburgerMenuController without requiring MonoBehaviour or UI GameObjects.
    /// </summary>
    public class HamburgerMenuControllerStub
    {
        public bool IsMenuOpen { get; private set; }
        public DeviceType CurrentDevice { get; private set; }
        public bool ZViewButtonEnabled { get; private set; }

        private static readonly Dictionary<string, DeviceType> ModelCodeMap =
            new Dictionary<string, DeviceType>(StringComparer.OrdinalIgnoreCase)
            {
                { "DSK-100", DeviceType.Desktop },
                { "DSK-200", DeviceType.Desktop },
                { "MKO-100", DeviceType.Mako },
                { "MKO-200", DeviceType.Mako },
                { "INS-100", DeviceType.Inspire },
                { "INS-200", DeviceType.Inspire }
            };

        public HamburgerMenuControllerStub()
        {
            IsMenuOpen = false;
            CurrentDevice = DeviceType.Unknown;
            ZViewButtonEnabled = true;
        }

        public void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;
        }

        public void UpdateButtonVisibility(DeviceType device)
        {
            CurrentDevice = device;
            // ZView is not supported on Inspire devices
            ZViewButtonEnabled = device != DeviceType.Inspire;
        }

        public DeviceType GetDeviceType(string modelCode)
        {
            if (string.IsNullOrEmpty(modelCode))
            {
                return DeviceType.Unknown;
            }

            if (ModelCodeMap.TryGetValue(modelCode, out DeviceType deviceType))
            {
                return deviceType;
            }

            return DeviceType.Unknown;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class HamburgerMenuControllerTests
    {
        private HamburgerMenuControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new HamburgerMenuControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void InitialState_MenuIsClosed()
        {
            // WHY: When the app launches, the hamburger menu should be collapsed
            // so the student sees the full 3D workspace without obstruction.

            // Assert
            Assert.IsFalse(_controller.IsMenuOpen,
                "Menu should be closed by default on initialization.");
        }

        [Test]
        public void ToggleMenu_OpensMenu_WhenCurrentlyClosed()
        {
            // WHY: Students tap the hamburger icon to access file and settings
            // options; the first tap must reliably open the menu.

            // Arrange — menu starts closed
            Assert.IsFalse(_controller.IsMenuOpen);

            // Act
            _controller.ToggleMenu();

            // Assert
            Assert.IsTrue(_controller.IsMenuOpen,
                "ToggleMenu should open the menu when it is currently closed.");
        }

        [Test]
        public void ToggleMenu_ClosesMenu_WhenCurrentlyOpen()
        {
            // WHY: After reviewing options, students tap the icon again to
            // dismiss the menu and return to the immersive 3D view.

            // Arrange — open the menu first
            _controller.ToggleMenu();
            Assert.IsTrue(_controller.IsMenuOpen);

            // Act
            _controller.ToggleMenu();

            // Assert
            Assert.IsFalse(_controller.IsMenuOpen,
                "ToggleMenu should close the menu when it is currently open.");
        }

        [Test]
        public void UpdateButtonVisibility_EnablesZView_OnDesktop()
        {
            // WHY: Desktop devices support ZView augmented reality sharing;
            // the button must be visible so teachers can present to the class.

            // Act
            _controller.UpdateButtonVisibility(DeviceType.Desktop);

            // Assert
            Assert.IsTrue(_controller.ZViewButtonEnabled,
                "ZView button should be enabled on Desktop devices.");
        }

        [Test]
        public void UpdateButtonVisibility_EnablesZView_OnMako()
        {
            // WHY: Mako devices also support ZView; hiding the button would
            // prevent teachers from using the AR presentation feature.

            // Act
            _controller.UpdateButtonVisibility(DeviceType.Mako);

            // Assert
            Assert.IsTrue(_controller.ZViewButtonEnabled,
                "ZView button should be enabled on Mako devices.");
        }

        [Test]
        public void UpdateButtonVisibility_DisablesZView_OnInspire()
        {
            // WHY: Inspire hardware does not support ZView; showing the button
            // would confuse students with a non-functional option.

            // Act
            _controller.UpdateButtonVisibility(DeviceType.Inspire);

            // Assert
            Assert.IsFalse(_controller.ZViewButtonEnabled,
                "ZView button should be disabled on Inspire devices.");
        }

        [Test]
        public void GetDeviceType_MapsKnownModelCodes()
        {
            // WHY: The app receives model codes from the hardware driver at
            // startup; correct mapping ensures device-specific features activate.

            // Act & Assert
            Assert.AreEqual(DeviceType.Desktop, _controller.GetDeviceType("DSK-100"),
                "DSK-100 should map to Desktop device type.");
            Assert.AreEqual(DeviceType.Mako, _controller.GetDeviceType("MKO-200"),
                "MKO-200 should map to Mako device type.");
            Assert.AreEqual(DeviceType.Inspire, _controller.GetDeviceType("INS-100"),
                "INS-100 should map to Inspire device type.");
        }

        [Test]
        public void GetDeviceType_ReturnsUnknown_ForUnrecognizedCodes()
        {
            // WHY: Future hardware revisions may introduce new model codes;
            // the app must degrade gracefully rather than crash.

            // Act & Assert
            Assert.AreEqual(DeviceType.Unknown, _controller.GetDeviceType("XYZ-999"),
                "Unrecognized model code should return Unknown device type.");
            Assert.AreEqual(DeviceType.Unknown, _controller.GetDeviceType(""),
                "Empty model code should return Unknown device type.");
            Assert.AreEqual(DeviceType.Unknown, _controller.GetDeviceType(null),
                "Null model code should return Unknown device type.");
        }
    }
}
