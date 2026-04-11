// =============================================================================
// CameraCubeControllerTests.cs - Edit Mode Unit Tests for CameraCubeController
// =============================================================================
// TARGET CLASS: CameraCubeController
//   Real file: Assets/CommonA3/zSpace/Scripts/Tools/CameraTool/CameraCubeController.cs
//
// WHAT IT TESTS:
//   Concrete camera controller that manages JSON serialization of camera state
//   and maps named view buttons (Front, Right, Top, etc.) to specific rotation
//   angles. Tests validate serialization roundtrips, null/empty deserialization,
//   view-button rotation mapping, and state get/set consistency.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace the using directives with the real namespaces.
//   3. The real CameraCubeController interacts with Unity's Transform and
//      camera system. The stub here exercises only the serialization and
//      mapping logic without Unity runtime.
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
    /// Represents a snapshot of the camera's position, rotation, and scale.
    /// </summary>
    public class CameraState
    {
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosZ { get; set; }
        public float RotX { get; set; }
        public float RotY { get; set; }
        public float RotZ { get; set; }
        public float Scale { get; set; } = 1.0f;
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight stand-in for the real CameraCubeController. Manages camera
    /// state serialization and view-button-to-rotation mapping without Unity
    /// Transform dependencies.
    /// </summary>
    public class CameraCubeControllerStub
    {
        private CameraState _currentState = new CameraState();

        public readonly Dictionary<string, (float rotX, float rotY, float rotZ)> ButtonRotationMap =
            new Dictionary<string, (float, float, float)>
            {
                { "Front",      (0f,    0f,   0f)   },
                { "FrontRight", (0f,   45f,   0f)   },
                { "Right",      (0f,   90f,   0f)   },
                { "BackRight",  (0f,  135f,   0f)   },
                { "Back",       (0f,  180f,   0f)   },
                { "BackLeft",   (0f,  225f,   0f)   },
                { "Left",       (0f,  270f,   0f)   },
                { "FrontLeft",  (0f,  315f,   0f)   },
                { "Top",        (-90f,  0f,   0f)   },
                { "Bottom",     (90f,   0f,   0f)   }
            };

        public string SerializeState(CameraState state)
        {
            if (state == null)
            {
                return "{}";
            }

            // Minimal JSON serialization without external dependencies
            return $"{{\"PosX\":{state.PosX},\"PosY\":{state.PosY},\"PosZ\":{state.PosZ}," +
                   $"\"RotX\":{state.RotX},\"RotY\":{state.RotY},\"RotZ\":{state.RotZ}," +
                   $"\"Scale\":{state.Scale}}}";
        }

        public CameraState DeserializeState(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new CameraState();
            }

            // Minimal parser for the known JSON format
            var state = new CameraState();
            try
            {
                state.PosX = ExtractFloat(json, "PosX");
                state.PosY = ExtractFloat(json, "PosY");
                state.PosZ = ExtractFloat(json, "PosZ");
                state.RotX = ExtractFloat(json, "RotX");
                state.RotY = ExtractFloat(json, "RotY");
                state.RotZ = ExtractFloat(json, "RotZ");
                state.Scale = ExtractFloat(json, "Scale");
            }
            catch
            {
                return new CameraState();
            }

            return state;
        }

        public (float rotX, float rotY, float rotZ) GetRotationForButton(string button)
        {
            if (ButtonRotationMap.TryGetValue(button, out var rotation))
            {
                return rotation;
            }

            return (0f, 0f, 0f);
        }

        public void SetState(CameraState state)
        {
            _currentState = state ?? new CameraState();
        }

        public CameraState GetState()
        {
            return _currentState;
        }

        private float ExtractFloat(string json, string key)
        {
            string search = $"\"{key}\":";
            int start = json.IndexOf(search, StringComparison.Ordinal);
            if (start < 0) return 0f;
            start += search.Length;
            int end = json.IndexOfAny(new[] { ',', '}' }, start);
            if (end < 0) return 0f;
            return float.Parse(json.Substring(start, end - start),
                System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class CameraCubeControllerTests
    {
        private CameraCubeControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new CameraCubeControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void SerializationRoundtrip_PreservesPosition()
        {
            // WHY: When a student bookmarks a camera view of the heart, the exact
            // position must survive save/load so they return to the same vantage point.

            // Arrange
            var original = new CameraState { PosX = 1.5f, PosY = -2.3f, PosZ = 4.7f };

            // Act
            string json = _controller.SerializeState(original);
            CameraState restored = _controller.DeserializeState(json);

            // Assert
            Assert.AreEqual(original.PosX, restored.PosX, 0.001f,
                "PosX must survive serialization roundtrip for accurate camera restoration.");
            Assert.AreEqual(original.PosY, restored.PosY, 0.001f,
                "PosY must survive serialization roundtrip for accurate camera restoration.");
            Assert.AreEqual(original.PosZ, restored.PosZ, 0.001f,
                "PosZ must survive serialization roundtrip for accurate camera restoration.");
        }

        [Test]
        public void SerializationRoundtrip_PreservesRotation()
        {
            // WHY: Rotation determines which side of the anatomy model the student
            // sees; losing rotation data would disorient them on reload.

            // Arrange
            var original = new CameraState { RotX = 45.0f, RotY = 90.0f, RotZ = 15.0f };

            // Act
            string json = _controller.SerializeState(original);
            CameraState restored = _controller.DeserializeState(json);

            // Assert
            Assert.AreEqual(original.RotX, restored.RotX, 0.001f,
                "RotX must survive serialization roundtrip.");
            Assert.AreEqual(original.RotY, restored.RotY, 0.001f,
                "RotY must survive serialization roundtrip.");
            Assert.AreEqual(original.RotZ, restored.RotZ, 0.001f,
                "RotZ must survive serialization roundtrip.");
        }

        [Test]
        public void SerializationRoundtrip_PreservesScale()
        {
            // WHY: Scale affects model magnification; if scale is lost the student
            // may see a microscopic or oversized organ on reload.

            // Arrange
            var original = new CameraState { Scale = 2.5f };

            // Act
            string json = _controller.SerializeState(original);
            CameraState restored = _controller.DeserializeState(json);

            // Assert
            Assert.AreEqual(original.Scale, restored.Scale, 0.001f,
                "Scale must survive serialization roundtrip for consistent magnification.");
        }

        [Test]
        public void DeserializeState_WithNull_ReturnsDefaultState()
        {
            // WHY: A newly created lesson has no saved camera state; deserialization
            // must return safe defaults rather than throwing an exception.

            // Act
            CameraState result = _controller.DeserializeState(null);

            // Assert
            Assert.IsNotNull(result,
                "DeserializeState(null) should return a non-null default CameraState.");
            Assert.AreEqual(0f, result.PosX, 0.001f,
                "Default PosX should be 0.");
            Assert.AreEqual(1.0f, result.Scale, 0.001f,
                "Default Scale should be 1.0.");
        }

        [Test]
        public void DeserializeState_WithEmptyString_ReturnsDefaultState()
        {
            // WHY: Corrupted or empty JSON from storage must not crash the
            // application; a safe default state keeps the lesson running.

            // Act
            CameraState result = _controller.DeserializeState("");

            // Assert
            Assert.IsNotNull(result,
                "DeserializeState('') should return a non-null default CameraState.");
            Assert.AreEqual(0f, result.RotX, 0.001f,
                "Default RotX should be 0.");
        }

        [Test]
        public void FrontButton_MapsToZeroRotation()
        {
            // WHY: The Front view is the canonical starting orientation for anatomy
            // models; it must map to identity rotation (0, 0, 0).

            // Act
            var rotation = _controller.GetRotationForButton("Front");

            // Assert
            Assert.AreEqual(0f, rotation.rotX, 0.001f, "Front rotX should be 0.");
            Assert.AreEqual(0f, rotation.rotY, 0.001f, "Front rotY should be 0.");
            Assert.AreEqual(0f, rotation.rotZ, 0.001f, "Front rotZ should be 0.");
        }

        [Test]
        public void RightButton_MapsTo90DegreeYRotation()
        {
            // WHY: Clicking "Right" must rotate the camera 90 degrees around Y so
            // students see the right lateral view of the skeleton.

            // Act
            var rotation = _controller.GetRotationForButton("Right");

            // Assert
            Assert.AreEqual(0f, rotation.rotX, 0.001f, "Right rotX should be 0.");
            Assert.AreEqual(90f, rotation.rotY, 0.001f, "Right rotY should be 90.");
            Assert.AreEqual(0f, rotation.rotZ, 0.001f, "Right rotZ should be 0.");
        }

        [Test]
        public void TopButton_MapsToNegative90DegreeXRotation()
        {
            // WHY: The Top view looks down at the model from above; this requires
            // a -90 degree pitch so the superior aspect is visible.

            // Act
            var rotation = _controller.GetRotationForButton("Top");

            // Assert
            Assert.AreEqual(-90f, rotation.rotX, 0.001f, "Top rotX should be -90.");
            Assert.AreEqual(0f, rotation.rotY, 0.001f, "Top rotY should be 0.");
            Assert.AreEqual(0f, rotation.rotZ, 0.001f, "Top rotZ should be 0.");
        }

        [Test]
        public void AllTenViewButtons_AreMapped()
        {
            // WHY: The camera cube has 10 clickable faces/edges; every one must have
            // a defined rotation to avoid missing-mapping bugs in the UI.

            // Assert
            Assert.AreEqual(10, _controller.ButtonRotationMap.Count,
                "ButtonRotationMap should contain exactly 10 view entries.");

            string[] expectedButtons = {
                "Front", "FrontRight", "Right", "BackRight", "Back",
                "BackLeft", "Left", "FrontLeft", "Top", "Bottom"
            };

            foreach (string button in expectedButtons)
            {
                Assert.IsTrue(_controller.ButtonRotationMap.ContainsKey(button),
                    $"ButtonRotationMap should contain an entry for '{button}'.");
            }
        }

        [Test]
        public void SetState_GetState_Roundtrip()
        {
            // WHY: Internal state management must be consistent; SetState followed
            // by GetState must return the exact same object reference.

            // Arrange
            var state = new CameraState
            {
                PosX = 3.0f, PosY = 1.0f, PosZ = -5.0f,
                RotX = 10.0f, RotY = 20.0f, RotZ = 30.0f,
                Scale = 1.5f
            };

            // Act
            _controller.SetState(state);
            CameraState retrieved = _controller.GetState();

            // Assert
            Assert.AreSame(state, retrieved,
                "GetState should return the same CameraState instance that was passed to SetState.");
        }
    }
}
