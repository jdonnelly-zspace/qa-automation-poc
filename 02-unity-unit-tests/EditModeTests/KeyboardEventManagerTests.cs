// =============================================================================
// KeyboardEventManagerTests.cs - Edit Mode Unit Tests for KeyboardEventManager
// =============================================================================
// TARGET CLASS: KeyboardEventManager
//   Real file: Assets/CommonA3/zSpace/Scripts/Utilities/KeyboardEventManager.cs
//
// WHAT IT TESTS:
//   Singleton keyboard shortcut registry that maps (KeyCode, Modifier)
//   combinations to handler delegates. Tests validate registration,
//   removal, modifier-key distinction, enable/disable gating, the ignore
//   list, multi-handler dispatch, and safe removal of non-existent handlers.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/enum marked with the "TODO: DELETE this stub"
//      comment and replace using directives with real namespaces.
//   3. The real KeyboardEventManager hooks into Unity's Input system. The
//      stub here uses SimulateKeyPress() to exercise dispatch logic without
//      the Unity runtime.
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
    [Flags]
    public enum KeyModifier
    {
        None  = 0,
        Ctrl  = 1 << 0,
        Alt   = 1 << 1,
        Shift = 1 << 2
    }

    // TODO: DELETE this stub when integrating into the Unity project — use UnityEngine.KeyCode instead.
    public enum StubKeyCode
    {
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Delete, Escape, Return, Space, Tab,
        F1, F2, F3, F4, F5
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the handler-registry and dispatch portions of the real
    /// KeyboardEventManager, with a SimulateKeyPress helper for testing.
    /// </summary>
    public class KeyboardEventManagerStub
    {
        private readonly Dictionary<(StubKeyCode, KeyModifier), List<Action>> _handlers =
            new Dictionary<(StubKeyCode, KeyModifier), List<Action>>();

        private readonly HashSet<StubKeyCode> _ignoreList = new HashSet<StubKeyCode>();

        private bool _enabled = true;

        public void AddKeyPressEventHandler(StubKeyCode key, Action handler,
            KeyModifier modifier = KeyModifier.None)
        {
            var binding = (key, modifier);
            if (!_handlers.ContainsKey(binding))
            {
                _handlers[binding] = new List<Action>();
            }

            _handlers[binding].Add(handler);
        }

        public void RemoveKeyPressEventHandler(StubKeyCode key, Action handler,
            KeyModifier modifier = KeyModifier.None)
        {
            var binding = (key, modifier);
            if (_handlers.ContainsKey(binding))
            {
                _handlers[binding].Remove(handler);
            }
        }

        public void EnableKeyboardControls(bool enabled)
        {
            _enabled = enabled;
        }

        public void AddKeyToIgnoreList(StubKeyCode key)
        {
            _ignoreList.Add(key);
        }

        public void RemoveKeyFromIgnoreList(StubKeyCode key)
        {
            _ignoreList.Remove(key);
        }

        /// <summary>
        /// Simulates a key press for testing purposes, firing all registered
        /// handlers for the given key + modifier combination.
        /// </summary>
        public void SimulateKeyPress(StubKeyCode key,
            KeyModifier modifier = KeyModifier.None)
        {
            if (!_enabled)
            {
                return;
            }

            if (_ignoreList.Contains(key))
            {
                return;
            }

            var binding = (key, modifier);
            if (_handlers.ContainsKey(binding))
            {
                // Iterate a copy to allow safe modification during dispatch
                foreach (Action handler in _handlers[binding].ToArray())
                {
                    handler.Invoke();
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class KeyboardEventManagerTests
    {
        private KeyboardEventManagerStub _keyManager;

        [SetUp]
        public void SetUp()
        {
            _keyManager = new KeyboardEventManagerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _keyManager = null;
        }

        [Test]
        public void AddHandler_RegistersCorrectly_HandlerFiresOnKeyPress()
        {
            // WHY: Teachers rely on keyboard shortcuts (e.g., Ctrl+Z for undo)
            // during live lessons; failed registration breaks the workflow.

            // Arrange
            bool handlerFired = false;
            _keyManager.AddKeyPressEventHandler(StubKeyCode.Z, () => handlerFired = true,
                KeyModifier.Ctrl);

            // Act
            _keyManager.SimulateKeyPress(StubKeyCode.Z, KeyModifier.Ctrl);

            // Assert
            Assert.IsTrue(handlerFired,
                "Handler should fire when the registered key + modifier is pressed.");
        }

        [Test]
        public void RemoveHandler_Unregisters_HandlerNoLongerFires()
        {
            // WHY: When a tool panel is closed, its shortcuts must be removed to
            // prevent phantom actions that confuse the student.

            // Arrange
            int callCount = 0;
            Action handler = () => callCount++;
            _keyManager.AddKeyPressEventHandler(StubKeyCode.Delete, handler);
            _keyManager.SimulateKeyPress(StubKeyCode.Delete);
            Assert.AreEqual(1, callCount, "Precondition: handler should fire once.");

            // Act
            _keyManager.RemoveKeyPressEventHandler(StubKeyCode.Delete, handler);
            _keyManager.SimulateKeyPress(StubKeyCode.Delete);

            // Assert
            Assert.AreEqual(1, callCount,
                "Handler should not fire after being removed.");
        }

        [Test]
        public void SimulateKeyPress_ModifiersAreDistinct_CtrlAPlusAAreDifferent()
        {
            // WHY: Ctrl+A (select all) vs. bare A (type letter) have completely
            // different semantics; confusing them corrupts user input.

            // Arrange
            bool ctrlAFired = false;
            bool bareAFired = false;
            _keyManager.AddKeyPressEventHandler(StubKeyCode.A, () => ctrlAFired = true,
                KeyModifier.Ctrl);
            _keyManager.AddKeyPressEventHandler(StubKeyCode.A, () => bareAFired = true);

            // Act - press only bare A
            _keyManager.SimulateKeyPress(StubKeyCode.A);

            // Assert
            Assert.IsFalse(ctrlAFired,
                "Ctrl+A handler should NOT fire when only A is pressed.");
            Assert.IsTrue(bareAFired,
                "Bare A handler should fire when A is pressed without modifiers.");
        }

        [Test]
        public void EnableKeyboardControls_False_PreventsAllHandlersFiring()
        {
            // WHY: During modal dialogs (e.g., save-confirmation) keyboard shortcuts
            // must be suppressed to prevent unintended scene modifications.

            // Arrange
            bool handlerFired = false;
            _keyManager.AddKeyPressEventHandler(StubKeyCode.S, () => handlerFired = true,
                KeyModifier.Ctrl);
            _keyManager.EnableKeyboardControls(false);

            // Act
            _keyManager.SimulateKeyPress(StubKeyCode.S, KeyModifier.Ctrl);

            // Assert
            Assert.IsFalse(handlerFired,
                "No handlers should fire when keyboard controls are disabled.");
        }

        [Test]
        public void IgnoreList_BlocksSpecificKeys_OtherKeysStillWork()
        {
            // WHY: When a text field has focus, Escape should close it but Delete
            // should type-delete rather than deleting the selected 3D object.

            // Arrange
            bool deleteFired = false;
            bool escapeFired = false;
            _keyManager.AddKeyPressEventHandler(StubKeyCode.Delete, () => deleteFired = true);
            _keyManager.AddKeyPressEventHandler(StubKeyCode.Escape, () => escapeFired = true);
            _keyManager.AddKeyToIgnoreList(StubKeyCode.Delete);

            // Act
            _keyManager.SimulateKeyPress(StubKeyCode.Delete);
            _keyManager.SimulateKeyPress(StubKeyCode.Escape);

            // Assert
            Assert.IsFalse(deleteFired,
                "Delete handler should be blocked by the ignore list.");
            Assert.IsTrue(escapeFired,
                "Escape handler should still fire because it is not on the ignore list.");
        }

        [Test]
        public void MultipleHandlers_SameKey_AllFire()
        {
            // WHY: Multiple subsystems (e.g., toolbar + undo manager) may
            // independently register for the same shortcut and all must execute.

            // Arrange
            int handler1Count = 0;
            int handler2Count = 0;
            _keyManager.AddKeyPressEventHandler(StubKeyCode.Z, () => handler1Count++,
                KeyModifier.Ctrl);
            _keyManager.AddKeyPressEventHandler(StubKeyCode.Z, () => handler2Count++,
                KeyModifier.Ctrl);

            // Act
            _keyManager.SimulateKeyPress(StubKeyCode.Z, KeyModifier.Ctrl);

            // Assert
            Assert.AreEqual(1, handler1Count,
                "First handler should fire exactly once.");
            Assert.AreEqual(1, handler2Count,
                "Second handler should fire exactly once.");
        }

        [Test]
        public void RemoveHandler_NonExistent_DoesNotThrow()
        {
            // WHY: Defensive teardown code in panels may call Remove without
            // checking if the handler was ever added; this must not crash.

            // Arrange
            Action neverAdded = () => { };

            // Act & Assert - should not throw
            Assert.DoesNotThrow(
                () => _keyManager.RemoveKeyPressEventHandler(StubKeyCode.F5, neverAdded),
                "Removing a handler that was never registered should not throw.");
        }

        [Test]
        public void RemoveKeyFromIgnoreList_RestoresKeyFunctionality()
        {
            // WHY: After a text field loses focus, previously ignored keys must
            // resume normal shortcut behavior.

            // Arrange
            bool deleteFired = false;
            _keyManager.AddKeyPressEventHandler(StubKeyCode.Delete, () => deleteFired = true);
            _keyManager.AddKeyToIgnoreList(StubKeyCode.Delete);
            _keyManager.SimulateKeyPress(StubKeyCode.Delete);
            Assert.IsFalse(deleteFired, "Precondition: Delete should be blocked.");

            // Act
            _keyManager.RemoveKeyFromIgnoreList(StubKeyCode.Delete);
            _keyManager.SimulateKeyPress(StubKeyCode.Delete);

            // Assert
            Assert.IsTrue(deleteFired,
                "Delete handler should fire after key is removed from the ignore list.");
        }
    }
}
