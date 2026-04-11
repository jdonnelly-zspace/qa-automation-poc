// =============================================================================
// DeleteManagerTests.cs - Edit Mode Unit Tests for DeleteManager
// =============================================================================
// TARGET CLASS: DeleteManager
//   Real file: Assets/CommonA3/zSpace/Scripts/Delete/DeleteManager.cs
//
// WHAT IT TESTS:
//   The singleton that implements soft-delete with undo support for scene
//   objects. Validates delete, restore, batch operations, event firing,
//   state queries, and edge cases like double-delete safety.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real DeleteManager is a singleton MonoBehaviour. This stub
//      exercises the soft-delete logic through lightweight POCOs so it
//      compiles standalone in the POC without a Unity runtime.
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
    /// Reuses the same shape as the real SceneObject for delete tracking.
    /// </summary>
    public class DeleteableSceneObject
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public DeleteableSceneObject(string name, bool isActive = true)
        {
            Name = name;
            IsActive = isActive;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight stand-in for the real DeleteManager singleton.
    /// Implements soft-delete: objects are marked inactive and tracked in a
    /// set, allowing restore (undo) before permanent removal via Clear.
    /// </summary>
    public class DeleteManagerStub
    {
        private readonly HashSet<DeleteableSceneObject> _deletedObjects =
            new HashSet<DeleteableSceneObject>();

        public event Action<DeleteableSceneObject> OnDelete;

        public void Delete(DeleteableSceneObject sceneObject)
        {
            if (sceneObject == null)
            {
                return;
            }

            sceneObject.IsActive = false;
            _deletedObjects.Add(sceneObject);
            OnDelete?.Invoke(sceneObject);
        }

        public void Delete(IEnumerable<DeleteableSceneObject> sceneObjects)
        {
            foreach (DeleteableSceneObject obj in sceneObjects)
            {
                Delete(obj);
            }
        }

        public void Restore(DeleteableSceneObject sceneObject)
        {
            if (sceneObject == null || !_deletedObjects.Contains(sceneObject))
            {
                return;
            }

            sceneObject.IsActive = true;
            _deletedObjects.Remove(sceneObject);
        }

        public void Restore(IEnumerable<DeleteableSceneObject> sceneObjects)
        {
            foreach (DeleteableSceneObject obj in sceneObjects.ToList())
            {
                Restore(obj);
            }
        }

        public bool IsObjectDeleted(DeleteableSceneObject sceneObject)
        {
            return _deletedObjects.Contains(sceneObject);
        }

        public void Clear()
        {
            _deletedObjects.Clear();
        }

        public int DeletedCount => _deletedObjects.Count;
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class DeleteManagerTests
    {
        private DeleteManagerStub _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new DeleteManagerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
        }

        [Test]
        public void Delete_MarksObjectAsInactive()
        {
            // WHY: Soft-delete must deactivate the object in the scene so it
            // disappears visually but remains available for undo.

            // Arrange
            var heart = new DeleteableSceneObject("Human Heart");
            Assert.IsTrue(heart.IsActive, "Object should start active.");

            // Act
            _manager.Delete(heart);

            // Assert
            Assert.IsFalse(heart.IsActive,
                "Object should be inactive after deletion.");
        }

        [Test]
        public void Delete_FiresOnDeleteEvent()
        {
            // WHY: The selection manager and property panel listen to OnDelete
            // to deselect and hide UI for the deleted object.

            // Arrange
            DeleteableSceneObject deletedObj = null;
            _manager.OnDelete += obj => deletedObj = obj;
            var skull = new DeleteableSceneObject("Human Skull");

            // Act
            _manager.Delete(skull);

            // Assert
            Assert.IsNotNull(deletedObj,
                "OnDelete event should have fired.");
            Assert.AreEqual("Human Skull", deletedObj.Name,
                "OnDelete should pass the deleted object.");
        }

        [Test]
        public void IsObjectDeleted_ReturnsTrue_AfterDelete()
        {
            // WHY: Systems like the undo stack need to query whether an object
            // is in the deleted state to decide if restore is appropriate.

            // Arrange
            var globe = new DeleteableSceneObject("Earth Globe");

            // Act
            _manager.Delete(globe);

            // Assert
            Assert.IsTrue(_manager.IsObjectDeleted(globe),
                "IsObjectDeleted should return true after the object is deleted.");
        }

        [Test]
        public void Restore_ReactivatesObject()
        {
            // WHY: Undo-delete must re-show the object in the scene exactly
            // where it was, so the student can recover mistakes.

            // Arrange
            var rover = new DeleteableSceneObject("Mars Rover");
            _manager.Delete(rover);
            Assert.IsFalse(rover.IsActive, "Object should be inactive after delete.");

            // Act
            _manager.Restore(rover);

            // Assert
            Assert.IsTrue(rover.IsActive,
                "Object should be active again after restore.");
        }

        [Test]
        public void Restore_MakesIsObjectDeletedReturnFalse()
        {
            // WHY: After restore, the object must no longer appear in the
            // deleted set; otherwise it could be double-restored or skipped
            // during scene save.

            // Arrange
            var beaker = new DeleteableSceneObject("Glass Beaker");
            _manager.Delete(beaker);

            // Act
            _manager.Restore(beaker);

            // Assert
            Assert.IsFalse(_manager.IsObjectDeleted(beaker),
                "IsObjectDeleted should return false after the object is restored.");
        }

        [Test]
        public void Clear_RemovesAllDeletedObjectsPermanently()
        {
            // WHY: When saving a scene, all soft-deleted objects are
            // permanently discarded. Clear empties the deleted set so they
            // cannot be restored after save.

            // Arrange
            var obj1 = new DeleteableSceneObject("Telescope");
            var obj2 = new DeleteableSceneObject("Microscope");
            _manager.Delete(obj1);
            _manager.Delete(obj2);
            Assert.AreEqual(2, _manager.DeletedCount, "Two objects should be in deleted set.");

            // Act
            _manager.Clear();

            // Assert
            Assert.AreEqual(0, _manager.DeletedCount,
                "Deleted set should be empty after Clear().");
            Assert.IsFalse(_manager.IsObjectDeleted(obj1),
                "Telescope should no longer be tracked as deleted after Clear.");
            Assert.IsFalse(_manager.IsObjectDeleted(obj2),
                "Microscope should no longer be tracked as deleted after Clear.");
        }

        [Test]
        public void Delete_BatchOperation_DeletesAll()
        {
            // WHY: Multi-select delete must soft-delete every selected object
            // in one operation, firing events for each.

            // Arrange
            var objects = new List<DeleteableSceneObject>
            {
                new DeleteableSceneObject("Red Blood Cell"),
                new DeleteableSceneObject("White Blood Cell"),
                new DeleteableSceneObject("Platelet")
            };
            int eventCount = 0;
            _manager.OnDelete += _ => eventCount++;

            // Act
            _manager.Delete(objects);

            // Assert
            Assert.AreEqual(3, _manager.DeletedCount,
                "All three objects should be in the deleted set.");
            Assert.IsTrue(objects.All(o => !o.IsActive),
                "All objects should be inactive after batch delete.");
            Assert.AreEqual(3, eventCount,
                "OnDelete should fire once per deleted object.");
        }

        [Test]
        public void Restore_BatchOperation_RestoresAll()
        {
            // WHY: Undo on a batch delete must restore every object in the
            // group, not just the first one.

            // Arrange
            var objects = new List<DeleteableSceneObject>
            {
                new DeleteableSceneObject("Femur Bone"),
                new DeleteableSceneObject("Tibia Bone")
            };
            _manager.Delete(objects);

            // Act
            _manager.Restore(objects);

            // Assert
            Assert.IsTrue(objects.All(o => o.IsActive),
                "All objects should be active after batch restore.");
            Assert.AreEqual(0, _manager.DeletedCount,
                "No objects should remain in the deleted set after batch restore.");
        }

        [Test]
        public void DoubleDelete_IsSafe_NoException()
        {
            // WHY: Race conditions or rapid clicks could call Delete twice on
            // the same object. This must not crash or corrupt state.

            // Arrange
            var planet = new DeleteableSceneObject("Jupiter Model");
            _manager.Delete(planet);

            // Act & Assert
            Assert.DoesNotThrow(() => _manager.Delete(planet),
                "Deleting an already-deleted object should not throw an exception.");
            Assert.AreEqual(1, _manager.DeletedCount,
                "Deleted count should still be 1 (HashSet prevents duplicates).");
            Assert.IsFalse(planet.IsActive,
                "Object should remain inactive after double-delete.");
        }
    }
}
