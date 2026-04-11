// =============================================================================
// CircularLineRendererControllerTests.cs - Edit Mode Unit Tests
// =============================================================================
// TARGET CLASS: CircularLineRendererController
//   Real file: Assets/StudioA3/Scripts/Tools/MoveTool/CircularLineRendererController.cs
//
// WHAT IT TESTS:
//   Controller that draws a circular LineRenderer for the move tool's
//   rotation gizmo. Validates circle geometry calculation (position count,
//   radius, angle distribution), show/hide visibility toggling, and
//   scale/position adjustment based on viewer scale.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real CircularLineRendererController is a MonoBehaviour that uses
//      a Unity LineRenderer. These tests exercise the geometry math through
//      a lightweight POCO stub that records positions in a list.
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
    /// Minimal stand-in for UnityEngine.Vector3.
    /// </summary>
    public struct SimpleVector3
    {
        public float X;
        public float Y;
        public float Z;

        public SimpleVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static SimpleVector3 Zero => new SimpleVector3(0, 0, 0);
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API surface of
    /// CircularLineRendererController, without requiring MonoBehaviour
    /// or LineRenderer. Records generated positions for assertion.
    /// </summary>
    public class CircularLineRendererControllerStub
    {
        private bool _isEnabled;
        private List<SimpleVector3> _positions = new List<SimpleVector3>();
        private SimpleVector3 _localScale;
        private SimpleVector3 _localPosition;
        private bool _useWorldSpace;
        private int _positionCount;

        public bool IsEnabled => _isEnabled;
        public List<SimpleVector3> Positions => _positions;
        public SimpleVector3 LocalScale => _localScale;
        public SimpleVector3 LocalPosition => _localPosition;
        public bool UseWorldSpace => _useWorldSpace;
        public int PositionCount => _positionCount;

        public CircularLineRendererControllerStub()
        {
            _isEnabled = false;
            _useWorldSpace = true; // default, overridden by CreateCircularLine
        }

        /// <summary>
        /// Creates a circular line by calculating positions around a circle.
        /// Mirrors the real controller's geometry math.
        /// </summary>
        public void CreateCircularLine(int segments, float radius,
            float scaledViewerScale)
        {
            _positions.Clear();
            _useWorldSpace = false;
            _positionCount = segments + 2;
            float angle = 0f;

            _localScale = new SimpleVector3(
                scaledViewerScale, scaledViewerScale, scaledViewerScale);
            _localPosition = new SimpleVector3(
                -scaledViewerScale * radius, 0, 0);

            for (int i = 0; i < (segments + 2); i++)
            {
                float x = (float)(Math.Sin(angle * Math.PI / 180.0) * radius);
                float y = (float)(Math.Cos(angle * Math.PI / 180.0) * radius);
                float z = 0f;

                _positions.Add(new SimpleVector3(x, y, z));
                angle += (360f / segments);
            }
        }

        public void Show()
        {
            _isEnabled = true;
        }

        public void Hide()
        {
            _isEnabled = false;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class CircularLineRendererControllerTests
    {
        private CircularLineRendererControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new CircularLineRendererControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void CreateCircularLine_GeneratesCorrectPositionCount()
        {
            // WHY: The LineRenderer needs exactly (segments + 2) positions to
            //      form a closed circle. Wrong count = visible gap or overlap.

            // Arrange
            int segments = 36;

            // Act
            _controller.CreateCircularLine(segments, 1.0f, 1.0f);

            // Assert
            Assert.AreEqual(segments + 2, _controller.PositionCount,
                "Position count should be segments + 2 to close the circle.");
            Assert.AreEqual(segments + 2, _controller.Positions.Count,
                "Number of generated positions should match the position count.");
        }

        [Test]
        public void CreateCircularLine_SetsWorldSpaceFalse()
        {
            // WHY: The circle must render in local space so it moves with the
            //      target object. World space would leave the circle static.

            // Act
            _controller.CreateCircularLine(12, 1.0f, 1.0f);

            // Assert
            Assert.IsFalse(_controller.UseWorldSpace,
                "UseWorldSpace should be false so the circle follows the object.");
        }

        [Test]
        public void CreateCircularLine_ScalesTransformByViewerScale()
        {
            // WHY: The rotation gizmo must scale proportionally to the viewer
            //      scale so it appears the same size regardless of zoom level.

            // Arrange
            float viewerScale = 2.5f;

            // Act
            _controller.CreateCircularLine(12, 1.0f, viewerScale);

            // Assert
            Assert.AreEqual(viewerScale, _controller.LocalScale.X, 0.001f,
                "Local scale X should equal the scaled viewer scale.");
            Assert.AreEqual(viewerScale, _controller.LocalScale.Y, 0.001f,
                "Local scale Y should equal the scaled viewer scale.");
            Assert.AreEqual(viewerScale, _controller.LocalScale.Z, 0.001f,
                "Local scale Z should equal the scaled viewer scale.");
        }

        [Test]
        public void CreateCircularLine_OffsetsPositionByRadiusTimesScale()
        {
            // WHY: The circle is offset along the X axis by (-scale * radius)
            //      so it appears centered on the rotation pivot point.

            // Arrange
            float radius = 3.0f;
            float viewerScale = 2.0f;

            // Act
            _controller.CreateCircularLine(12, radius, viewerScale);

            // Assert
            float expectedX = -viewerScale * radius;
            Assert.AreEqual(expectedX, _controller.LocalPosition.X, 0.001f,
                "Local position X should be -(viewerScale * radius).");
            Assert.AreEqual(0f, _controller.LocalPosition.Y, 0.001f,
                "Local position Y should be 0.");
            Assert.AreEqual(0f, _controller.LocalPosition.Z, 0.001f,
                "Local position Z should be 0.");
        }

        [Test]
        public void CreateCircularLine_FirstPositionIsAtTopOfCircle()
        {
            // WHY: The first point starts at angle 0, where sin(0)=0 and cos(0)=1,
            //      meaning it should be at (0, radius, 0) -- the top of the circle.

            // Arrange
            float radius = 5.0f;

            // Act
            _controller.CreateCircularLine(36, radius, 1.0f);

            // Assert
            SimpleVector3 first = _controller.Positions[0];
            Assert.AreEqual(0f, first.X, 0.01f,
                "First position X should be ~0 (sin(0) * radius).");
            Assert.AreEqual(radius, first.Y, 0.01f,
                "First position Y should equal the radius (cos(0) * radius).");
            Assert.AreEqual(0f, first.Z, 0.01f,
                "All Z positions should be 0 for a flat circle in the XY plane.");
        }

        [Test]
        public void Show_EnablesRenderer_SoCircleIsDrawn()
        {
            // WHY: The rotation gizmo circle must be visible when the move tool
            //      is active so the user can see the rotation affordance.

            // Arrange
            Assert.IsFalse(_controller.IsEnabled,
                "Controller should start with renderer disabled.");

            // Act
            _controller.Show();

            // Assert
            Assert.IsTrue(_controller.IsEnabled,
                "Show() should enable the line renderer.");
        }

        [Test]
        public void Hide_DisablesRenderer_SoCircleDisappears()
        {
            // WHY: When the user switches away from the move tool, the rotation
            //      circle must disappear to avoid visual clutter.

            // Arrange
            _controller.Show();

            // Act
            _controller.Hide();

            // Assert
            Assert.IsFalse(_controller.IsEnabled,
                "Hide() should disable the line renderer.");
        }

        [Test]
        public void CreateCircularLine_AllPositionsHaveZeroZ()
        {
            // WHY: The rotation circle lies in the XY plane. Any non-zero Z
            //      values would cause the circle to appear tilted or distorted.

            // Act
            _controller.CreateCircularLine(24, 2.0f, 1.0f);

            // Assert
            for (int i = 0; i < _controller.Positions.Count; i++)
            {
                Assert.AreEqual(0f, _controller.Positions[i].Z, 0.001f,
                    string.Format("Position {0} Z coordinate should be 0 for a flat circle.", i));
            }
        }
    }
}
