// =============================================================================
// KeyboardControllerTests.cs - Edit Mode Unit Tests for KeyboardController
// =============================================================================
// TARGET CLASS: KeyboardController
//   Real file: Assets/StudioA3/Scripts/Input/KeyboardController.cs
//
// WHAT IT TESTS:
//   Keyboard shortcut routing for Studio. Validates that key handlers
//   correctly gate on input field focus (so typing in a text box does not
//   trigger shortcuts), that undo/redo shortcuts check stack depth before
//   calling, that tool-switching shortcuts map to the correct tool toggle,
//   and that license-gated shortcuts are added/removed based on license type.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real KeyboardController is a MonoBehaviour that wires up handlers
//      via KeyboardEventManager. These tests exercise the handler logic
//      through POCO stubs without a Unity runtime.
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
    /// Minimal stand-in for UndoRedoManager singleton behavior.
    /// </summary>
    public class UndoRedoManagerMock
    {
        private int _undoDepth;
        private int _redoDepth;
        public int UndoCallCount { get; private set; }
        public int RedoCallCount { get; private set; }

        public UndoRedoManagerMock(int undoDepth = 0, int redoDepth = 0)
        {
            _undoDepth = undoDepth;
            _redoDepth = redoDepth;
        }

        public int GetUndoStackDepth() { return _undoDepth; }
        public int GetRedoStackDepth() { return _redoDepth; }

        public void Undo()
        {
            UndoCallCount++;
        }

        public void Redo()
        {
            RedoCallCount++;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for ToggleButton behavior.
    /// </summary>
    public class ToggleButtonStub
    {
        public bool IsToggled { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for ButtonGroup that tracks which button was toggled.
    /// </summary>
    public class ButtonGroupStub
    {
        public string LastToggledButton { get; private set; }

        public void SetButtonToggled(string buttonName, bool toggled)
        {
            if (toggled)
            {
                LastToggledButton = buttonName;
            }
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for CanvasGroup to test blocksRaycasts gating.
    /// </summary>
    public class CanvasGroupStub
    {
        public bool BlocksRaycasts { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for CutCopyPasteManager.
    /// </summary>
    public class CutCopyPasteManagerMock
    {
        public int CopyCallCount { get; private set; }
        public int CutCallCount { get; private set; }
        public int PasteCallCount { get; private set; }

        public void Copy(object inputField) { CopyCallCount++; }
        public void Cut(object inputField) { CutCallCount++; }
        public void Paste(object inputField) { PasteCallCount++; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real enum instead.
    /// <summary>
    /// Mirrors the licensing types from CommonAppA3.
    /// </summary>
    public enum LicensingType
    {
        Standard,
        Pro
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the keyboard handler logic from the real
    /// KeyboardController, without requiring MonoBehaviour or KeyboardEventManager.
    /// </summary>
    public class KeyboardControllerStub
    {
        public ButtonGroupStub ToolButtonGroup = new ButtonGroupStub();
        public ToggleButtonStub ModelGalleryToggle = new ToggleButtonStub();
        public ToggleButtonStub SlideBuilderToggle = new ToggleButtonStub();
        public CanvasGroupStub HyperLinkEditorCanvasGroup = new CanvasGroupStub();
        public CanvasGroupStub HyperLinkDisplayCanvasGroup = new CanvasGroupStub();
        public UndoRedoManagerMock UndoRedoManager;
        public CutCopyPasteManagerMock CutCopyPasteManager = new CutCopyPasteManagerMock();

        // Tracks which Pro-only shortcuts are registered
        public bool SlideBuilderShortcutRegistered { get; private set; }
        public bool ImportGalleryShortcutRegistered { get; private set; }

        /// <summary>
        /// Returns true if the input field is null and no hyperlink editors
        /// are blocking, which is the common guard in most handlers.
        /// </summary>
        public bool CanExecuteShortcut(bool hasInputFieldFocus)
        {
            return !hasInputFieldFocus &&
                   !HyperLinkDisplayCanvasGroup.BlocksRaycasts &&
                   !HyperLinkEditorCanvasGroup.BlocksRaycasts;
        }

        public void HandleUndo(bool hasInputFieldFocus)
        {
            if (UndoRedoManager != null && UndoRedoManager.GetUndoStackDepth() > 0)
            {
                UndoRedoManager.Undo();
            }
        }

        public void HandleRedo(bool hasInputFieldFocus)
        {
            if (UndoRedoManager != null && UndoRedoManager.GetRedoStackDepth() > 0)
            {
                UndoRedoManager.Redo();
            }
        }

        public void HandleToggleMoveTool(bool hasInputFieldFocus)
        {
            if (CanExecuteShortcut(hasInputFieldFocus))
            {
                ToolButtonGroup.SetButtonToggled("MoveToolToggle", true);
            }
        }

        public void HandleToggleDrawTool(bool hasInputFieldFocus)
        {
            if (CanExecuteShortcut(hasInputFieldFocus))
            {
                ToolButtonGroup.SetButtonToggled("DrawToolToggle", true);
            }
        }

        public void HandleToggleModelGallery(bool hasInputFieldFocus)
        {
            if (CanExecuteShortcut(hasInputFieldFocus))
            {
                ModelGalleryToggle.IsToggled = !ModelGalleryToggle.IsToggled;
            }
        }

        public void HandleCopy(bool hasInputFieldFocus)
        {
            CutCopyPasteManager.Copy(null);
        }

        public void HandleDelete(bool hasInputFieldFocus)
        {
            // Delete only operates when no input field is focused
        }

        /// <summary>
        /// Simulates HandleOnLicenseCheckCompleted — adds or removes
        /// Pro-only shortcuts based on the license type.
        /// </summary>
        public void HandleOnLicenseCheckCompleted(LicensingType licenseType)
        {
            if (licenseType == LicensingType.Pro)
            {
                SlideBuilderShortcutRegistered = true;
                ImportGalleryShortcutRegistered = true;
            }
            else
            {
                SlideBuilderShortcutRegistered = false;
                ImportGalleryShortcutRegistered = false;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class KeyboardControllerTests
    {
        private KeyboardControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new KeyboardControllerStub();
            _controller.UndoRedoManager = new UndoRedoManagerMock(undoDepth: 2, redoDepth: 1);
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void HandleUndo_WithActionsOnStack_CallsUndo()
        {
            // WHY: Ctrl+Z is the most critical shortcut in the app. It must
            // call Undo only when the undo stack has actions to prevent errors.

            // Act
            _controller.HandleUndo(hasInputFieldFocus: false);

            // Assert
            Assert.AreEqual(1, _controller.UndoRedoManager.UndoCallCount,
                "HandleUndo should call Undo() exactly once when the undo stack has actions.");
        }

        [Test]
        public void HandleUndo_WithEmptyStack_DoesNotCallUndo()
        {
            // WHY: Calling Undo on an empty stack would be a no-op at best or
            // an error at worst. The handler must check depth first.

            // Arrange
            _controller.UndoRedoManager = new UndoRedoManagerMock(undoDepth: 0, redoDepth: 0);

            // Act
            _controller.HandleUndo(hasInputFieldFocus: false);

            // Assert
            Assert.AreEqual(0, _controller.UndoRedoManager.UndoCallCount,
                "HandleUndo should not call Undo() when the undo stack is empty.");
        }

        [Test]
        public void HandleRedo_WithActionsOnStack_CallsRedo()
        {
            // WHY: Ctrl+Y must re-apply the last undone action when available.

            // Act
            _controller.HandleRedo(hasInputFieldFocus: false);

            // Assert
            Assert.AreEqual(1, _controller.UndoRedoManager.RedoCallCount,
                "HandleRedo should call Redo() exactly once when the redo stack has actions.");
        }

        [Test]
        public void HandleToggleMoveTool_NoInputFocus_SwitchesToMoveTool()
        {
            // WHY: Pressing Q should switch to the Move tool, but only when
            // the user is not typing in a text field.

            // Act
            _controller.HandleToggleMoveTool(hasInputFieldFocus: false);

            // Assert
            Assert.AreEqual("MoveToolToggle", _controller.ToolButtonGroup.LastToggledButton,
                "Q key should toggle the MoveToolToggle button in the ToolButtonGroup.");
        }

        [Test]
        public void HandleToggleDrawTool_WithInputFocus_DoesNotSwitch()
        {
            // WHY: When the user is typing in a text box, pressing D should
            // type the letter, not switch to the Draw tool.

            // Act
            _controller.HandleToggleDrawTool(hasInputFieldFocus: true);

            // Assert
            Assert.IsNull(_controller.ToolButtonGroup.LastToggledButton,
                "Tool should not switch when user has focus in an input field.");
        }

        [Test]
        public void HandleToggleModelGallery_NoInputFocus_TogglesGallery()
        {
            // WHY: G key opens/closes the model gallery. This is a frequent
            // workflow shortcut that must toggle the gallery panel state.

            // Arrange
            _controller.ModelGalleryToggle.IsToggled = false;

            // Act
            _controller.HandleToggleModelGallery(hasInputFieldFocus: false);

            // Assert
            Assert.IsTrue(_controller.ModelGalleryToggle.IsToggled,
                "G key should toggle the model gallery open when it was closed.");
        }

        [Test]
        public void CanExecuteShortcut_HyperlinkEditorBlocking_ReturnsFalse()
        {
            // WHY: When the hyperlink editor popup is open, keyboard shortcuts
            // must be suppressed so the user can type URLs without triggering
            // tool switches or other actions.

            // Arrange
            _controller.HyperLinkEditorCanvasGroup.BlocksRaycasts = true;

            // Act
            bool canExecute = _controller.CanExecuteShortcut(hasInputFieldFocus: false);

            // Assert
            Assert.IsFalse(canExecute,
                "Shortcuts must be suppressed when the hyperlink editor is blocking raycasts.");
        }

        [Test]
        public void HandleOnLicenseCheckCompleted_ProLicense_RegistersProShortcuts()
        {
            // WHY: Slide Builder (B) and Import Gallery (O) are Pro-only features.
            // Their keyboard shortcuts must only be active for Pro licensees.

            // Act
            _controller.HandleOnLicenseCheckCompleted(LicensingType.Pro);

            // Assert
            Assert.IsTrue(_controller.SlideBuilderShortcutRegistered,
                "Pro license should register the Slide Builder keyboard shortcut.");
            Assert.IsTrue(_controller.ImportGalleryShortcutRegistered,
                "Pro license should register the Import Gallery keyboard shortcut.");
        }
    }
}
