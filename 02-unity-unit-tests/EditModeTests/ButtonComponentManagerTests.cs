// =============================================================================
// ButtonComponentManagerTests.cs - Edit Mode Unit Tests for ButtonComponentManager
// =============================================================================
// TARGET CLASS: ButtonComponentManager
//   Real file: Assets/CommonA3/zSpace/licensing/Modernization/UI/Scripts/ButtonComponentManager.cs
//
// WHAT IT TESTS:
//   ButtonComponentManager aggregates UI sub-components (Button, ButtonToggle,
//   ButtonMenu, ButtonIcon, ButtonText, PointerEventListener) and manages
//   interactable and hittable state. Tests verify SetInteractable/IsInteractable,
//   SetHittable/IsHittable, and property accessors.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real ButtonComponentManager is a MonoBehaviour that aggregates
//      Unity UI components. These tests use POCO stubs to validate the
//      interactable/hittable state logic without Unity runtime.
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
    /// Stub for ButtonToggle component with enabled state.
    /// </summary>
    public class ButtonToggleStub
    {
        public bool Enabled { get; set; } = true;
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Stub for ButtonMenu component with enabled and menu indicator visibility.
    /// </summary>
    public class ButtonMenuStub
    {
        public bool Enabled { get; set; } = true;
        public bool MenuIndicatorVisible { get; set; } = true;

        public void SetMenuIndicatorVisible(bool visible)
        {
            MenuIndicatorVisible = visible;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Stub for ButtonIcon component with enabled state.
    /// </summary>
    public class ButtonIconStub
    {
        public bool Enabled { get; set; } = true;
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Stub for ButtonText component with enabled state and color refresh tracking.
    /// </summary>
    public class ButtonTextStub
    {
        public bool Enabled { get; set; } = true;
        public int RefreshColorCallCount { get; private set; }

        public void RefreshColor(float value)
        {
            RefreshColorCallCount++;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Stub for PointerEventListener with interactable state.
    /// </summary>
    public class PointerEventListenerStub
    {
        public bool IsInteractableValue { get; private set; } = true;

        public void SetInteractable(bool isInteractable)
        {
            IsInteractableValue = isInteractable;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API of ButtonComponentManager.
    /// Tracks interactable and hittable state, and exposes sub-component accessors.
    /// </summary>
    public class ButtonComponentManagerStub
    {
        private bool _isInteractable = true;
        private bool _isHittable = true;

        public ButtonToggleStub ButtonToggle { get; set; }
        public ButtonMenuStub ButtonMenu { get; set; }
        public ButtonIconStub ButtonIcon { get; set; }
        public ButtonTextStub ButtonText { get; set; }
        public PointerEventListenerStub PointerEventListener { get; set; }

        public void SetHittable(bool isHittable)
        {
            _isHittable = isHittable;
        }

        public bool IsHittable()
        {
            return _isHittable;
        }

        public void SetInteractable(bool isInteractable)
        {
            _isInteractable = isInteractable;

            if (this.ButtonToggle != null)
            {
                this.ButtonToggle.Enabled = isInteractable;
            }

            if (this.ButtonMenu != null)
            {
                this.ButtonMenu.Enabled = isInteractable;
                this.ButtonMenu.SetMenuIndicatorVisible(isInteractable);
            }

            if (this.ButtonIcon != null)
            {
                this.ButtonIcon.Enabled = isInteractable;
            }

            if (this.ButtonText != null)
            {
                this.ButtonText.Enabled = isInteractable;
                this.ButtonText.RefreshColor(0.0f);
            }

            if (this.PointerEventListener != null)
            {
                this.PointerEventListener.SetInteractable(isInteractable);
            }
        }

        public bool IsInteractable()
        {
            return _isInteractable;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ButtonComponentManagerTests
    {
        private ButtonComponentManagerStub _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new ButtonComponentManagerStub
            {
                ButtonToggle = new ButtonToggleStub(),
                ButtonMenu = new ButtonMenuStub(),
                ButtonIcon = new ButtonIconStub(),
                ButtonText = new ButtonTextStub(),
                PointerEventListener = new PointerEventListenerStub()
            };
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
        }

        // WHY: SetInteractable(false) must disable all sub-components so the
        // button is completely non-interactive, preventing ghost clicks.
        [Test]
        public void SetInteractable_DisablesAllSubComponents_WhenSetToFalse()
        {
            // Act
            _manager.SetInteractable(false);

            // Assert
            Assert.IsFalse(_manager.IsInteractable(),
                "IsInteractable should return false after SetInteractable(false).");
            Assert.IsFalse(_manager.ButtonToggle.Enabled,
                "ButtonToggle should be disabled when interactable is false.");
            Assert.IsFalse(_manager.ButtonMenu.Enabled,
                "ButtonMenu should be disabled when interactable is false.");
            Assert.IsFalse(_manager.ButtonIcon.Enabled,
                "ButtonIcon should be disabled when interactable is false.");
            Assert.IsFalse(_manager.ButtonText.Enabled,
                "ButtonText should be disabled when interactable is false.");
        }

        // WHY: Re-enabling interactable must restore all sub-components so the
        // button returns to full functionality.
        [Test]
        public void SetInteractable_EnablesAllSubComponents_WhenSetToTrue()
        {
            // Arrange - first disable
            _manager.SetInteractable(false);

            // Act
            _manager.SetInteractable(true);

            // Assert
            Assert.IsTrue(_manager.IsInteractable(),
                "IsInteractable should return true after SetInteractable(true).");
            Assert.IsTrue(_manager.ButtonToggle.Enabled,
                "ButtonToggle should be enabled when interactable is true.");
            Assert.IsTrue(_manager.ButtonMenu.Enabled,
                "ButtonMenu should be enabled when interactable is true.");
        }

        // WHY: The menu indicator must match interactable state so users get
        // visual feedback that the dropdown is available or unavailable.
        [Test]
        public void SetInteractable_UpdatesMenuIndicatorVisibility()
        {
            // Act
            _manager.SetInteractable(false);

            // Assert
            Assert.IsFalse(_manager.ButtonMenu.MenuIndicatorVisible,
                "Menu indicator should be hidden when button is not interactable.");

            // Act
            _manager.SetInteractable(true);

            // Assert
            Assert.IsTrue(_manager.ButtonMenu.MenuIndicatorVisible,
                "Menu indicator should be visible when button is interactable.");
        }

        // WHY: ButtonText.RefreshColor must be called immediately on interactable
        // change to prevent stale color display until the next mouse hover.
        [Test]
        public void SetInteractable_RefreshesButtonTextColor()
        {
            // Act
            _manager.SetInteractable(false);

            // Assert
            Assert.AreEqual(1, _manager.ButtonText.RefreshColorCallCount,
                "RefreshColor should be called once when SetInteractable is invoked.");
        }

        // WHY: PointerEventListener must track interactable state so pointer
        // events are correctly blocked or allowed.
        [Test]
        public void SetInteractable_UpdatesPointerEventListener()
        {
            // Act
            _manager.SetInteractable(false);

            // Assert
            Assert.IsFalse(_manager.PointerEventListener.IsInteractableValue,
                "PointerEventListener should not be interactable when button is disabled.");
        }

        // WHY: Hittable state controls raycast targeting. A non-hittable button
        // lets pointer events pass through, which is needed for overlapping UI.
        [Test]
        public void SetHittable_ControlsRaycastTargeting()
        {
            // Initially hittable
            Assert.IsTrue(_manager.IsHittable(),
                "Button should be hittable by default.");

            // Act
            _manager.SetHittable(false);

            // Assert
            Assert.IsFalse(_manager.IsHittable(),
                "Button should not be hittable after SetHittable(false).");
        }

        // WHY: Null sub-components are valid (e.g., a button without a toggle).
        // SetInteractable must not throw when optional components are null.
        [Test]
        public void SetInteractable_HandlesNullSubComponents_WithoutThrowing()
        {
            // Arrange - create manager with no sub-components
            var sparseManager = new ButtonComponentManagerStub();

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => sparseManager.SetInteractable(false),
                "SetInteractable should handle null sub-components gracefully.");
            Assert.IsFalse(sparseManager.IsInteractable(),
                "IsInteractable should still track state even with null sub-components.");
        }

        // WHY: Hittable and interactable are independent concerns. Changing one
        // must not affect the other.
        [Test]
        public void SetHittable_IsIndependentOfInteractable()
        {
            // Act
            _manager.SetInteractable(false);
            _manager.SetHittable(true);

            // Assert
            Assert.IsTrue(_manager.IsHittable(),
                "Hittable state should be independent of interactable state.");
            Assert.IsFalse(_manager.IsInteractable(),
                "Interactable state should be independent of hittable state.");
        }
    }
}
