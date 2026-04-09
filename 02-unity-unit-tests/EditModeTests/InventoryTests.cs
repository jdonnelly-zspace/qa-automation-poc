// =============================================================================
// InventoryTests.cs - Edit Mode Unit Tests for Lab Inventory System
// =============================================================================
// PURPOSE: Demonstrates how to write Edit Mode tests for the Inventory system,
//          which manages the add/remove of lab items (beakers, tools, specimens)
//          during zSpace educational activities.
//
// TEMPLATE NOTICE: This file is a POC template. Search for "TODO" comments to find
//          every place you need to adapt to match your actual class names and methods.
//
// HOW TO USE:
//   1. Copy into Assets/Tests/EditModeTests/ in your Unity project
//   2. Update namespaces and class references to match your Inventory system
//   3. Run via Window > General > Test Runner > EditMode tab
// =============================================================================

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// TODO: Replace with the actual namespace where your Inventory class lives
// Example: using zSpace.Lab;
// Example: using zSpace.Inventory;

namespace QAAutomation.EditModeTests
{
    /// <summary>
    /// Tests for the Inventory system that manages lab items students can
    /// interact with during activities. Covers add, remove, duplicates,
    /// and capacity limits.
    /// </summary>
    [TestFixture]
    public class InventoryTests
    {
        // TODO: Replace with your actual Inventory type
        private InventoryStub _inventory;

        [SetUp]
        public void SetUp()
        {
            // Start each test with a fresh, empty inventory
            // TODO: Replace with your actual Inventory constructor
            _inventory = new InventoryStub(maxCapacity: 10);
        }

        [TearDown]
        public void TearDown()
        {
            _inventory = null;
        }

        // ---------------------------------------------------------------------
        // Test: Adding an item increases the inventory count
        // ---------------------------------------------------------------------
        // WHY: This is the fundamental happy-path operation. If adding items
        //      doesn't work, the entire lab experience is broken because
        //      students can't place objects on the virtual lab bench.
        // ---------------------------------------------------------------------
        [Test]
        public void AddItem_SingleItem_IncreasesCountByOne()
        {
            // Arrange -- inventory starts empty
            Assert.AreEqual(0, _inventory.Count, "Precondition: inventory should start empty");

            // Act -- add one lab item
            // TODO: Replace with your actual item type and add method
            bool added = _inventory.AddItem(new LabItemStub
            {
                ItemId = "beaker-250ml",
                DisplayName = "250ml Beaker"
            });

            // Assert
            Assert.IsTrue(added, "AddItem should return true when item is successfully added");
            Assert.AreEqual(1, _inventory.Count,
                "Inventory count should be 1 after adding a single item");
        }

        // ---------------------------------------------------------------------
        // Test: Removing an item decreases the inventory count
        // ---------------------------------------------------------------------
        // WHY: Students remove items from the lab bench to clean up or swap
        //      tools. If removal doesn't work, items pile up and the
        //      experience becomes cluttered and confusing.
        // ---------------------------------------------------------------------
        [Test]
        public void RemoveItem_ExistingItem_DecreasesCountByOne()
        {
            // Arrange -- add an item first so we have something to remove
            var item = new LabItemStub
            {
                ItemId = "thermometer-01",
                DisplayName = "Digital Thermometer"
            };
            _inventory.AddItem(item);
            Assert.AreEqual(1, _inventory.Count, "Precondition: should have 1 item");

            // Act -- remove the item we just added
            // TODO: Replace with your actual remove method
            bool removed = _inventory.RemoveItem("thermometer-01");

            // Assert
            Assert.IsTrue(removed, "RemoveItem should return true for an existing item");
            Assert.AreEqual(0, _inventory.Count,
                "Inventory count should be 0 after removing the only item");
        }

        // ---------------------------------------------------------------------
        // Test: Removing a non-existent item returns false without crashing
        // ---------------------------------------------------------------------
        // WHY: Race conditions or stale UI state can cause remove calls for
        //      items that are already gone. The system must handle this
        //      gracefully without exceptions.
        // ---------------------------------------------------------------------
        [Test]
        public void RemoveItem_NonExistentItem_ReturnsFalseWithoutThrowing()
        {
            // Arrange -- add one item (not the one we'll try to remove)
            _inventory.AddItem(new LabItemStub
            {
                ItemId = "beaker-250ml",
                DisplayName = "250ml Beaker"
            });

            // Act -- try to remove an item that was never added
            bool removed = _inventory.RemoveItem("nonexistent-item-xyz");

            // Assert
            Assert.IsFalse(removed,
                "RemoveItem should return false when the item ID doesn't exist");
            Assert.AreEqual(1, _inventory.Count,
                "Inventory count should be unchanged after a failed removal");
        }

        // ---------------------------------------------------------------------
        // Test: Adding a duplicate item ID is rejected
        // ---------------------------------------------------------------------
        // WHY: Each lab item instance should be unique. Allowing duplicates
        //      could cause issues with item tracking, save/load state, and
        //      interaction targeting. This test ensures the inventory enforces
        //      uniqueness.
        // ---------------------------------------------------------------------
        [Test]
        public void AddItem_DuplicateId_IsRejectedAndCountUnchanged()
        {
            // Arrange -- add the first item
            _inventory.AddItem(new LabItemStub
            {
                ItemId = "microscope-01",
                DisplayName = "Compound Microscope"
            });
            Assert.AreEqual(1, _inventory.Count, "Precondition: should have 1 item");

            // Act -- try to add another item with the SAME ID
            bool addedDuplicate = _inventory.AddItem(new LabItemStub
            {
                ItemId = "microscope-01",           // same ID
                DisplayName = "Duplicate Microscope" // different name, same ID
            });

            // Assert
            Assert.IsFalse(addedDuplicate,
                "AddItem should return false when adding a duplicate item ID");
            Assert.AreEqual(1, _inventory.Count,
                "Inventory count should remain 1 after rejecting a duplicate");
        }

        // ---------------------------------------------------------------------
        // Test: Adding items beyond capacity is rejected
        // ---------------------------------------------------------------------
        // WHY: The inventory has a max capacity to prevent performance issues
        //      (too many 3D objects) and to enforce pedagogical constraints.
        //      Exceeding capacity must be blocked cleanly.
        // ---------------------------------------------------------------------
        [Test]
        public void AddItem_BeyondCapacity_IsRejected()
        {
            // Arrange -- fill the inventory to its max capacity (10)
            for (int i = 0; i < 10; i++)
            {
                _inventory.AddItem(new LabItemStub
                {
                    ItemId = $"item-{i}",
                    DisplayName = $"Lab Item {i}"
                });
            }
            Assert.AreEqual(10, _inventory.Count, "Precondition: inventory should be full");

            // Act -- try to add one more item beyond capacity
            bool addedOverflow = _inventory.AddItem(new LabItemStub
            {
                ItemId = "item-overflow",
                DisplayName = "Overflow Item"
            });

            // Assert
            Assert.IsFalse(addedOverflow,
                "AddItem should return false when inventory is at max capacity");
            Assert.AreEqual(10, _inventory.Count,
                "Inventory count should remain at capacity limit");
        }

        // ---------------------------------------------------------------------
        // Test: Contains check works correctly
        // ---------------------------------------------------------------------
        // WHY: UI elements need to check whether an item is in the inventory
        //      to show the correct state (e.g., graying out an "Add" button).
        //      A broken Contains check leads to confusing UI states.
        // ---------------------------------------------------------------------
        [Test]
        public void Contains_AddedItem_ReturnsTrue()
        {
            // Arrange
            _inventory.AddItem(new LabItemStub
            {
                ItemId = "bunsen-burner-01",
                DisplayName = "Bunsen Burner"
            });

            // Act & Assert
            Assert.IsTrue(_inventory.Contains("bunsen-burner-01"),
                "Contains should return true for an item that was added");
            Assert.IsFalse(_inventory.Contains("item-never-added"),
                "Contains should return false for an item that was not added");
        }
    }

    // =========================================================================
    // STUB CLASSES -- Replace with references to your actual Inventory classes
    // =========================================================================
    // TODO: DELETE these stubs and reference your real Inventory and LabItem classes.
    // =========================================================================

    public class InventoryStub
    {
        private readonly int _maxCapacity;
        private readonly Dictionary<string, LabItemStub> _items = new Dictionary<string, LabItemStub>();

        public int Count => _items.Count;

        public InventoryStub(int maxCapacity = 50)
        {
            _maxCapacity = maxCapacity;
        }

        public bool AddItem(LabItemStub item)
        {
            if (item == null || _items.ContainsKey(item.ItemId) || _items.Count >= _maxCapacity)
                return false;
            _items[item.ItemId] = item;
            return true;
        }

        public bool RemoveItem(string itemId)
        {
            return _items.Remove(itemId);
        }

        public bool Contains(string itemId)
        {
            return _items.ContainsKey(itemId);
        }
    }

    public class LabItemStub
    {
        public string ItemId;
        public string DisplayName;
    }
}
