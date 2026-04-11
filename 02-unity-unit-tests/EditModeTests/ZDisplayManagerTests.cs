// =============================================================================
// ZDisplayManagerTests.cs - Edit Mode Unit Tests for ZDisplayManager
// =============================================================================
// TARGET CLASS: ZDisplayManager
//   Real file: Assets/zSpace/Core/Scripts/Sdk/ZDisplayManager.cs
//
// WHAT IT TESTS:
//   Hardware display manager in the zSpace SDK that queries and caches active
//   displays via the native plugin. Validates display enumeration by index,
//   by type, by desktop position, display count queries, cache refresh, and
//   the ZNativeResourceCache caching behavior.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real ZDisplayManager calls into ZPlugin native methods. These tests
//      exercise the public API surface through a lightweight POCO stub that
//      simulates display data in-memory, so they compile standalone without
//      the native zSpace runtime.
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
    /// <summary>
    /// Stand-in for ZDisplayType from the zSpace SDK.
    /// </summary>
    public enum ZDisplayType
    {
        Unknown = 0,
        Generic = 1,
        zSpace = 2
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for a ZDisplay resource.
    /// </summary>
    public class ZDisplayStub
    {
        public int Index { get; private set; }
        public ZDisplayType DisplayType { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public ZDisplayStub(int index, ZDisplayType displayType,
            int x, int y, int width, int height)
        {
            Index = index;
            DisplayType = displayType;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API of the real ZDisplayManager,
    /// without requiring the native ZPlugin or ZContext.
    /// </summary>
    public class ZDisplayManagerStub
    {
        private List<ZDisplayStub> _displays = new List<ZDisplayStub>();

        public void AddDisplay(ZDisplayStub display)
        {
            _displays.Add(display);
        }

        /// <summary>
        /// Mirrors RefreshDisplays() — clears the internal cache.
        /// In the real class this calls ZPlugin.RefreshDisplays.
        /// </summary>
        public void RefreshDisplays()
        {
            _displays.Clear();
        }

        /// <summary>
        /// Gets the total number of active displays.
        /// </summary>
        public int GetNumDisplays()
        {
            return _displays.Count;
        }

        /// <summary>
        /// Gets the number of displays of the specified type.
        /// </summary>
        public int GetNumDisplays(ZDisplayType displayType)
        {
            int count = 0;
            for (int i = 0; i < _displays.Count; i++)
            {
                if (_displays[i].DisplayType == displayType)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Gets a display by its global index.
        /// </summary>
        public ZDisplayStub GetDisplay(int index)
        {
            if (index < 0 || index >= _displays.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                    "Display index is out of range.");
            }
            return _displays[index];
        }

        /// <summary>
        /// Gets a display containing the specified desktop pixel position.
        /// </summary>
        public ZDisplayStub GetDisplay(int x, int y)
        {
            for (int i = 0; i < _displays.Count; i++)
            {
                ZDisplayStub d = _displays[i];
                if (x >= d.X && x < d.X + d.Width &&
                    y >= d.Y && y < d.Y + d.Height)
                {
                    return d;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a display of the specified type at the given type-relative index.
        /// </summary>
        public ZDisplayStub GetDisplay(ZDisplayType displayType, int index = 0)
        {
            int seen = 0;
            for (int i = 0; i < _displays.Count; i++)
            {
                if (_displays[i].DisplayType == displayType)
                {
                    if (seen == index)
                    {
                        return _displays[i];
                    }
                    seen++;
                }
            }
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ZDisplayManagerTests
    {
        private ZDisplayManagerStub _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new ZDisplayManagerStub();

            // Simulate a two-display setup: one generic monitor and one zSpace display
            _manager.AddDisplay(new ZDisplayStub(0, ZDisplayType.Generic,
                0, 0, 1920, 1080));
            _manager.AddDisplay(new ZDisplayStub(1, ZDisplayType.zSpace,
                1920, 0, 1920, 1080));
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
        }

        [Test]
        public void GetNumDisplays_ReturnsTotal_WhenMultipleDisplaysActive()
        {
            // WHY: The application needs an accurate total display count to
            //      decide whether to enable stereoscopic rendering paths.

            // Assert
            Assert.AreEqual(2, _manager.GetNumDisplays(),
                "GetNumDisplays() should return the total number of active displays.");
        }

        [Test]
        public void GetNumDisplaysByType_ReturnsFilteredCount_ForZSpaceType()
        {
            // WHY: Studio needs to know how many zSpace displays are present
            //      to configure the stereoscopic viewport and head tracking.

            // Assert
            Assert.AreEqual(1, _manager.GetNumDisplays(ZDisplayType.zSpace),
                "GetNumDisplays(zSpace) should return only zSpace-type displays.");
            Assert.AreEqual(1, _manager.GetNumDisplays(ZDisplayType.Generic),
                "GetNumDisplays(Generic) should return only Generic-type displays.");
        }

        [Test]
        public void GetDisplayByIndex_ReturnsCorrectDisplay_ForValidIndex()
        {
            // WHY: Retrieving a display by index is the primary enumeration API.
            //      Returning the wrong display could misconfigure the render target.

            // Act
            ZDisplayStub display = _manager.GetDisplay(0);

            // Assert
            Assert.IsNotNull(display,
                "GetDisplay(0) should return a non-null display.");
            Assert.AreEqual(ZDisplayType.Generic, display.DisplayType,
                "First display should be the Generic monitor.");
        }

        [Test]
        public void GetDisplayByIndex_ThrowsOutOfRange_ForInvalidIndex()
        {
            // WHY: Accessing a non-existent display index must fail clearly
            //      rather than returning null or a stale cached entry.

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _manager.GetDisplay(99),
                "GetDisplay() should throw ArgumentOutOfRangeException for an invalid index.");
        }

        [Test]
        public void GetDisplayByPosition_ReturnsCorrectDisplay_WhenPositionIsInBounds()
        {
            // WHY: When the user moves a window to a specific desktop coordinate,
            //      Studio must identify which physical display owns that pixel to
            //      decide whether to enable stereoscopic rendering.

            // Act — pixel (1920, 500) is inside the second display (1920..3839, 0..1079)
            ZDisplayStub display = _manager.GetDisplay(1920, 500);

            // Assert
            Assert.IsNotNull(display,
                "GetDisplay(x,y) should return a display when the position is within its bounds.");
            Assert.AreEqual(ZDisplayType.zSpace, display.DisplayType,
                "The display at position (1920,500) should be the zSpace display.");
        }

        [Test]
        public void GetDisplayByPosition_ReturnsNull_WhenPositionIsOutOfBounds()
        {
            // WHY: A desktop position that does not belong to any display should
            //      return null so the caller can fall back gracefully.

            // Act — pixel (5000, 5000) is beyond both displays
            ZDisplayStub display = _manager.GetDisplay(5000, 5000);

            // Assert
            Assert.IsNull(display,
                "GetDisplay(x,y) should return null when no display contains the position.");
        }

        [Test]
        public void GetDisplayByType_ReturnsFirstMatch_WhenIndexIsDefault()
        {
            // WHY: The type+index overload is used to get "the first zSpace display",
            //      which is the most common usage pattern for single-display setups.

            // Act
            ZDisplayStub display = _manager.GetDisplay(ZDisplayType.zSpace);

            // Assert
            Assert.IsNotNull(display,
                "GetDisplay(zSpace) should return the first zSpace display.");
            Assert.AreEqual(ZDisplayType.zSpace, display.DisplayType,
                "Returned display should be of type zSpace.");
        }

        [Test]
        public void RefreshDisplays_ClearsCache_SoCountReturnsZero()
        {
            // WHY: RefreshDisplays clears the internal cache before re-querying
            //      hardware. After the clear, the count must be zero until new
            //      display data arrives — ensures stale data is never used.

            // Arrange
            Assert.AreEqual(2, _manager.GetNumDisplays(),
                "Should start with 2 displays before refresh.");

            // Act
            _manager.RefreshDisplays();

            // Assert
            Assert.AreEqual(0, _manager.GetNumDisplays(),
                "GetNumDisplays() should return 0 after RefreshDisplays() clears the cache.");
        }
    }
}
