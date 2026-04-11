// =============================================================================
// ContextMenuControllerTests.cs - Edit Mode Unit Tests for ContextMenuController
// =============================================================================
// TARGET CLASS: ContextMenuController
//   Real file: Assets/StudioA3/Scripts/UI/ContextMenu/ContextMenuController.cs
//
// WHAT IT TESTS:
//   Context menu that builds menu items based on the current selection state,
//   manages lock/unlock icon display, determines when object settings are
//   visible, and clamps the menu position within screen bounds. Tests validate
//   menu composition rules, lock icon logic, creator-mode gating, and
//   boundary clamping math.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real ContextMenuController is a MonoBehaviour that creates Unity
//      UI elements at runtime. The stub here exercises only the menu-building
//      logic, icon selection, and position clamping without Unity runtime.
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
    /// Lightweight stand-in for a scene object that can be selected and locked.
    /// </summary>
    public class SceneObject
    {
        public string Name { get; set; }
        public bool IsLocked { get; set; }

        public SceneObject(string name, bool isLocked = false)
        {
            Name = name;
            IsLocked = isLocked;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real enum instead.
    /// <summary>
    /// Controls how labels are displayed on 3D models in the scene.
    /// </summary>
    public enum LabelSetting
    {
        ShowNearest,
        ShowAll,
        ShowNone
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the menu-building, icon-selection, settings-gating, and
    /// position-clamping logic of the real ContextMenuController without
    /// requiring MonoBehaviour or Unity UI.
    /// </summary>
    public class ContextMenuControllerStub
    {
        // Base items always shown in the context menu
        private static readonly List<string> BaseMenuItems = new List<string>
        {
            "Lock",
            "Duplicate",
            "Select All"
        };

        // Items only shown when exactly one object is selected
        private static readonly List<string> SingleSelectionItems = new List<string>
        {
            "Cut",
            "Copy",
            "Delete"
        };

        /// <summary>
        /// Builds the list of context menu item names based on the current
        /// selection. Cut, Copy, and Delete are only available for single
        /// selection to avoid ambiguity in multi-select scenarios.
        /// </summary>
        public List<string> BuildMenuItems(List<SceneObject> selection)
        {
            var items = new List<string>(BaseMenuItems);

            if (selection != null && selection.Count == 1)
            {
                items.AddRange(SingleSelectionItems);
            }

            return items;
        }

        /// <summary>
        /// Returns the appropriate icon name based on the object's lock state.
        /// </summary>
        public string GetLockIcon(bool isLocked)
        {
            return isLocked ? "Unlock" : "Lock";
        }

        /// <summary>
        /// Object settings (material, physics, etc.) are only shown for model
        /// objects when the user is in creator mode. Students in viewer mode
        /// should not see configuration options.
        /// </summary>
        public bool ShouldShowObjectSettings(SceneObject obj, bool isCreatorMode)
        {
            if (obj == null)
            {
                return false;
            }

            return isCreatorMode;
        }

        /// <summary>
        /// Clamps the context menu position so it stays within the visible
        /// screen area. Returns the clamped (x, y) as a tuple.
        /// </summary>
        public (float x, float y) ClampToScreenBounds(
            float x, float y,
            float menuWidth, float menuHeight,
            float screenWidth, float screenHeight)
        {
            float clampedX = Math.Max(0, Math.Min(x, screenWidth - menuWidth));
            float clampedY = Math.Max(0, Math.Min(y, screenHeight - menuHeight));
            return (clampedX, clampedY);
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ContextMenuControllerTests
    {
        private ContextMenuControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new ContextMenuControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void BuildMenuItems_SingleSelection_ShowsCutCopyDelete()
        {
            // WHY: When a student right-clicks a single 3D model, they expect
            // clipboard operations so they can rearrange their scene layout.

            // Arrange
            var selection = new List<SceneObject>
            {
                new SceneObject("Heart Model")
            };

            // Act
            List<string> items = _controller.BuildMenuItems(selection);

            // Assert
            Assert.IsTrue(items.Contains("Cut"),
                "Single selection should include Cut in the context menu.");
            Assert.IsTrue(items.Contains("Copy"),
                "Single selection should include Copy in the context menu.");
            Assert.IsTrue(items.Contains("Delete"),
                "Single selection should include Delete in the context menu.");
        }

        [Test]
        public void BuildMenuItems_MultiSelection_HidesCutCopyDelete()
        {
            // WHY: Multi-object Cut/Copy/Delete can cause confusion about which
            // object is affected; the app hides these to prevent accidental data loss.

            // Arrange
            var selection = new List<SceneObject>
            {
                new SceneObject("Heart Model"),
                new SceneObject("Lung Model"),
                new SceneObject("Brain Model")
            };

            // Act
            List<string> items = _controller.BuildMenuItems(selection);

            // Assert
            Assert.IsFalse(items.Contains("Cut"),
                "Multi-selection should hide Cut from the context menu.");
            Assert.IsFalse(items.Contains("Copy"),
                "Multi-selection should hide Copy from the context menu.");
            Assert.IsFalse(items.Contains("Delete"),
                "Multi-selection should hide Delete from the context menu.");
        }

        [Test]
        public void GetLockIcon_LockedObject_ShowsUnlock()
        {
            // WHY: When a model is locked, the context menu should offer Unlock
            // so the student can move it again; showing Lock would be confusing.

            // Arrange
            var lockedObj = new SceneObject("Skeleton", isLocked: true);

            // Act
            string icon = _controller.GetLockIcon(lockedObj.IsLocked);

            // Assert
            Assert.AreEqual("Unlock", icon,
                "A locked object's context menu should show the Unlock icon.");
        }

        [Test]
        public void GetLockIcon_UnlockedObject_ShowsLock()
        {
            // WHY: When a model is unlocked and movable, the context menu should
            // offer Lock so teachers can pin reference models in place.

            // Arrange
            var unlockedObj = new SceneObject("Microscope", isLocked: false);

            // Act
            string icon = _controller.GetLockIcon(unlockedObj.IsLocked);

            // Assert
            Assert.AreEqual("Lock", icon,
                "An unlocked object's context menu should show the Lock icon.");
        }

        [Test]
        public void ClampToScreenBounds_KeepsMenuInsideScreen()
        {
            // WHY: If a student right-clicks near the screen edge, the menu
            // must shift inward so all options remain visible and clickable.

            // Arrange — menu at center of a 1920x1080 screen
            float menuWidth = 200f;
            float menuHeight = 300f;

            // Act
            var result = _controller.ClampToScreenBounds(
                500f, 400f, menuWidth, menuHeight, 1920f, 1080f);

            // Assert
            Assert.AreEqual(500f, result.x,
                "Menu X should remain unchanged when it fits within the screen.");
            Assert.AreEqual(400f, result.y,
                "Menu Y should remain unchanged when it fits within the screen.");
        }

        [Test]
        public void ClampToScreenBounds_ClampsAtEdgePositions()
        {
            // WHY: Edge cases where the menu would extend beyond the screen must
            // be clamped so no menu items are hidden off-screen.

            // Arrange — menu near bottom-right corner
            float menuWidth = 200f;
            float menuHeight = 300f;
            float screenWidth = 1920f;
            float screenHeight = 1080f;

            // Act — position would cause menu to overflow right and bottom edges
            var result = _controller.ClampToScreenBounds(
                1800f, 900f, menuWidth, menuHeight, screenWidth, screenHeight);

            // Assert
            Assert.AreEqual(1720f, result.x,
                "Menu X should be clamped to screenWidth - menuWidth (1920 - 200 = 1720).");
            Assert.AreEqual(780f, result.y,
                "Menu Y should be clamped to screenHeight - menuHeight (1080 - 300 = 780).");
        }

        [Test]
        public void BuildMenuItems_EmptySelection_ReturnsBaseMenuOnly()
        {
            // WHY: Right-clicking on empty space should still show general scene
            // commands (Lock, Duplicate, Select All) but not object-specific ones.

            // Arrange
            var emptySelection = new List<SceneObject>();

            // Act
            List<string> items = _controller.BuildMenuItems(emptySelection);

            // Assert
            Assert.IsTrue(items.Contains("Lock"),
                "Base menu should always include Lock.");
            Assert.IsTrue(items.Contains("Duplicate"),
                "Base menu should always include Duplicate.");
            Assert.IsTrue(items.Contains("Select All"),
                "Base menu should always include Select All.");
            Assert.IsFalse(items.Contains("Cut"),
                "Empty selection should not include Cut.");
            Assert.AreEqual(3, items.Count,
                "Empty selection should return only the three base menu items.");
        }

        [Test]
        public void ShouldShowObjectSettings_OnlyInCreatorMode()
        {
            // WHY: Object settings (materials, physics) are editing features;
            // showing them in viewer mode would let students accidentally modify
            // a teacher's carefully configured activity.

            // Arrange
            var model = new SceneObject("DNA Helix");

            // Act & Assert
            Assert.IsTrue(_controller.ShouldShowObjectSettings(model, isCreatorMode: true),
                "Object settings should be visible in creator mode.");
            Assert.IsFalse(_controller.ShouldShowObjectSettings(model, isCreatorMode: false),
                "Object settings should be hidden in viewer mode.");
        }
    }
}
