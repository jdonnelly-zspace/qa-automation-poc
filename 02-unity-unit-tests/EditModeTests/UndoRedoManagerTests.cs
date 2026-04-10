// =============================================================================
// UndoRedoManagerTests.cs - Edit Mode Unit Tests for UndoRedoManager
// =============================================================================
// TARGET CLASS: UndoRedoManager
//   Real file: Assets/zSpace/StudioA3/Scripts/UndoRedo/UndoRedoManager.cs
//
// WHAT IT TESTS:
//   Stack-based undo/redo system used throughout Studio A3. Validates that
//   pushing actions, undoing, redoing, clearing, dirty tracking, and nested
//   stack save/restore all behave correctly.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real UndoRedoManager is a singleton MonoBehaviour. These tests
//      exercise the logic through a lightweight POCO stub so they compile
//      standalone in the POC without a Unity runtime.
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
    /// Minimal stand-in for the real IUndoRedoAction interface.
    /// </summary>
    public interface IUndoRedoAction
    {
        void Undo();
        void Redo();
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Simple action that records whether Undo/Redo was called, useful for
    /// assertions in tests.
    /// </summary>
    public class MockUndoRedoAction : IUndoRedoAction
    {
        public int UndoCallCount { get; private set; }
        public int RedoCallCount { get; private set; }
        public string Label { get; }

        public MockUndoRedoAction(string label = "")
        {
            Label = label;
        }

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
    /// Lightweight POCO that mirrors the public API of the real
    /// UndoRedoManager singleton, without requiring MonoBehaviour.
    /// </summary>
    public class UndoRedoManagerStub
    {
        private Stack<IUndoRedoAction> _undoStack = new Stack<IUndoRedoAction>();
        private Stack<IUndoRedoAction> _redoStack = new Stack<IUndoRedoAction>();
        private Stack<(Stack<IUndoRedoAction> undo, Stack<IUndoRedoAction> redo)> _savedStacks =
            new Stack<(Stack<IUndoRedoAction>, Stack<IUndoRedoAction>)>();
        private bool _isDirty;

        public bool IsDirty
        {
            get { return _isDirty; }
        }

        public void Push(IUndoRedoAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _undoStack.Push(action);
            _redoStack.Clear();
            _isDirty = true;
        }

        public void Undo()
        {
            if (_undoStack.Count == 0)
            {
                return;
            }

            IUndoRedoAction action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);
        }

        public void Redo()
        {
            if (_redoStack.Count == 0)
            {
                return;
            }

            IUndoRedoAction action = _redoStack.Pop();
            action.Redo();
            _undoStack.Push(action);
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _isDirty = false;
        }

        public void ClearDirty()
        {
            _isDirty = false;
        }

        public void PushStacks()
        {
            _savedStacks.Push((
                new Stack<IUndoRedoAction>(new Stack<IUndoRedoAction>(_undoStack)),
                new Stack<IUndoRedoAction>(new Stack<IUndoRedoAction>(_redoStack))
            ));
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public void PopStacks()
        {
            if (_savedStacks.Count == 0)
            {
                return;
            }

            var saved = _savedStacks.Pop();
            _undoStack = saved.undo;
            _redoStack = saved.redo;
        }

        public int GetUndoStackDepth()
        {
            return _undoStack.Count;
        }

        public int GetRedoStackDepth()
        {
            return _redoStack.Count;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class UndoRedoManagerTests
    {
        private UndoRedoManagerStub _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new UndoRedoManagerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
        }

        [Test]
        public void Push_AddsToUndoStack_ClearsRedoStack()
        {
            // Arrange
            var action1 = new MockUndoRedoAction("action1");
            var action2 = new MockUndoRedoAction("action2");

            // Push two, undo one so redo has an entry
            _manager.Push(action1);
            _manager.Push(action2);
            _manager.Undo();
            Assert.AreEqual(1, _manager.GetRedoStackDepth(),
                "Redo stack should have one entry after undo.");

            // Act - push a new action; redo stack must be cleared
            var action3 = new MockUndoRedoAction("action3");
            _manager.Push(action3);

            // Assert
            Assert.AreEqual(2, _manager.GetUndoStackDepth(),
                "Undo stack should contain original + newly pushed action.");
            Assert.AreEqual(0, _manager.GetRedoStackDepth(),
                "Redo stack must be cleared after a new push.");
        }

        [Test]
        public void Undo_MovesActionToRedoStack()
        {
            // Arrange
            var action = new MockUndoRedoAction("moveable");
            _manager.Push(action);

            // Act
            _manager.Undo();

            // Assert
            Assert.AreEqual(0, _manager.GetUndoStackDepth(),
                "Undo stack should be empty after undoing the only action.");
            Assert.AreEqual(1, _manager.GetRedoStackDepth(),
                "Redo stack should contain the undone action.");
            Assert.AreEqual(1, action.UndoCallCount,
                "Action.Undo() should have been called exactly once.");
        }

        [Test]
        public void Redo_MovesActionToUndoStack()
        {
            // Arrange
            var action = new MockUndoRedoAction("redoable");
            _manager.Push(action);
            _manager.Undo();

            // Act
            _manager.Redo();

            // Assert
            Assert.AreEqual(1, _manager.GetUndoStackDepth(),
                "Undo stack should contain the re-done action.");
            Assert.AreEqual(0, _manager.GetRedoStackDepth(),
                "Redo stack should be empty after redo.");
            Assert.AreEqual(1, action.RedoCallCount,
                "Action.Redo() should have been called exactly once.");
        }

        [Test]
        public void Clear_EmptiesBothStacks()
        {
            // Arrange
            _manager.Push(new MockUndoRedoAction("a"));
            _manager.Push(new MockUndoRedoAction("b"));
            _manager.Undo();

            // Act
            _manager.Clear();

            // Assert
            Assert.AreEqual(0, _manager.GetUndoStackDepth(),
                "Undo stack should be empty after Clear().");
            Assert.AreEqual(0, _manager.GetRedoStackDepth(),
                "Redo stack should be empty after Clear().");
            Assert.IsFalse(_manager.IsDirty,
                "IsDirty should be false after Clear().");
        }

        [Test]
        public void IsDirty_TracksPushAndClearDirty()
        {
            // Initially clean
            Assert.IsFalse(_manager.IsDirty,
                "Manager should start clean.");

            // Becomes dirty after push
            _manager.Push(new MockUndoRedoAction("dirty"));
            Assert.IsTrue(_manager.IsDirty,
                "Manager should be dirty after Push.");

            // ClearDirty resets the flag without affecting stacks
            _manager.ClearDirty();
            Assert.IsFalse(_manager.IsDirty,
                "ClearDirty should reset the dirty flag.");
            Assert.AreEqual(1, _manager.GetUndoStackDepth(),
                "ClearDirty must not touch the undo stack.");
        }

        [Test]
        public void PushPopStacks_SavesAndRestoresHistory()
        {
            // Arrange - build up some history
            _manager.Push(new MockUndoRedoAction("outer1"));
            _manager.Push(new MockUndoRedoAction("outer2"));
            _manager.Undo(); // redo has 1

            int savedUndo = _manager.GetUndoStackDepth(); // 1
            int savedRedo = _manager.GetRedoStackDepth(); // 1

            // Act - push stacks (saves current, starts fresh)
            _manager.PushStacks();

            Assert.AreEqual(0, _manager.GetUndoStackDepth(),
                "After PushStacks, undo should be empty.");
            Assert.AreEqual(0, _manager.GetRedoStackDepth(),
                "After PushStacks, redo should be empty.");

            // Do some inner work
            _manager.Push(new MockUndoRedoAction("inner"));

            // Pop stacks (restores saved state)
            _manager.PopStacks();

            // Assert - stacks are restored to what they were before PushStacks
            Assert.AreEqual(savedUndo, _manager.GetUndoStackDepth(),
                "Undo depth should be restored after PopStacks.");
            Assert.AreEqual(savedRedo, _manager.GetRedoStackDepth(),
                "Redo depth should be restored after PopStacks.");
        }

        [Test]
        public void StackDepth_ReturnsCorrectCounts()
        {
            // Empty stacks
            Assert.AreEqual(0, _manager.GetUndoStackDepth());
            Assert.AreEqual(0, _manager.GetRedoStackDepth());

            // Push 3 actions
            _manager.Push(new MockUndoRedoAction("1"));
            _manager.Push(new MockUndoRedoAction("2"));
            _manager.Push(new MockUndoRedoAction("3"));
            Assert.AreEqual(3, _manager.GetUndoStackDepth());
            Assert.AreEqual(0, _manager.GetRedoStackDepth());

            // Undo 2
            _manager.Undo();
            _manager.Undo();
            Assert.AreEqual(1, _manager.GetUndoStackDepth());
            Assert.AreEqual(2, _manager.GetRedoStackDepth());

            // Redo 1
            _manager.Redo();
            Assert.AreEqual(2, _manager.GetUndoStackDepth());
            Assert.AreEqual(1, _manager.GetRedoStackDepth());
        }
    }
}
