// =============================================================================
// CutCopyPasteManagerTests.cs - Edit Mode Unit Tests for CutCopyPasteManager
// =============================================================================
// TARGET CLASS: CutCopyPasteManager
//   Real file: Assets/VivedUpgrades/CutCopyPasteManager.cs
//
// WHAT IT TESTS:
//   Clipboard operations (Copy, Cut, Paste) used throughout Studio. Validates
//   that selected objects are serialized to the system clipboard, locked items
//   are filtered when requested, cut deletes originals, paste creates objects
//   with incremental offsets, and slide builder fallback logic is invoked
//   when no scene objects are selected.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real CutCopyPasteManager is a ZSingleton<T> MonoBehaviour. These
//      tests exercise logic through lightweight POCO stubs so they compile
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

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Minimal stand-in for a scene object that may be locked.</summary>
    public class SceneObjectStub
    {
        public string Id { get; set; }
        public bool IsLocked { get; set; }

        public SceneObjectStub(string id, bool isLocked = false)
        {
            Id = id;
            IsLocked = isLocked;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Minimal stand-in for SelectionManager tracking selected objects.</summary>
    public class SelectionManagerStub
    {
        public List<SceneObjectStub> SelectedSceneObjects { get; } = new List<SceneObjectStub>();

        public void Clear()
        {
            SelectedSceneObjects.Clear();
        }

        public void AddMultiple(List<SceneObjectStub> objects)
        {
            SelectedSceneObjects.AddRange(objects);
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Minimal stand-in for SceneManager clipboard serialization.</summary>
    public class SceneManagerStub
    {
        public string CurrentScene { get; set; } = "Scene1";
        public List<SceneObjectStub> LastDeletedObjects { get; } = new List<SceneObjectStub>();
        public List<SceneObjectStub> ObjectsToCreateOnPaste { get; set; } = new List<SceneObjectStub>();

        public string CreatePasteString(List<SceneObjectStub> objects)
        {
            // Simulate serialization by joining IDs
            var ids = new List<string>();
            foreach (var obj in objects)
            {
                ids.Add(obj.Id);
            }
            return string.Join(",", ids);
        }

        public List<SceneObjectStub> PasteFromString(string pasteString)
        {
            if (string.IsNullOrEmpty(pasteString))
            {
                return new List<SceneObjectStub>();
            }
            return new List<SceneObjectStub>(ObjectsToCreateOnPaste);
        }

        public void DeleteSceneObjects(List<SceneObjectStub> objects)
        {
            LastDeletedObjects.AddRange(objects);
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Minimal stand-in for SlideBuilderController visibility and clipboard operations.</summary>
    public class SlideBuilderControllerStub
    {
        public bool IsVisible { get; set; }
        public bool SlideCopyCalled { get; private set; }
        public bool SlideCutCalled { get; private set; }
        public bool SlidePasteCalled { get; private set; }

        public void SlideCopy() { SlideCopyCalled = true; }
        public void SlideCut() { SlideCutCalled = true; }
        public void SlidePaste() { SlidePasteCalled = true; }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>
    /// Lightweight POCO mirroring the public API of CutCopyPasteManager
    /// without requiring MonoBehaviour or ZSingleton.
    /// </summary>
    public class CutCopyPasteManagerStub
    {
        private readonly SelectionManagerStub _selectionManager;
        private readonly SceneManagerStub _sceneManager;
        private readonly SlideBuilderControllerStub _slideBuilderController;

        private string _systemCopyBuffer = "";
        private int _pasteCount = 0;
        private string _currentScene;
        private static readonly float _pasteOffsetMagnitude = 0.1f;

        public string SystemCopyBuffer => _systemCopyBuffer;
        public int PasteCount => _pasteCount;

        public CutCopyPasteManagerStub(
            SelectionManagerStub selectionManager,
            SceneManagerStub sceneManager,
            SlideBuilderControllerStub slideBuilderController)
        {
            _selectionManager = selectionManager;
            _sceneManager = sceneManager;
            _slideBuilderController = slideBuilderController;
        }

        public void Copy(bool shouldBlockLockedItems = true)
        {
            _currentScene = _sceneManager.CurrentScene;
            if (_selectionManager.SelectedSceneObjects.Count > 0)
            {
                if (shouldBlockLockedItems)
                {
                    List<SceneObjectStub> unlocked = new List<SceneObjectStub>();
                    foreach (var obj in _selectionManager.SelectedSceneObjects)
                    {
                        if (!obj.IsLocked)
                        {
                            unlocked.Add(obj);
                        }
                    }
                    _systemCopyBuffer = _sceneManager.CreatePasteString(unlocked);
                }
                else
                {
                    _systemCopyBuffer = _sceneManager.CreatePasteString(_selectionManager.SelectedSceneObjects);
                }
            }
            else if (_slideBuilderController.IsVisible)
            {
                _slideBuilderController.SlideCopy();
            }
            _pasteCount = 1;
        }

        public void Cut(bool shouldBlockLockedItems = true)
        {
            _currentScene = _sceneManager.CurrentScene;
            if (_selectionManager.SelectedSceneObjects.Count > 0)
            {
                if (shouldBlockLockedItems)
                {
                    List<SceneObjectStub> unlocked = new List<SceneObjectStub>();
                    foreach (var obj in _selectionManager.SelectedSceneObjects)
                    {
                        if (!obj.IsLocked)
                        {
                            unlocked.Add(obj);
                        }
                    }
                    _systemCopyBuffer = _sceneManager.CreatePasteString(unlocked);
                    _sceneManager.DeleteSceneObjects(unlocked);
                }
                else
                {
                    _systemCopyBuffer = _sceneManager.CreatePasteString(_selectionManager.SelectedSceneObjects);
                    _sceneManager.DeleteSceneObjects(_selectionManager.SelectedSceneObjects);
                }
                _selectionManager.Clear();
            }
            else if (_slideBuilderController.IsVisible)
            {
                _slideBuilderController.SlideCut();
            }
            _pasteCount = 0;
        }

        public void Paste()
        {
            List<SceneObjectStub> created = _sceneManager.PasteFromString(_systemCopyBuffer);
            if (created.Count > 0)
            {
                _selectionManager.Clear();
                _selectionManager.AddMultiple(created);
                _pasteCount++;
            }
            else if (_slideBuilderController.IsVisible)
            {
                _slideBuilderController.SlidePaste();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class CutCopyPasteManagerTests
    {
        private CutCopyPasteManagerStub _manager;
        private SelectionManagerStub _selectionManager;
        private SceneManagerStub _sceneManager;
        private SlideBuilderControllerStub _slideBuilderController;

        [SetUp]
        public void SetUp()
        {
            _selectionManager = new SelectionManagerStub();
            _sceneManager = new SceneManagerStub();
            _slideBuilderController = new SlideBuilderControllerStub();
            _manager = new CutCopyPasteManagerStub(
                _selectionManager,
                _sceneManager,
                _slideBuilderController);
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
            _selectionManager = null;
            _sceneManager = null;
            _slideBuilderController = null;
        }

        // WHY: Users expect Copy to serialize only unlocked objects so that
        // locked background items are not accidentally duplicated.
        [Test]
        public void Copy_FiltersLockedItems_WhenBlockLockedIsTrue()
        {
            // Arrange
            _selectionManager.SelectedSceneObjects.Add(new SceneObjectStub("obj1", isLocked: false));
            _selectionManager.SelectedSceneObjects.Add(new SceneObjectStub("obj2", isLocked: true));
            _selectionManager.SelectedSceneObjects.Add(new SceneObjectStub("obj3", isLocked: false));

            // Act
            _manager.Copy(shouldBlockLockedItems: true);

            // Assert
            Assert.AreEqual("obj1,obj3", _manager.SystemCopyBuffer,
                "Copy with shouldBlockLockedItems=true must exclude locked objects from the clipboard.");
        }

        // WHY: Creator-mode workflows sometimes need to copy locked items
        // (e.g., templates). When blocking is disabled, all selected objects
        // must be included regardless of lock state.
        [Test]
        public void Copy_IncludesLockedItems_WhenBlockLockedIsFalse()
        {
            // Arrange
            _selectionManager.SelectedSceneObjects.Add(new SceneObjectStub("obj1", isLocked: false));
            _selectionManager.SelectedSceneObjects.Add(new SceneObjectStub("obj2", isLocked: true));

            // Act
            _manager.Copy(shouldBlockLockedItems: false);

            // Assert
            Assert.AreEqual("obj1,obj2", _manager.SystemCopyBuffer,
                "Copy with shouldBlockLockedItems=false must include all selected objects, even locked ones.");
        }

        // WHY: Copy should reset the paste counter to 1 so the first paste
        // applies the standard offset, preventing exact overlap.
        [Test]
        public void Copy_ResetsPasteCountToOne_AfterCopy()
        {
            // Arrange
            _selectionManager.SelectedSceneObjects.Add(new SceneObjectStub("obj1"));

            // Act
            _manager.Copy();

            // Assert
            Assert.AreEqual(1, _manager.PasteCount,
                "Paste count should be reset to 1 after Copy so the first Paste applies a single offset.");
        }

        // WHY: Cut must delete the source objects so the user sees a true
        // "move" operation, not a duplicate.
        [Test]
        public void Cut_DeletesUnlockedObjects_AndClearsSelection()
        {
            // Arrange
            _selectionManager.SelectedSceneObjects.Add(new SceneObjectStub("obj1", isLocked: false));
            _selectionManager.SelectedSceneObjects.Add(new SceneObjectStub("obj2", isLocked: true));

            // Act
            _manager.Cut(shouldBlockLockedItems: true);

            // Assert
            Assert.AreEqual(1, _sceneManager.LastDeletedObjects.Count,
                "Cut should delete only unlocked objects when shouldBlockLockedItems is true.");
            Assert.AreEqual("obj1", _sceneManager.LastDeletedObjects[0].Id,
                "The deleted object should be the unlocked one.");
            Assert.AreEqual(0, _selectionManager.SelectedSceneObjects.Count,
                "Selection must be cleared after Cut so the deleted objects are no longer highlighted.");
        }

        // WHY: Cut resets paste count to 0 so the first paste lands at the
        // original position (no offset), preserving spatial intent.
        [Test]
        public void Cut_ResetsPasteCountToZero_ForZeroOffsetOnFirstPaste()
        {
            // Arrange
            _selectionManager.SelectedSceneObjects.Add(new SceneObjectStub("obj1"));

            // Act
            _manager.Cut();

            // Assert
            Assert.AreEqual(0, _manager.PasteCount,
                "Paste count should be 0 after Cut so the first Paste lands at the original position.");
        }

        // WHY: Each successive paste must increment the counter so pasted
        // objects fan out instead of stacking on top of each other.
        [Test]
        public void Paste_IncrementsPasteCount_WhenObjectsCreated()
        {
            // Arrange
            _selectionManager.SelectedSceneObjects.Add(new SceneObjectStub("obj1"));
            _sceneManager.ObjectsToCreateOnPaste.Add(new SceneObjectStub("pasted1"));
            _manager.Copy();
            int countAfterCopy = _manager.PasteCount; // 1

            // Act
            _manager.Paste();

            // Assert
            Assert.AreEqual(countAfterCopy + 1, _manager.PasteCount,
                "Paste count should increment by 1 after each successful paste to increase the offset.");
        }

        // WHY: When no scene objects are selected but the slide builder is
        // open, clipboard operations should delegate to the slide builder
        // so that slide-level copy/cut/paste works seamlessly.
        [Test]
        public void Copy_DelegatesToSlideBuilder_WhenNoObjectsSelectedAndSlideBuilderVisible()
        {
            // Arrange - no selected scene objects
            _slideBuilderController.IsVisible = true;

            // Act
            _manager.Copy();

            // Assert
            Assert.IsTrue(_slideBuilderController.SlideCopyCalled,
                "Copy should delegate to SlideBuilderController.SlideCopy() when no objects are selected and the slide builder is visible.");
        }

        // WHY: Paste must update the selection to the newly created objects
        // so the user can immediately manipulate them.
        [Test]
        public void Paste_SelectsNewlyCreatedObjects_AfterPaste()
        {
            // Arrange
            var pastedObj = new SceneObjectStub("pasted1");
            _selectionManager.SelectedSceneObjects.Add(new SceneObjectStub("original"));
            _sceneManager.ObjectsToCreateOnPaste.Add(pastedObj);
            _manager.Copy();

            // Act
            _manager.Paste();

            // Assert
            Assert.AreEqual(1, _selectionManager.SelectedSceneObjects.Count,
                "Selection should contain only the newly pasted objects.");
            Assert.AreEqual("pasted1", _selectionManager.SelectedSceneObjects[0].Id,
                "The selected object should be the pasted one, not the original.");
        }
    }
}
