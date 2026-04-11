// =============================================================================
// StylusCursorControllerTests.cs - Edit Mode Unit Tests for StylusCursorController
// =============================================================================
// TARGET CLASS: StylusCursorController
//   Real file: Assets/CommonA3/zSpace/Scripts/Input/StylusCursorController.cs
//
// WHAT IT TESTS:
//   Stylus cursor management for zSpace stereoscopic displays. Validates that
//   the correct sprite is selected for each tool mode (Move, Camera, Draw,
//   Line, Text), that the stylus is hidden when not visible, that draw color
//   is applied correctly, and that the camera distance scale calculation
//   returns sensible values.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real StylusCursorController depends on ZStylus, ZCamera, DrawTool,
//      ToolManager, and SpriteRenderer. These tests use POCO stubs so they
//      compile standalone without a Unity runtime.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the stylus sprite-selection logic from
    /// the real StylusCursorController, without requiring MonoBehaviour.
    /// Reuses ToolId, SpriteStub, and ColorStub from MouseCursorControllerTests.
    /// If running standalone, those stubs must also be present.
    /// </summary>
    public class StylusCursorControllerStub
    {
        // Inspector-assigned sprites
        public string RotateCursor = "Rotate";
        public string AddModelCursor = "AddModel";
        public string PanCursor = "Pan";
        public string PanDownCursor = "PanDown";
        public string DrawCursor = "Draw";
        public string TextboxCursor = "Textbox";
        public string ResizeCursor = "Resize";
        public string TextCursor = "Text";
        public string LineCursor = "Line";

        // Simulated state
        public ToolId CurrentTool = ToolId.Move;
        public bool IsStylusVisible = true;
        public bool IsButton0Pressed;
        public bool IsButton1Pressed;
        public bool IsOverUIElement;
        public bool IsOverTextResizeButton;
        public bool IsOverTextObject;
        public bool IsOverModelGalleryTile;
        public ColorStub DrawToolLineColor = ColorStub.Red;

        // Output after SetStylusSprite
        public string ResultSprite { get; private set; }
        public ColorStub ResultColor { get; private set; }
        public bool WasUpdateSkipped { get; private set; }

        /// <summary>
        /// Simulates the Update method — skips if stylus is not visible.
        /// </summary>
        public void SimulateUpdate()
        {
            WasUpdateSkipped = false;
            if (!IsStylusVisible)
            {
                WasUpdateSkipped = true;
                return;
            }

            SetStylusSprite();
        }

        /// <summary>
        /// Mirrors GetCameraDistanceScale from the real class.
        /// Returns (distanceToCameraPlane / distanceFromCameraToZeroParallax).
        /// </summary>
        public float GetCameraDistanceScale(float distanceToCameraPlane, float distanceFromCameraToZeroParallax)
        {
            if (Math.Abs(distanceFromCameraToZeroParallax) < 0.0001f)
            {
                return 0f;
            }
            return distanceToCameraPlane / distanceFromCameraToZeroParallax;
        }

        private void SetStylusSprite()
        {
            ResultSprite = null;
            ResultColor = ColorStub.White;

            switch (CurrentTool)
            {
                case ToolId.Move:
                    ResultColor = ColorStub.White;
                    ResultSprite = null;
                    break;

                case ToolId.Camera:
                    ResultColor = ColorStub.White;
                    if (IsOverUIElement)
                    {
                        ResultSprite = null;
                    }
                    else
                    {
                        if (IsButton0Pressed)
                        {
                            ResultSprite = PanDownCursor;
                        }
                        else if (IsButton1Pressed)
                        {
                            ResultSprite = RotateCursor;
                        }
                        else
                        {
                            ResultSprite = PanCursor;
                        }
                    }
                    break;

                case ToolId.Draw:
                    ResultColor = DrawToolLineColor;
                    ResultSprite = DrawCursor;
                    if (IsOverUIElement)
                    {
                        ResultColor = ColorStub.White;
                        ResultSprite = null;
                    }
                    break;

                case ToolId.Line:
                    ResultColor = ColorStub.White;
                    ResultSprite = LineCursor;
                    if (IsOverUIElement)
                    {
                        ResultSprite = null;
                    }
                    break;

                case ToolId.Text:
                    ResultColor = ColorStub.White;
                    ResultSprite = TextCursor;
                    if (IsOverTextResizeButton)
                    {
                        ResultSprite = ResizeCursor;
                    }
                    else if (IsOverTextObject)
                    {
                        ResultSprite = TextboxCursor;
                    }
                    else if (IsOverUIElement)
                    {
                        ResultSprite = null;
                    }
                    break;
            }

            // Gallery tile override
            if (IsOverModelGalleryTile)
            {
                ResultSprite = AddModelCursor;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class StylusCursorControllerTests
    {
        private StylusCursorControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new StylusCursorControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void SimulateUpdate_StylusNotVisible_SkipsAllProcessing()
        {
            // WHY: When the stylus is not tracked by the hardware, the
            // controller must bail out early to avoid updating a cursor
            // that nobody can see, saving unnecessary computation.

            // Arrange
            _controller.IsStylusVisible = false;

            // Act
            _controller.SimulateUpdate();

            // Assert
            Assert.IsTrue(_controller.WasUpdateSkipped,
                "Update should be skipped entirely when the stylus is not visible.");
        }

        [Test]
        public void SetStylusSprite_MoveTool_SpriteIsNull()
        {
            // WHY: The Move tool uses the built-in 3D stylus ray — no extra
            // sprite overlay should be rendered on top of it.

            // Arrange
            _controller.CurrentTool = ToolId.Move;

            // Act
            _controller.SimulateUpdate();

            // Assert
            Assert.IsNull(_controller.ResultSprite,
                "Move tool should set sprite to null since the stylus ray is sufficient.");
            Assert.AreEqual(ColorStub.White, _controller.ResultColor,
                "Move tool cursor color should be white.");
        }

        [Test]
        public void SetStylusSprite_CameraToolIdle_ShowsPanCursor()
        {
            // WHY: When no button is pressed in Camera mode, the Pan cursor
            // tells the user that dragging will pan the scene.

            // Arrange
            _controller.CurrentTool = ToolId.Camera;
            _controller.IsButton0Pressed = false;
            _controller.IsButton1Pressed = false;
            _controller.IsOverUIElement = false;

            // Act
            _controller.SimulateUpdate();

            // Assert
            Assert.AreEqual("Pan", _controller.ResultSprite,
                "Camera tool idle state should show the Pan cursor.");
        }

        [Test]
        public void SetStylusSprite_CameraToolButton0_ShowsPanDownCursor()
        {
            // WHY: When the primary button is held in Camera mode, the PanDown
            // cursor provides feedback that active panning is occurring.

            // Arrange
            _controller.CurrentTool = ToolId.Camera;
            _controller.IsButton0Pressed = true;
            _controller.IsOverUIElement = false;

            // Act
            _controller.SimulateUpdate();

            // Assert
            Assert.AreEqual("PanDown", _controller.ResultSprite,
                "Camera tool with button 0 held should show PanDown cursor.");
        }

        [Test]
        public void SetStylusSprite_DrawTool_ShowsDrawCursorWithLineColor()
        {
            // WHY: The draw cursor color must match the selected line color
            // so users see a preview of the stroke color before drawing.

            // Arrange
            _controller.CurrentTool = ToolId.Draw;
            _controller.IsOverUIElement = false;
            _controller.DrawToolLineColor = new ColorStub(0, 0, 1, 1); // blue

            // Act
            _controller.SimulateUpdate();

            // Assert
            Assert.AreEqual("Draw", _controller.ResultSprite,
                "Draw tool should display the Draw cursor sprite.");
            Assert.AreEqual(new ColorStub(0, 0, 1, 1), _controller.ResultColor,
                "Draw cursor color must match DrawTool.LineColor.");
        }

        [Test]
        public void SetStylusSprite_LineTool_ShowsLineCursor()
        {
            // WHY: The Line tool uses a specific cursor to indicate that
            // clicking will create measurement/annotation lines in 3D.

            // Arrange
            _controller.CurrentTool = ToolId.Line;
            _controller.IsOverUIElement = false;

            // Act
            _controller.SimulateUpdate();

            // Assert
            Assert.AreEqual("Line", _controller.ResultSprite,
                "Line tool should show the Line cursor when not over UI.");
        }

        [Test]
        public void SetStylusSprite_OverModelGalleryTile_ShowsAddModelCursor()
        {
            // WHY: When hovering over a model gallery tile, the AddModel
            // cursor signals that clicking will insert a 3D model into the scene.
            // This overrides the normal tool cursor.

            // Arrange
            _controller.CurrentTool = ToolId.Move;
            _controller.IsOverModelGalleryTile = true;

            // Act
            _controller.SimulateUpdate();

            // Assert
            Assert.AreEqual("AddModel", _controller.ResultSprite,
                "Hovering over a ModelGalleryTile must override cursor to AddModel.");
        }

        [Test]
        public void GetCameraDistanceScale_ReturnsRatio_ForStereoScaling()
        {
            // WHY: The stylus cursor scales based on distance from the camera
            // to maintain consistent apparent size in monoscopic rendering.
            // The ratio (distance / cameraToZeroParallax) drives this scaling.

            // Act
            float scale = _controller.GetCameraDistanceScale(2.0f, 1.0f);

            // Assert
            Assert.AreEqual(2.0f, scale, 0.001f,
                "Scale should be distanceToCameraPlane / distanceFromCameraToZeroParallax.");
        }
    }
}
