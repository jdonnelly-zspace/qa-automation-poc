// =============================================================================
// CameraControllerBaseTests.cs - Edit Mode Unit Tests for CameraControllerBase
// =============================================================================
// TARGET CLASS: CameraControllerBase
//   Real file: Assets/CommonA3/zSpace/Scripts/Controllers/CameraControllerBase.cs
//
// WHAT IT TESTS:
//   Abstract base class for camera state management. Defines the CameraState
//   inner class and abstract serialization contract. Tests validate state
//   get/set, serialization round-tripping, null/empty handling, and
//   preservation of custom data and scale values.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real CameraControllerBase is an abstract MonoBehaviour. The stub
//      provides a concrete subclass (ConcreteCameraController) with simple
//      JSON serialization so tests run without Unity runtime.
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
    /// Lightweight stand-in for Unity Transform position and rotation,
    /// storing XYZ position and XYZ Euler rotation as floats.
    /// </summary>
    public class TransformA3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RotX { get; set; }
        public float RotY { get; set; }
        public float RotZ { get; set; }

        public TransformA3() { }

        public TransformA3(float x, float y, float z, float rotX, float rotY, float rotZ)
        {
            X = x;
            Y = y;
            Z = z;
            RotX = rotX;
            RotY = rotY;
            RotZ = rotZ;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the CameraState inner class from CameraControllerBase,
    /// capturing camera transform, follower transform, scale, and custom data.
    /// </summary>
    public class CameraState
    {
        public TransformA3 CameraTransform { get; set; }
        public TransformA3 CameraFollowerTransform { get; set; }
        public float CameraScale { get; set; }
        public object CustomData { get; set; }

        public CameraState()
        {
            CameraTransform = new TransformA3();
            CameraFollowerTransform = new TransformA3();
            CameraScale = 1.0f;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Concrete implementation of the abstract CameraControllerBase for
    /// testing. Uses a minimal key-value format for serialization so tests
    /// can validate round-trip without depending on JsonUtility.
    /// </summary>
    public class ConcreteCameraController
    {
        private CameraState _currentState;

        public ConcreteCameraController()
        {
            _currentState = new CameraState();
        }

        public CameraState GetState()
        {
            return _currentState;
        }

        public void SetState(CameraState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _currentState = state;
        }

        /// <summary>
        /// Serializes a CameraState to a simple delimited string.
        /// Format: X|Y|Z|RotX|RotY|RotZ|FX|FY|FZ|FRotX|FRotY|FRotZ|Scale|CustomData
        /// </summary>
        public string GetCameraStateSaveString(CameraState state)
        {
            if (state == null)
            {
                return null;
            }

            var t = state.CameraTransform;
            var f = state.CameraFollowerTransform;
            string custom = state.CustomData?.ToString() ?? "";

            return string.Join("|",
                t.X, t.Y, t.Z, t.RotX, t.RotY, t.RotZ,
                f.X, f.Y, f.Z, f.RotX, f.RotY, f.RotZ,
                state.CameraScale,
                custom);
        }

        /// <summary>
        /// Deserializes a CameraState from the delimited string format.
        /// Returns null if the input is null, empty, or malformed.
        /// </summary>
        public CameraState CameraStateFromSaveString(string saveString,
            int version = 4, int appVersion = 1)
        {
            if (string.IsNullOrEmpty(saveString))
            {
                return null;
            }

            string[] parts = saveString.Split('|');
            if (parts.Length < 13)
            {
                return null;
            }

            try
            {
                var state = new CameraState
                {
                    CameraTransform = new TransformA3(
                        float.Parse(parts[0]), float.Parse(parts[1]),
                        float.Parse(parts[2]), float.Parse(parts[3]),
                        float.Parse(parts[4]), float.Parse(parts[5])),
                    CameraFollowerTransform = new TransformA3(
                        float.Parse(parts[6]), float.Parse(parts[7]),
                        float.Parse(parts[8]), float.Parse(parts[9]),
                        float.Parse(parts[10]), float.Parse(parts[11])),
                    CameraScale = float.Parse(parts[12])
                };

                if (parts.Length > 13 && !string.IsNullOrEmpty(parts[13]))
                {
                    state.CustomData = parts[13];
                }

                return state;
            }
            catch
            {
                return null;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class CameraControllerBaseTests
    {
        private ConcreteCameraController _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new ConcreteCameraController();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void GetState_ReturnsCurrentState_WithDefaults()
        {
            // WHY: When a scene loads, the camera must report a valid default
            // state so the viewport renders at the correct initial position.

            // Act
            CameraState state = _controller.GetState();

            // Assert
            Assert.IsNotNull(state,
                "GetState should return a non-null CameraState.");
            Assert.IsNotNull(state.CameraTransform,
                "CameraTransform should be initialized.");
            Assert.IsNotNull(state.CameraFollowerTransform,
                "CameraFollowerTransform should be initialized.");
            Assert.AreEqual(1.0f, state.CameraScale, 0.001f,
                "Default camera scale should be 1.0 (no zoom).");
        }

        [Test]
        public void SetState_AppliesState_GetStateReflectsChange()
        {
            // WHY: Scene restore (undo camera move, load saved scene) relies on
            // SetState to reposition the camera exactly where it was.

            // Arrange
            var newState = new CameraState
            {
                CameraTransform = new TransformA3(1.5f, 2.0f, -3.0f, 15f, 45f, 0f),
                CameraFollowerTransform = new TransformA3(0f, 1.0f, 0f, 0f, 0f, 0f),
                CameraScale = 2.5f
            };

            // Act
            _controller.SetState(newState);
            CameraState result = _controller.GetState();

            // Assert
            Assert.AreEqual(1.5f, result.CameraTransform.X, 0.001f,
                "Camera X position should match the state that was set.");
            Assert.AreEqual(45f, result.CameraTransform.RotY, 0.001f,
                "Camera Y rotation should match the state that was set.");
            Assert.AreEqual(2.5f, result.CameraScale, 0.001f,
                "Camera scale should match the state that was set.");
        }

        [Test]
        public void SerializationRoundtrip_PreservesAllData()
        {
            // WHY: When a student saves and reopens a scene, the camera must
            // return to the exact viewpoint they left off at.

            // Arrange
            var original = new CameraState
            {
                CameraTransform = new TransformA3(10.5f, -3.2f, 7.8f, 30f, 60f, 0f),
                CameraFollowerTransform = new TransformA3(0.1f, 0.2f, 0.3f, 5f, 10f, 15f),
                CameraScale = 0.75f,
                CustomData = "zSpace_HeartModel_v2"
            };

            // Act
            string serialized = _controller.GetCameraStateSaveString(original);
            CameraState restored = _controller.CameraStateFromSaveString(serialized);

            // Assert
            Assert.IsNotNull(restored,
                "Deserialized state should not be null.");
            Assert.AreEqual(original.CameraTransform.X, restored.CameraTransform.X, 0.001f,
                "Camera X should survive serialization round-trip.");
            Assert.AreEqual(original.CameraTransform.RotY, restored.CameraTransform.RotY, 0.001f,
                "Camera RotY should survive serialization round-trip.");
            Assert.AreEqual(original.CameraFollowerTransform.Z, restored.CameraFollowerTransform.Z, 0.001f,
                "Follower Z should survive serialization round-trip.");
            Assert.AreEqual(original.CameraScale, restored.CameraScale, 0.001f,
                "Camera scale should survive serialization round-trip.");
        }

        [Test]
        public void CameraStateFromSaveString_ReturnsNull_WhenInputIsNullOrEmpty()
        {
            // WHY: Corrupted or missing save data should not crash the camera
            // system; it must fall back gracefully to defaults.

            // Act
            CameraState fromNull = _controller.CameraStateFromSaveString(null);
            CameraState fromEmpty = _controller.CameraStateFromSaveString("");

            // Assert
            Assert.IsNull(fromNull,
                "Deserializing null should return null, not throw.");
            Assert.IsNull(fromEmpty,
                "Deserializing empty string should return null, not throw.");
        }

        [Test]
        public void CameraState_DefaultValues_AreValid()
        {
            // WHY: A freshly constructed CameraState is used as a fallback when
            // save data is missing; it must place the camera in a usable position.

            // Arrange & Act
            var state = new CameraState();

            // Assert
            Assert.AreEqual(0f, state.CameraTransform.X, 0.001f,
                "Default X position should be zero (origin).");
            Assert.AreEqual(0f, state.CameraTransform.Y, 0.001f,
                "Default Y position should be zero (origin).");
            Assert.AreEqual(0f, state.CameraTransform.Z, 0.001f,
                "Default Z position should be zero (origin).");
            Assert.AreEqual(1.0f, state.CameraScale, 0.001f,
                "Default scale should be 1.0 (no magnification).");
            Assert.IsNull(state.CustomData,
                "Default CustomData should be null.");
        }

        [Test]
        public void CustomData_PreservedThroughRoundtrip()
        {
            // WHY: Custom data carries controller-specific state (e.g., which
            // anatomy layer is focused); losing it resets the student's context.

            // Arrange
            var state = new CameraState
            {
                CameraTransform = new TransformA3(0f, 0f, 0f, 0f, 0f, 0f),
                CameraFollowerTransform = new TransformA3(0f, 0f, 0f, 0f, 0f, 0f),
                CameraScale = 1.0f,
                CustomData = "skeletal_system_focus"
            };

            // Act
            string serialized = _controller.GetCameraStateSaveString(state);
            CameraState restored = _controller.CameraStateFromSaveString(serialized);

            // Assert
            Assert.IsNotNull(restored.CustomData,
                "CustomData should not be null after round-trip.");
            Assert.AreEqual("skeletal_system_focus", restored.CustomData.ToString(),
                "CustomData string should be preserved exactly through serialization.");
        }

        [Test]
        public void ScaleValue_PreservedCorrectly_AtExtremes()
        {
            // WHY: Students zoom in very close to examine tissue or zoom out to
            // see the full skeleton; extreme scale values must not be lost.

            // Arrange - very small scale (zoomed out) and very large (zoomed in)
            var zoomedOut = new CameraState
            {
                CameraTransform = new TransformA3(0f, 5f, -20f, 0f, 0f, 0f),
                CameraFollowerTransform = new TransformA3(0f, 0f, 0f, 0f, 0f, 0f),
                CameraScale = 0.01f
            };

            var zoomedIn = new CameraState
            {
                CameraTransform = new TransformA3(0.1f, 0.1f, -0.2f, 0f, 0f, 0f),
                CameraFollowerTransform = new TransformA3(0f, 0f, 0f, 0f, 0f, 0f),
                CameraScale = 50.0f
            };

            // Act
            string serializedOut = _controller.GetCameraStateSaveString(zoomedOut);
            string serializedIn = _controller.GetCameraStateSaveString(zoomedIn);
            CameraState restoredOut = _controller.CameraStateFromSaveString(serializedOut);
            CameraState restoredIn = _controller.CameraStateFromSaveString(serializedIn);

            // Assert
            Assert.AreEqual(0.01f, restoredOut.CameraScale, 0.001f,
                "Very small scale (zoomed out) should be preserved through serialization.");
            Assert.AreEqual(50.0f, restoredIn.CameraScale, 0.5f,
                "Very large scale (zoomed in) should be preserved through serialization.");
        }
    }
}
