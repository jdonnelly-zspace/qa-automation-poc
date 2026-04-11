// =============================================================================
// SelectionManagerTests.cs - Edit Mode Unit Tests for SelectionManager
// =============================================================================
// TARGET CLASS: SelectionManager
//   Real file: Assets/CommonA3/zSpace/Scripts/Tools/SelectionTool/SelectionManager.cs
//
// WHAT IT TESTS:
//   The singleton that manages selected scene objects in the Studio A3 editor.
//   Validates add, remove, contains, clear, multi-select, and the
//   OnSelectionChanged event behavior including the invoke parameter.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real SelectionManager is a singleton MonoBehaviour. This stub
//      exercises the selection logic through lightweight POCOs so it compiles
//      standalone in the POC without a Unity runtime.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for a scene object in the 3D workspace.
    /// </summary>
    public class SceneObject
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public SceneObject(string name, bool isActive = true)
        {
            Name = name;
            IsActive = isActive;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for an interactable wrapper around a scene object.
    /// </summary>
    public class Interactable
    {
        public string Id { get; set; }
        public SceneObject SceneObject { get; set; }

        public Interactable(string id, SceneObject sceneObject)
        {
            Id = id;
            SceneObject = sceneObject;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight stand-in for the real SelectionManager singleton.
    /// Replicates add, remove, clear, and event APIs.
    /// </summary>
    public class SelectionManagerStub
    {
        private readonly List<SceneObject> _selectedSceneObjects = new List<SceneObject>();
        private readonly List<Interactable> _selectedObjects = new List<Interactable>();

        public event Action OnSelectionChanged;

        public IReadOnlyList<SceneObject> SelectedSceneObjects => _selectedSceneObjects.AsReadOnly();
        public IReadOnlyList<Interactable> SelectedObjects => _selectedObjects.AsReadOnly();

        public void Add(SceneObject sceneObject)
        {
            if (sceneObject == null || _selectedSceneObjects.Contains(sceneObject))
            {
                return;
            }

            _selectedSceneObjects.Add(sceneObject);
            OnSelectionChanged?.Invoke();
        }

        public void Add(Interactable interactable)
        {
            if (interactable == null || _selectedObjects.Contains(interactable))
            {
                return;
            }

            _selectedObjects.Add(interactable);
            if (interactable.SceneObject != null &&
                !_selectedSceneObjects.Contains(interactable.SceneObject))
            {
                _selectedSceneObjects.Add(interactable.SceneObject);
            }
            OnSelectionChanged?.Invoke();
        }

        public void AddMultiple(IEnumerable<SceneObject> sceneObjects)
        {
            bool changed = false;
            foreach (SceneObject obj in sceneObjects)
            {
                if (obj != null && !_selectedSceneObjects.Contains(obj))
                {
                    _selectedSceneObjects.Add(obj);
                    changed = true;
                }
            }

            if (changed)
            {
                OnSelectionChanged?.Invoke();
            }
        }

        public bool Contains(Interactable interactable)
        {
            return _selectedObjects.Contains(interactable);
        }

        public bool Remove(Interactable interactable)
        {
            bool removed = _selectedObjects.Remove(interactable);
            if (removed && interactable.SceneObject != null)
            {
                _selectedSceneObjects.Remove(interactable.SceneObject);
                OnSelectionChanged?.Invoke();
            }
            return removed;
        }

        public void Clear(bool invoke = true)
        {
            _selectedSceneObjects.Clear();
            _selectedObjects.Clear();

            if (invoke)
            {
                OnSelectionChanged?.Invoke();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class SelectionManagerTests
    {
        private SelectionManagerStub _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SelectionManagerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
        }

        [Test]
        public void Add_SingleObject_IncreasesCount()
        {
            // WHY: Selecting a single model in the 3D workspace is the most
            // common interaction; the manager must track it.

            // Arrange
            var heart = new SceneObject("Human Heart");

            // Act
            _manager.Add(heart);

            // Assert
            Assert.AreEqual(1, _manager.SelectedSceneObjects.Count,
                "Selected scene objects should contain exactly one item after adding.");
            Assert.AreEqual("Human Heart", _manager.SelectedSceneObjects[0].Name,
                "The selected object should be the Human Heart.");
        }

        [Test]
        public void Add_DuplicateSceneObject_IsIgnored()
        {
            // WHY: Double-clicking the same object must not add it twice;
            // duplicate entries would break property panels and transforms.

            // Arrange
            var skull = new SceneObject("Human Skull");
            _manager.Add(skull);

            // Act
            _manager.Add(skull);

            // Assert
            Assert.AreEqual(1, _manager.SelectedSceneObjects.Count,
                "Adding the same scene object twice should not duplicate it.");
        }

        [Test]
        public void Contains_ReturnsTrue_ForAddedInteractable()
        {
            // WHY: The selection highlight shader checks Contains() to decide
            // whether to render the selection outline.

            // Arrange
            var sceneObj = new SceneObject("Frog Model");
            var interactable = new Interactable("frog-001", sceneObj);
            _manager.Add(interactable);

            // Act & Assert
            Assert.IsTrue(_manager.Contains(interactable),
                "Contains should return true for an interactable that was added.");
        }

        [Test]
        public void Remove_ReturnsTrue_AndDecreasesCount()
        {
            // WHY: Deselecting an object must cleanly remove it so the
            // property panel and gizmos update correctly.

            // Arrange
            var sceneObj = new SceneObject("Solar Panel");
            var interactable = new Interactable("panel-001", sceneObj);
            _manager.Add(interactable);

            // Act
            bool removed = _manager.Remove(interactable);

            // Assert
            Assert.IsTrue(removed,
                "Remove should return true when the interactable was in the selection.");
            Assert.AreEqual(0, _manager.SelectedObjects.Count,
                "Selected objects count should be zero after removal.");
            Assert.AreEqual(0, _manager.SelectedSceneObjects.Count,
                "Selected scene objects should also be cleared after removal.");
        }

        [Test]
        public void Remove_ReturnsFalse_ForNonExistentInteractable()
        {
            // WHY: Attempting to remove something not selected should be a
            // safe no-op; the UI should not show errors.

            // Arrange
            var sceneObj = new SceneObject("Rocket Ship");
            var interactable = new Interactable("rocket-001", sceneObj);

            // Act
            bool removed = _manager.Remove(interactable);

            // Assert
            Assert.IsFalse(removed,
                "Remove should return false when the interactable was not in the selection.");
        }

        [Test]
        public void Clear_EmptiesSelectionList()
        {
            // WHY: Pressing Escape or switching tools must clear all
            // selections to return to a clean editor state.

            // Arrange
            _manager.Add(new SceneObject("Earth Globe"));
            _manager.Add(new SceneObject("Moon Rock"));
            _manager.Add(new SceneObject("Mars Rover"));

            // Act
            _manager.Clear();

            // Assert
            Assert.AreEqual(0, _manager.SelectedSceneObjects.Count,
                "All scene objects should be removed after Clear().");
        }

        [Test]
        public void Clear_WithInvokeTrue_FiresOnSelectionChanged()
        {
            // WHY: UI panels like the property inspector listen to
            // OnSelectionChanged to update; Clear must notify them.

            // Arrange
            _manager.Add(new SceneObject("Beaker"));
            int eventCount = 0;
            _manager.OnSelectionChanged += () => eventCount++;

            // Act
            _manager.Clear(invoke: true);

            // Assert
            Assert.AreEqual(1, eventCount,
                "OnSelectionChanged should fire exactly once when Clear is called with invoke=true.");
        }

        [Test]
        public void Clear_WithInvokeFalse_DoesNotFireOnSelectionChanged()
        {
            // WHY: During bulk operations (e.g., scene teardown) we need to
            // clear without triggering expensive UI refreshes.

            // Arrange
            _manager.Add(new SceneObject("Test Tube"));
            int eventCount = 0;
            _manager.OnSelectionChanged += () => eventCount++;

            // Act
            _manager.Clear(invoke: false);

            // Assert
            Assert.AreEqual(0, eventCount,
                "OnSelectionChanged should NOT fire when Clear is called with invoke=false.");
            Assert.AreEqual(0, _manager.SelectedSceneObjects.Count,
                "Selection should still be empty even though the event was suppressed.");
        }

        [Test]
        public void AddMultiple_AddsAllSceneObjects()
        {
            // WHY: Marquee selection (drag-box) selects multiple objects at
            // once; AddMultiple must handle the batch correctly.

            // Arrange
            var objects = new List<SceneObject>
            {
                new SceneObject("Mitochondria"),
                new SceneObject("Cell Membrane"),
                new SceneObject("Nucleus")
            };

            // Act
            _manager.AddMultiple(objects);

            // Assert
            Assert.AreEqual(3, _manager.SelectedSceneObjects.Count,
                "All three scene objects should be in the selection after AddMultiple.");
            Assert.IsTrue(_manager.SelectedSceneObjects.Any(o => o.Name == "Mitochondria"),
                "Mitochondria should be in the selection.");
            Assert.IsTrue(_manager.SelectedSceneObjects.Any(o => o.Name == "Nucleus"),
                "Nucleus should be in the selection.");
        }
    }
}
