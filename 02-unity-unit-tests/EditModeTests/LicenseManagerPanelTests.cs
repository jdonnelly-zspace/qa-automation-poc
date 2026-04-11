// =============================================================================
// LicenseManagerPanelTests.cs - Edit Mode Unit Tests for LicenseManagerPanel
// =============================================================================
// TARGET CLASS: LicenseManagerPanel
//   Real file: Assets/CommonA3/zSpace/licensing/Modernization/UI/Scripts/LicenseManagerPanel.cs
//
// WHAT IT TESTS:
//   LicenseManagerPanel manages the license activation/deactivation UI with
//   button interactable state, license key and expiration date text display,
//   fade in/out behavior, and hittable (raycast) state. Tests verify the
//   public constants, text getters/setters, button interactable lookup,
//   hittable state, and fade behavior.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real LicenseManagerPanel uses CanvasGroupFader and Unity Buttons.
//      These tests use POCO stubs to verify state management logic.
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
    /// Lightweight POCO that mirrors the public API of LicenseManagerPanel.
    /// Tracks license key text, expiration date text, button interactable
    /// states, hittable/alpha state, and fade behavior.
    /// </summary>
    public class LicenseManagerPanelStub
    {
        public const string CheckUpdatesButtonId = "check-updates";
        public const string ActivateButtonId = "activate";
        public const string DeactivateButtonId = "deactivate";
        public const string CloseButtonId = "close";

        private string _licenseKeyText = "";
        private string _expirationDateText = "";
        private bool _isHittable = false;
        private float _alpha = 0f;
        private bool _destroyOnFadeOut = true;

        private Dictionary<string, bool> _buttonInteractableStates =
            new Dictionary<string, bool>();

        public bool DestroyOnFadeOut
        {
            get { return _destroyOnFadeOut; }
            set { _destroyOnFadeOut = value; }
        }

        public bool FadeInCalled { get; private set; }
        public bool FadeOutCalled { get; private set; }

        public LicenseManagerPanelStub()
        {
            _buttonInteractableStates[CheckUpdatesButtonId] = true;
            _buttonInteractableStates[ActivateButtonId] = true;
            _buttonInteractableStates[DeactivateButtonId] = true;
            _buttonInteractableStates[CloseButtonId] = true;
        }

        public void SetAlpha(float alpha)
        {
            _alpha = alpha;
        }

        public float GetAlpha()
        {
            return _alpha;
        }

        public void SetHittable(bool isHittable)
        {
            _isHittable = isHittable;
        }

        public bool IsHittable()
        {
            return _isHittable;
        }

        public void FadeIn(Action onComplete = null)
        {
            FadeInCalled = true;
            _isHittable = true;
            _alpha = 1f;
            onComplete?.Invoke();
        }

        public void FadeOut(Action onComplete = null)
        {
            FadeOutCalled = true;
            _isHittable = false;
            _alpha = 0f;
            onComplete?.Invoke();
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

        public void SetLicenseKeyText(string text)
        {
            _licenseKeyText = text;
        }

        public string GetLicenseKeyText()
        {
            return _licenseKeyText;
        }

        public void SetExpirationDateText(string text)
        {
            _expirationDateText = text;
        }

        public string GetExpirationDateText()
        {
            return _expirationDateText;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class LicenseManagerPanelTests
    {
        private LicenseManagerPanelStub _panel;

        [SetUp]
        public void SetUp()
        {
            _panel = new LicenseManagerPanelStub();
        }

        [TearDown]
        public void TearDown()
        {
            _panel = null;
        }

        // WHY: Public button ID constants must match expected values since they are
        // used by external code to reference specific buttons by name.
        [Test]
        public void ButtonIdConstants_HaveExpectedValues()
        {
            Assert.AreEqual("check-updates", LicenseManagerPanelStub.CheckUpdatesButtonId,
                "CheckUpdatesButtonId must match the expected string for external lookups.");
            Assert.AreEqual("activate", LicenseManagerPanelStub.ActivateButtonId,
                "ActivateButtonId must match the expected string for external lookups.");
            Assert.AreEqual("deactivate", LicenseManagerPanelStub.DeactivateButtonId,
                "DeactivateButtonId must match the expected string for external lookups.");
            Assert.AreEqual("close", LicenseManagerPanelStub.CloseButtonId,
                "CloseButtonId must match the expected string for external lookups.");
        }

        // WHY: The license key text is displayed to the user and must be retrievable
        // after being set, supporting the activate/deactivate workflow.
        [Test]
        public void SetGetLicenseKeyText_RoundTrips_Correctly()
        {
            // Act
            _panel.SetLicenseKeyText("XXXX-YYYY-ZZZZ-1234");

            // Assert
            Assert.AreEqual("XXXX-YYYY-ZZZZ-1234", _panel.GetLicenseKeyText(),
                "GetLicenseKeyText should return the value set by SetLicenseKeyText.");
        }

        // WHY: The expiration date informs users when their license expires.
        // It must be settable and retrievable to keep the display accurate.
        [Test]
        public void SetGetExpirationDateText_RoundTrips_Correctly()
        {
            // Act
            _panel.SetExpirationDateText("2026-12-31");

            // Assert
            Assert.AreEqual("2026-12-31", _panel.GetExpirationDateText(),
                "GetExpirationDateText should return the value set by SetExpirationDateText.");
        }

        // WHY: Individual buttons can be disabled (e.g., deactivate is only
        // available for active licenses). State lookup must work per-button.
        [Test]
        public void SetButtonInteractable_DisablesSpecificButton()
        {
            // Act
            _panel.SetButtonInteractable(LicenseManagerPanelStub.DeactivateButtonId, false);

            // Assert
            Assert.IsFalse(_panel.IsButtonInteractable(LicenseManagerPanelStub.DeactivateButtonId),
                "Deactivate button should not be interactable after disabling it.");
            Assert.IsTrue(_panel.IsButtonInteractable(LicenseManagerPanelStub.ActivateButtonId),
                "Activate button should remain interactable when only deactivate was disabled.");
        }

        // WHY: Querying an unknown button ID must return false rather than
        // throwing, providing safe fallback behavior for dynamic UI.
        [Test]
        public void IsButtonInteractable_ReturnsFalse_ForUnknownButtonId()
        {
            // Act
            bool result = _panel.IsButtonInteractable("nonexistent-button");

            // Assert
            Assert.IsFalse(result,
                "IsButtonInteractable should return false for an unregistered button ID.");
        }

        // WHY: FadeIn must set the panel to hittable so the user can interact
        // with the license management buttons once the panel is visible.
        [Test]
        public void FadeIn_SetsHittableToTrue()
        {
            // Arrange
            _panel.SetHittable(false);

            // Act
            _panel.FadeIn();

            // Assert
            Assert.IsTrue(_panel.IsHittable(),
                "Panel should be hittable after FadeIn to allow user interaction.");
            Assert.IsTrue(_panel.FadeInCalled,
                "FadeIn should have been invoked.");
        }

        // WHY: FadeOut must block interaction immediately to prevent clicks
        // on a disappearing panel from triggering unintended actions.
        [Test]
        public void FadeOut_SetsHittableToFalse()
        {
            // Arrange
            _panel.FadeIn();

            // Act
            _panel.FadeOut();

            // Assert
            Assert.IsFalse(_panel.IsHittable(),
                "Panel should not be hittable after FadeOut to block stale clicks.");
            Assert.IsTrue(_panel.FadeOutCalled,
                "FadeOut should have been invoked.");
        }

        // WHY: SetAlpha is used to control panel transparency independently
        // of fade animations (e.g., for instant show/hide).
        [Test]
        public void SetAlpha_UpdatesAlphaValue()
        {
            // Act
            _panel.SetAlpha(0.5f);

            // Assert
            Assert.AreEqual(0.5f, _panel.GetAlpha(), 0.001f,
                "Alpha should be updated to the value passed to SetAlpha.");
        }
    }
}
