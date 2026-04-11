// =============================================================================
// ZViewManagerTests.cs - Edit Mode Unit Tests for ZViewManager
// =============================================================================
// TARGET CLASS: ZViewManager
//   Real file: Assets/CommonA3/zSpace/Scripts/Utilities/ZViewManager.cs
//
// WHAT IT TESTS:
//   Manages the zView augmented reality overlay by configuring layer masks for
//   AR ignore layers and AR environment layers. Tests validate bitmask
//   computation, connection lifecycle, and layer classification logic.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace the using directives with the real namespaces.
//   3. The real ZViewManager interacts with the zView SDK and Unity's layer
//      system. The stub here exercises only the layer mask computation and
//      connection state logic without native plugin dependencies.
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
    /// Lightweight stand-in for the real ZViewManager. Handles layer mask
    /// computation and AR connection state without the zView SDK.
    /// </summary>
    public class ZViewManagerStub
    {
        public List<string> ArIgnoreLayers { get; private set; } = new List<string>();
        public List<string> ArEnvironmentLayers { get; private set; } = new List<string>();
        public bool IsConnected { get; private set; }

        public int ComputeLayerMask(List<string> layerNames, Dictionary<string, int> layerMap)
        {
            if (layerNames == null || layerMap == null)
            {
                return 0;
            }

            int mask = 0;
            foreach (string layerName in layerNames)
            {
                if (layerMap.TryGetValue(layerName, out int layerIndex))
                {
                    mask |= (1 << layerIndex);
                }
            }

            return mask;
        }

        public void Connect()
        {
            IsConnected = true;
        }

        public void Disconnect()
        {
            IsConnected = false;
        }

        public bool IsLayerIgnored(string layerName)
        {
            return ArIgnoreLayers.Contains(layerName);
        }

        public bool IsLayerEnvironment(string layerName)
        {
            return ArEnvironmentLayers.Contains(layerName);
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ZViewManagerTests
    {
        private ZViewManagerStub _zViewManager;
        private Dictionary<string, int> _layerMap;

        [SetUp]
        public void SetUp()
        {
            _zViewManager = new ZViewManagerStub();

            // Simulate a typical Unity layer map for a zSpace anatomy project
            _layerMap = new Dictionary<string, int>
            {
                { "Default",        0 },
                { "UI",             5 },
                { "ARIgnore",       8 },
                { "AREnvironment",  9 },
                { "Anatomy",       10 },
                { "Labels",        11 }
            };
        }

        [TearDown]
        public void TearDown()
        {
            _zViewManager = null;
            _layerMap = null;
        }

        [Test]
        public void ComputeLayerMask_SingleLayer_ReturnsBitmaskForThatLayer()
        {
            // WHY: When configuring the AR overlay to ignore a single layer (e.g.,
            // UI), the bitmask must set exactly that layer's bit.

            // Arrange
            var layers = new List<string> { "UI" };

            // Act
            int mask = _zViewManager.ComputeLayerMask(layers, _layerMap);

            // Assert
            int expected = 1 << 5; // UI is layer 5
            Assert.AreEqual(expected, mask,
                "ComputeLayerMask with a single layer should return a bitmask with only that layer's bit set.");
        }

        [Test]
        public void ComputeLayerMask_MultipleLayers_ReturnsCombinedBitmask()
        {
            // WHY: The AR overlay typically ignores several layers at once (UI,
            // labels, cursors); all bits must be OR-ed together correctly.

            // Arrange
            var layers = new List<string> { "ARIgnore", "AREnvironment", "Labels" };

            // Act
            int mask = _zViewManager.ComputeLayerMask(layers, _layerMap);

            // Assert
            int expected = (1 << 8) | (1 << 9) | (1 << 11);
            Assert.AreEqual(expected, mask,
                "ComputeLayerMask with multiple layers should OR all layer bits together.");
        }

        [Test]
        public void ComputeLayerMask_UnknownLayerName_ReturnsZero()
        {
            // WHY: If a layer name is misspelled or removed from the project, the
            // mask must not include random bits; returning 0 is the safe fallback.

            // Arrange
            var layers = new List<string> { "NonExistentLayer" };

            // Act
            int mask = _zViewManager.ComputeLayerMask(layers, _layerMap);

            // Assert
            Assert.AreEqual(0, mask,
                "ComputeLayerMask should return 0 when no layer names match the layer map.");
        }

        [Test]
        public void IsLayerIgnored_ReturnsTrue_ForConfiguredIgnoreLayers()
        {
            // WHY: Layers like UI and cursor overlays must be excluded from the AR
            // camera; IsLayerIgnored drives the rendering pipeline decision.

            // Arrange
            _zViewManager.ArIgnoreLayers.Add("UI");
            _zViewManager.ArIgnoreLayers.Add("Labels");

            // Act & Assert
            Assert.IsTrue(_zViewManager.IsLayerIgnored("UI"),
                "IsLayerIgnored should return true for a layer in the ArIgnoreLayers list.");
            Assert.IsTrue(_zViewManager.IsLayerIgnored("Labels"),
                "IsLayerIgnored should return true for all configured ignore layers.");
        }

        [Test]
        public void IsLayerIgnored_ReturnsFalse_ForNonConfiguredLayers()
        {
            // WHY: The Anatomy layer must render in AR so students see the 3D model
            // through the overlay; it should never be accidentally ignored.

            // Arrange
            _zViewManager.ArIgnoreLayers.Add("UI");

            // Act & Assert
            Assert.IsFalse(_zViewManager.IsLayerIgnored("Anatomy"),
                "IsLayerIgnored should return false for layers not in the ArIgnoreLayers list.");
            Assert.IsFalse(_zViewManager.IsLayerIgnored("AREnvironment"),
                "IsLayerIgnored should return false for environment layers not in the ignore list.");
        }

        [Test]
        public void Connect_SetsIsConnectedToTrue()
        {
            // WHY: When the zView display is plugged in, the manager must transition
            // to connected state so the AR rendering pipeline activates.

            // Act
            _zViewManager.Connect();

            // Assert
            Assert.IsTrue(_zViewManager.IsConnected,
                "Connect() should set IsConnected to true so AR rendering activates.");
        }

        [Test]
        public void Disconnect_ClearsIsConnected()
        {
            // WHY: When the zView display is unplugged mid-lesson, the manager must
            // cleanly transition back to disconnected to avoid rendering errors.

            // Arrange
            _zViewManager.Connect();
            Assert.IsTrue(_zViewManager.IsConnected, "Precondition: should be connected.");

            // Act
            _zViewManager.Disconnect();

            // Assert
            Assert.IsFalse(_zViewManager.IsConnected,
                "Disconnect() should set IsConnected to false to disable AR rendering.");
        }

        [Test]
        public void DefaultState_IsDisconnected()
        {
            // WHY: On startup, no zView display is assumed to be connected; the
            // manager must default to disconnected to avoid premature AR rendering.

            // Assert
            Assert.IsFalse(_zViewManager.IsConnected,
                "ZViewManager should default to disconnected state on construction.");
            Assert.AreEqual(0, _zViewManager.ArIgnoreLayers.Count,
                "ArIgnoreLayers should be empty by default.");
            Assert.AreEqual(0, _zViewManager.ArEnvironmentLayers.Count,
                "ArEnvironmentLayers should be empty by default.");
        }
    }
}
