// =============================================================================
// EditToolbarControllerTests.cs - Edit Mode Unit Tests for EditToolbarController
// =============================================================================
// TARGET CLASS: EditToolbarController
//   Real file: Assets/VivedUpgrades/Toolbar/Scripts/EditToolbarController.cs
//
// WHAT IT TESTS:
//   Edit toolbar that wires Undo, Redo, Paste, and Clear All buttons to their
//   respective managers. Validates button interactability based on undo/redo
//   stack depth, correct delegation to UndoRedoManager and CutCopyPasteManager,
//   and the Clear All workflow that removes all scene objects.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real EditToolbarController is a MonoBehaviour that depends on
//      UndoRedoManager, SelectionManager, CutCopyPasteManager singletons
//      and a ButtonGroup. These tests exercise the routing logic through
//      lightweight POCO stubs so they compile standalone.
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
    /// Minimal stand-in for the ButtonGroup button, tracking name and
    /// interactability state.
    /// </summary>
    public class ToolbarButtonStub
    {
        public string Name { get; private set; }
        public bool IsInteractable { get; set; }

        public ToolbarButtonStub(string name)
        {
            Name = name;
            IsInteractable = true;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for the ButtonGroup that holds named buttons.
    /// </summary>
    public class ButtonGroupStub
    {
        private Dictionary<string, ToolbarButtonStub> _buttons =
            new Dictionary<string, ToolbarButtonStub>();

        public void AddButton(string name)
        {
            _buttons[name] = new ToolbarButtonStub(name);
        }

        public ToolbarButtonStub GetButton(string name)
        {
            return _buttons.ContainsKey(name) ? _buttons[name] : null;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal UndoRedoManager stand-in for tracking undo/redo stack depth
    /// and recording whether Undo()/Redo() were called.
    /// </summary>
    public class UndoRedoManagerStubForToolbar
    {
        public int UndoStackDepth { get; set; }
        public int RedoStackDepth { get; set; }
        public int UndoCallCount { get; private set; }
        public int RedoCallCount { get; private set; }

        public int GetUndoStackDepth() { return UndoStackDepth; }
        public int GetRedoStackDepth() { return RedoStackDepth; }

        public void Undo()
        {
            if (UndoStackDepth > 0)
            {
                UndoStackDepth--;
                RedoStackDepth++;
                UndoCallCount++;
            }
        }

        public void Redo()
        {
            if (RedoStackDepth > 0)
            {
                RedoStackDepth--;
                UndoStackDepth++;
                RedoCallCount++;
            }
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal CutCopyPasteManager stand-in that records Paste calls.
    /// </summary>
    public class CutCopyPasteManagerStub
    {
        public int PasteCallCount { get; private set; }

        public void Paste()
        {
            PasteCallCount++;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the toolbar routing logic of the real
    /// EditToolbarController, without requiring MonoBehaviour or singletons.
    /// </summary>
    public class EditToolbarControllerStub
    {
        private const string UndoButtonName = "UndoButton";
        private const string RedoButtonName = "RedoButton";
        private const string PasteButtonName = "PasteButton";
        private const string ClearAllButtonName = "ClearAllButton";

        private ButtonGroupStub _buttonGroup;
        private UndoRedoManagerStubForToolbar _undoRedoManager;
        private CutCopyPasteManagerStub _pasteManager;
        private bool _clearAllInvoked;

        public bool ClearAllInvoked { get { return _clearAllInvoked; } }

        public EditToolbarControllerStub(
            ButtonGroupStub buttonGroup,
            UndoRedoManagerStubForToolbar undoRedoManager,
            CutCopyPasteManagerStub pasteManager)
        {
            _buttonGroup = buttonGroup;
            _undoRedoManager = undoRedoManager;
            _pasteManager = pasteManager;
            _clearAllInvoked = false;
        }

        /// <summary>
        /// Mirrors HandleOnStackChanged — updates undo/redo button interactability.
        /// </summary>
        public void HandleOnStackChanged()
        {
            _buttonGroup.GetButton(UndoButtonName).IsInteractable =
                _undoRedoManager.GetUndoStackDepth() > 0;
            _buttonGroup.GetButton(RedoButtonName).IsInteractable =
                _undoRedoManager.GetRedoStackDepth() > 0;
        }

        /// <summary>
        /// Mirrors HandleOnButtonClicked — routes button clicks to the correct handler.
        /// </summary>
        public void HandleOnButtonClicked(string buttonName)
        {
            switch (buttonName)
            {
                case UndoButtonName:
                    HandleOnUndoButtonClicked();
                    break;
                case RedoButtonName:
                    HandleOnRedoButtonClicked();
                    break;
                case PasteButtonName:
                    HandleOnPasteButtonClicked();
                    break;
                case ClearAllButtonName:
                    HandleOnClearAllButtonClicked();
                    break;
            }
        }

        private void HandleOnUndoButtonClicked()
        {
            if (_undoRedoManager.GetUndoStackDepth() > 0)
            {
                _undoRedoManager.Undo();
            }
        }

        private void HandleOnRedoButtonClicked()
        {
            if (_undoRedoManager.GetRedoStackDepth() > 0)
            {
                _undoRedoManager.Redo();
            }
        }

        private void HandleOnPasteButtonClicked()
        {
            _pasteManager.Paste();
        }

        private void HandleOnClearAllButtonClicked()
        {
            _clearAllInvoked = true;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class EditToolbarControllerTests
    {
        private ButtonGroupStub _buttonGroup;
        private UndoRedoManagerStubForToolbar _undoRedoManager;
        private CutCopyPasteManagerStub _pasteManager;
        private EditToolbarControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _buttonGroup = new ButtonGroupStub();
            _buttonGroup.AddButton("UndoButton");
            _buttonGroup.AddButton("RedoButton");
            _buttonGroup.AddButton("PasteButton");
            _buttonGroup.AddButton("ClearAllButton");

            _undoRedoManager = new UndoRedoManagerStubForToolbar();
            _pasteManager = new CutCopyPasteManagerStub();
            _controller = new EditToolbarControllerStub(
                _buttonGroup, _undoRedoManager, _pasteManager);
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
            _buttonGroup = null;
            _undoRedoManager = null;
            _pasteManager = null;
        }

        [Test]
        public void HandleOnStackChanged_DisablesUndo_WhenUndoStackEmpty()
        {
            // WHY: Undo button must be grayed out when there is nothing to undo,
            //      preventing user confusion and accidental no-op clicks.

            // Arrange
            _undoRedoManager.UndoStackDepth = 0;
            _undoRedoManager.RedoStackDepth = 2;

            // Act
            _controller.HandleOnStackChanged();

            // Assert
            Assert.IsFalse(_buttonGroup.GetButton("UndoButton").IsInteractable,
                "Undo button should be disabled when undo stack is empty.");
            Assert.IsTrue(_buttonGroup.GetButton("RedoButton").IsInteractable,
                "Redo button should be enabled when redo stack has entries.");
        }

        [Test]
        public void HandleOnStackChanged_DisablesRedo_WhenRedoStackEmpty()
        {
            // WHY: Redo button must be grayed out when there is nothing to redo,
            //      keeping toolbar state consistent with available operations.

            // Arrange
            _undoRedoManager.UndoStackDepth = 3;
            _undoRedoManager.RedoStackDepth = 0;

            // Act
            _controller.HandleOnStackChanged();

            // Assert
            Assert.IsTrue(_buttonGroup.GetButton("UndoButton").IsInteractable,
                "Undo button should be enabled when undo stack has entries.");
            Assert.IsFalse(_buttonGroup.GetButton("RedoButton").IsInteractable,
                "Redo button should be disabled when redo stack is empty.");
        }

        [Test]
        public void HandleOnStackChanged_EnablesBoth_WhenBothStacksNonEmpty()
        {
            // WHY: When both undo and redo are available, both buttons must be
            //      clickable so users can navigate freely through their edit history.

            // Arrange
            _undoRedoManager.UndoStackDepth = 1;
            _undoRedoManager.RedoStackDepth = 1;

            // Act
            _controller.HandleOnStackChanged();

            // Assert
            Assert.IsTrue(_buttonGroup.GetButton("UndoButton").IsInteractable,
                "Undo button should be enabled when undo stack is non-empty.");
            Assert.IsTrue(_buttonGroup.GetButton("RedoButton").IsInteractable,
                "Redo button should be enabled when redo stack is non-empty.");
        }

        [Test]
        public void UndoButtonClick_CallsUndo_WhenStackNonEmpty()
        {
            // WHY: Clicking the undo button must delegate to UndoRedoManager.Undo()
            //      so the user's last action is reversed.

            // Arrange
            _undoRedoManager.UndoStackDepth = 2;

            // Act
            _controller.HandleOnButtonClicked("UndoButton");

            // Assert
            Assert.AreEqual(1, _undoRedoManager.UndoCallCount,
                "UndoRedoManager.Undo() should be called once when undo button is clicked.");
            Assert.AreEqual(1, _undoRedoManager.UndoStackDepth,
                "Undo stack depth should decrease by 1 after undo.");
        }

        [Test]
        public void UndoButtonClick_DoesNothing_WhenStackEmpty()
        {
            // WHY: Guard clause prevents calling Undo() on an empty stack,
            //      which would be a no-op but could mask bugs in stack tracking.

            // Arrange
            _undoRedoManager.UndoStackDepth = 0;

            // Act
            _controller.HandleOnButtonClicked("UndoButton");

            // Assert
            Assert.AreEqual(0, _undoRedoManager.UndoCallCount,
                "Undo() should not be called when undo stack is empty.");
        }

        [Test]
        public void RedoButtonClick_CallsRedo_WhenStackNonEmpty()
        {
            // WHY: Clicking the redo button must delegate to UndoRedoManager.Redo()
            //      so a previously undone action is re-applied.

            // Arrange
            _undoRedoManager.RedoStackDepth = 1;

            // Act
            _controller.HandleOnButtonClicked("RedoButton");

            // Assert
            Assert.AreEqual(1, _undoRedoManager.RedoCallCount,
                "UndoRedoManager.Redo() should be called once when redo button is clicked.");
        }

        [Test]
        public void PasteButtonClick_DelegatesToPasteManager()
        {
            // WHY: The paste button must route through CutCopyPasteManager so
            //      clipboard content is correctly placed into the scene or text field.

            // Act
            _controller.HandleOnButtonClicked("PasteButton");

            // Assert
            Assert.AreEqual(1, _pasteManager.PasteCallCount,
                "CutCopyPasteManager.Paste() should be called once when paste button is clicked.");
        }

        [Test]
        public void ClearAllButtonClick_InvokesClearAllWorkflow()
        {
            // WHY: Clear All removes every scene object. This is a destructive
            //      operation and the button click must reliably trigger the workflow.

            // Arrange
            Assert.IsFalse(_controller.ClearAllInvoked,
                "ClearAll should not be invoked before the button is clicked.");

            // Act
            _controller.HandleOnButtonClicked("ClearAllButton");

            // Assert
            Assert.IsTrue(_controller.ClearAllInvoked,
                "ClearAll workflow should be invoked when clear all button is clicked.");
        }
    }
}
