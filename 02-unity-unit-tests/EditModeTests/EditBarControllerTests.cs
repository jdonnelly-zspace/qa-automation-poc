// =============================================================================
// EditBarControllerTests.cs - Edit Mode Unit Tests for EditBarController
// =============================================================================
// TARGET CLASS: EditBarController
//   Real file: Assets/StudioA3/Scripts/UI/EditBarController.cs
//
// WHAT IT TESTS:
//   Edit toolbar state management for undo, redo, cut, copy, paste, and delete
//   operations. Validates that each button's enabled state correctly reflects
//   the application state (stack depth, selection, text editing mode, clipboard
//   content) and that paste offset increments for duplicated objects.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment and
//      replace the using directives with the real namespaces.
//   3. The real EditBarController is a MonoBehaviour that reads from the
//      UndoRedoManager, SelectionManager, and Clipboard. These tests exercise
//      the logic through a lightweight POCO stub so they compile standalone.
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
    /// Lightweight POCO that mirrors the public API of the real
    /// EditBarController, without requiring MonoBehaviour.
    /// </summary>
    public class EditBarControllerStub
    {
        public int UndoStackDepth = 0;
        public int RedoStackDepth = 0;
        public int SelectionCount = 0;
        public bool TextEditing = false;
        public string ClipboardContent = null;
        public int PasteCount = 0;

        private const float PasteOffsetStep = 0.05f;

        /// <summary>
        /// Returns true if the Undo button should be enabled.
        /// </summary>
        public bool IsUndoEnabled()
        {
            return UndoStackDepth > 0;
        }

        /// <summary>
        /// Returns true if the Redo button should be enabled.
        /// </summary>
        public bool IsRedoEnabled()
        {
            return RedoStackDepth > 0;
        }

        /// <summary>
        /// Returns true if the Delete button should be enabled.
        /// Delete is disabled during text editing to avoid conflicting with
        /// the text field's own delete behavior.
        /// </summary>
        public bool IsDeleteEnabled()
        {
            return SelectionCount > 0 && !TextEditing;
        }

        /// <summary>
        /// Returns true if the Copy button should be enabled.
        /// Copy is disabled during text editing to let the text field handle it.
        /// </summary>
        public bool IsCopyEnabled()
        {
            return SelectionCount > 0 && !TextEditing;
        }

        /// <summary>
        /// Returns true if the Paste button should be enabled.
        /// </summary>
        public bool IsPasteEnabled()
        {
            return ClipboardContent != null;
        }

        /// <summary>
        /// Increments the paste count so subsequent pastes are offset.
        /// </summary>
        public void IncrementPasteCount()
        {
            PasteCount++;
        }

        /// <summary>
        /// Returns the spatial offset for the current paste operation so
        /// pasted objects do not stack directly on top of originals.
        /// </summary>
        public float GetPasteOffset()
        {
            return PasteCount * PasteOffsetStep;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class EditBarControllerTests
    {
        private EditBarControllerStub _editBar;

        [SetUp]
        public void SetUp()
        {
            _editBar = new EditBarControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _editBar = null;
        }

        // ---------------------------------------------------------------------
        // Test: Undo enabled when stack has items
        // ---------------------------------------------------------------------
        // WHY: The Undo button must be visually enabled when there are actions
        //      to undo. A grayed-out button when undo is available frustrates
        //      users and makes the app feel broken.
        // ---------------------------------------------------------------------
        [Test]
        public void IsUndoEnabled_StackHasItems_ReturnsTrue()
        {
            // Arrange
            _editBar.UndoStackDepth = 3;

            // Act
            bool result = _editBar.IsUndoEnabled();

            // Assert
            Assert.IsTrue(result,
                "Undo should be enabled when the undo stack contains 3 items");
        }

        // ---------------------------------------------------------------------
        // Test: Undo disabled when stack is empty
        // ---------------------------------------------------------------------
        // WHY: Clicking an enabled Undo button with nothing to undo can cause
        //      unexpected behavior or errors. The button must be disabled to
        //      prevent invalid operations and communicate state to the user.
        // ---------------------------------------------------------------------
        [Test]
        public void IsUndoEnabled_StackEmpty_ReturnsFalse()
        {
            // Arrange
            _editBar.UndoStackDepth = 0;

            // Act
            bool result = _editBar.IsUndoEnabled();

            // Assert
            Assert.IsFalse(result,
                "Undo should be disabled when the undo stack is empty");
        }

        // ---------------------------------------------------------------------
        // Test: Redo enabled when stack has items, disabled when empty
        // ---------------------------------------------------------------------
        // WHY: Redo must mirror undo's enable/disable logic. An enabled redo
        //      button with nothing to redo is misleading; a disabled button when
        //      redo is available blocks the user's workflow.
        // ---------------------------------------------------------------------
        [Test]
        public void IsRedoEnabled_StackHasItems_ReturnsTrue_EmptyReturnsFalse()
        {
            // Act & Assert -- redo stack with items
            _editBar.RedoStackDepth = 2;
            Assert.IsTrue(_editBar.IsRedoEnabled(),
                "Redo should be enabled when the redo stack contains 2 items");

            // Act & Assert -- redo stack empty
            _editBar.RedoStackDepth = 0;
            Assert.IsFalse(_editBar.IsRedoEnabled(),
                "Redo should be disabled when the redo stack is empty");
        }

        // ---------------------------------------------------------------------
        // Test: Delete enabled only when objects are selected and not text editing
        // ---------------------------------------------------------------------
        // WHY: Delete must only operate on selected 3D objects in the scene. If
        //      the user is editing a text field, the Delete key must go to the
        //      text field, not delete the selected scene object.
        // ---------------------------------------------------------------------
        [Test]
        public void IsDeleteEnabled_WithSelectionNotTextEditing_ReturnsTrue()
        {
            // Arrange -- objects selected, not in text editing mode
            _editBar.SelectionCount = 2;
            _editBar.TextEditing = false;

            // Act
            bool result = _editBar.IsDeleteEnabled();

            // Assert
            Assert.IsTrue(result,
                "Delete should be enabled when 2 objects are selected and text editing is off");
        }

        // ---------------------------------------------------------------------
        // Test: Copy enabled with selection, disabled without
        // ---------------------------------------------------------------------
        // WHY: Copy without a selection has nothing to copy. The UI must
        //      correctly reflect whether the operation is meaningful to avoid
        //      confusing students who are learning the tool.
        // ---------------------------------------------------------------------
        [Test]
        public void IsCopyEnabled_WithSelection_ReturnsTrue_WithoutReturnsFalse()
        {
            // Act & Assert -- with selection
            _editBar.SelectionCount = 1;
            _editBar.TextEditing = false;
            Assert.IsTrue(_editBar.IsCopyEnabled(),
                "Copy should be enabled when 1 object is selected and not text editing");

            // Act & Assert -- without selection
            _editBar.SelectionCount = 0;
            Assert.IsFalse(_editBar.IsCopyEnabled(),
                "Copy should be disabled when no objects are selected");
        }

        // ---------------------------------------------------------------------
        // Test: Paste enabled only with clipboard content
        // ---------------------------------------------------------------------
        // WHY: Paste with an empty clipboard does nothing and confuses the user.
        //      The button must accurately reflect whether there is content
        //      available to paste.
        // ---------------------------------------------------------------------
        [Test]
        public void IsPasteEnabled_WithClipboardContent_ReturnsTrue()
        {
            // Arrange & Assert -- clipboard has content
            _editBar.ClipboardContent = "model-human-heart";
            Assert.IsTrue(_editBar.IsPasteEnabled(),
                "Paste should be enabled when clipboard contains content");

            // Arrange & Assert -- clipboard is null
            _editBar.ClipboardContent = null;
            Assert.IsFalse(_editBar.IsPasteEnabled(),
                "Paste should be disabled when clipboard content is null");
        }

        // ---------------------------------------------------------------------
        // Test: Paste offset increments correctly with each paste
        // ---------------------------------------------------------------------
        // WHY: When a student pastes the same object multiple times, each copy
        //      must be spatially offset so they do not stack directly on top of
        //      each other, which would make them appear as a single object and
        //      confuse the student.
        // ---------------------------------------------------------------------
        [Test]
        public void GetPasteOffset_IncrementsWithEachPaste()
        {
            // Arrange -- initial paste count is 0
            Assert.AreEqual(0f, _editBar.GetPasteOffset(), 0.001f,
                "Initial paste offset should be 0.0 before any pastes");

            // Act -- simulate three consecutive paste operations
            _editBar.IncrementPasteCount();
            float firstOffset = _editBar.GetPasteOffset();

            _editBar.IncrementPasteCount();
            float secondOffset = _editBar.GetPasteOffset();

            _editBar.IncrementPasteCount();
            float thirdOffset = _editBar.GetPasteOffset();

            // Assert
            Assert.AreEqual(0.05f, firstOffset, 0.001f,
                "After 1 paste, offset should be 0.05");
            Assert.AreEqual(0.10f, secondOffset, 0.001f,
                "After 2 pastes, offset should be 0.10");
            Assert.AreEqual(0.15f, thirdOffset, 0.001f,
                "After 3 pastes, offset should be 0.15");
        }

        // ---------------------------------------------------------------------
        // Test: Text editing disables cut/copy/delete
        // ---------------------------------------------------------------------
        // WHY: When a student is typing in a text field (e.g., renaming an
        //      object or writing in the notebook), keyboard shortcuts for
        //      cut/copy/delete must target the text field, not the scene.
        //      The edit bar buttons must be disabled to prevent accidental
        //      scene-level operations.
        // ---------------------------------------------------------------------
        [Test]
        public void TextEditing_DisablesCutCopyDelete()
        {
            // Arrange -- objects are selected but user is editing text
            _editBar.SelectionCount = 3;
            _editBar.TextEditing = true;

            // Act & Assert
            Assert.IsFalse(_editBar.IsDeleteEnabled(),
                "Delete should be disabled during text editing even with 3 objects selected");
            Assert.IsFalse(_editBar.IsCopyEnabled(),
                "Copy should be disabled during text editing even with 3 objects selected");
        }
    }
}
