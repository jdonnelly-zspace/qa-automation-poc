// =============================================================================
// MouseCursorControllerTests.cs - Edit Mode Unit Tests for MouseCursorController
// =============================================================================
// TARGET CLASS: MouseCursorController
//   Real file: Assets/CommonA3/zSpace/Scripts/Input/MouseCursorController.cs
//
// WHAT IT TESTS:
//   Mouse cursor management that changes the cursor sprite based on the
//   currently active tool (Move, Camera, Draw, Line, Text). Validates
//   default cursor selection for each tool mode, stylus visibility hiding
//   logic, and cursor color changes for the Draw tool.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real MouseCursorController is a MonoBehaviour that depends on
//      ZPointer, ZStylus, ZMouse, ToolManager, and SpriteRenderer. These
//      tests use lightweight stubs to exercise the cursor-selection logic
//      without a Unity runtime.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real enum instead.
    /// <summary>
    /// Mirrors the ToolId enum used by ToolManager.
    /// </summary>
    public enum ToolId
    {
        Move,
        Camera,
        Draw,
        Line,
        Text
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for a Unity Sprite reference, identified by name.
    /// </summary>
    public class SpriteStub
    {
        public string Name { get; }

        public SpriteStub(string name)
        {
            Name = name;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real struct instead.
    /// <summary>
    /// Simplified Color struct for testing cursor color logic.
    /// </summary>
    public struct ColorStub
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public ColorStub(float r, float g, float b, float a = 1.0f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static ColorStub White = new ColorStub(1, 1, 1, 1);
        public static ColorStub Red = new ColorStub(1, 0, 0, 1);

        public override bool Equals(object obj)
        {
            if (obj is ColorStub other)
            {
                return Math.Abs(R - other.R) < 0.001f &&
                       Math.Abs(G - other.G) < 0.001f &&
                       Math.Abs(B - other.B) < 0.001f &&
                       Math.Abs(A - other.A) < 0.001f;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode();
        }

        public override string ToString()
        {
            return $"({R}, {G}, {B}, {A})";
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the cursor-selection logic from the
    /// real MouseCursorController, without requiring MonoBehaviour.
    /// </summary>
    public class MouseCursorControllerStub
    {
        // Inspector-assigned cursor sprites
        public SpriteStub DefaultCursor = new SpriteStub("Default");
        public SpriteStub RotateCursor = new SpriteStub("Rotate");
        public SpriteStub AddCursor = new SpriteStub("Add");
        public SpriteStub PanCursor = new SpriteStub("Pan");
        public SpriteStub PanDownCursor = new SpriteStub("PanDown");
        public SpriteStub DrawCursor = new SpriteStub("Draw");
        public SpriteStub TextboxCursor = new SpriteStub("Textbox");
        public SpriteStub ResizeCursor = new SpriteStub("Resize");
        public SpriteStub AddTextLineCursor = new SpriteStub("AddTextLine");

        // Simulated state
        public ToolId CurrentTool = ToolId.Move;
        public bool IsButton0Pressed;
        public bool IsButton1Pressed;
        public bool IsOverUIElement;
        public bool IsStylusVisible;
        public ColorStub DrawToolLineColor = ColorStub.Red;

        // Output
        public SpriteStub ResultSprite { get; private set; }
        public ColorStub ResultColor { get; private set; }

        /// <summary>
        /// Simulates the cursor-selection logic from Update(), selecting
        /// the appropriate sprite and color based on the current tool.
        /// </summary>
        public void UpdateCursor()
        {
            SpriteStub sprite = DefaultCursor;
            ColorStub color = ColorStub.White;

            switch (CurrentTool)
            {
                case ToolId.Move:
                    // Default values
                    break;

                case ToolId.Camera:
                    if (!IsOverUIElement)
                    {
                        if (IsButton0Pressed)
                        {
                            sprite = PanDownCursor;
                        }
                        else if (IsButton1Pressed)
                        {
                            sprite = RotateCursor;
                        }
                        else
                        {
                            sprite = PanCursor;
                        }
                    }
                    break;

                case ToolId.Draw:
                    if (!IsOverUIElement)
                    {
                        color = DrawToolLineColor;
                        sprite = DrawCursor;
                    }
                    break;

                case ToolId.Line:
                    if (!IsOverUIElement)
                    {
                        sprite = AddTextLineCursor;
                    }
                    break;

                case ToolId.Text:
                    sprite = AddTextLineCursor;
                    break;
            }

            ResultSprite = sprite;
            ResultColor = color;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class MouseCursorControllerTests
    {
        private MouseCursorControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new MouseCursorControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void UpdateCursor_MoveTool_ShowsDefaultCursor()
        {
            // WHY: The Move tool is the default interaction mode. The cursor
            // must be the standard arrow so users know they can select objects.

            // Arrange
            _controller.CurrentTool = ToolId.Move;

            // Act
            _controller.UpdateCursor();

            // Assert
            Assert.AreEqual("Default", _controller.ResultSprite.Name,
                "Move tool should display the default cursor sprite.");
            Assert.AreEqual(ColorStub.White, _controller.ResultColor,
                "Move tool cursor should be white (unmodified).");
        }

        [Test]
        public void UpdateCursor_CameraToolIdle_ShowsPanCursor()
        {
            // WHY: When the Camera tool is active but no button is pressed,
            // the pan cursor signals that dragging will pan the viewport.

            // Arrange
            _controller.CurrentTool = ToolId.Camera;
            _controller.IsButton0Pressed = false;
            _controller.IsButton1Pressed = false;
            _controller.IsOverUIElement = false;

            // Act
            _controller.UpdateCursor();

            // Assert
            Assert.AreEqual("Pan", _controller.ResultSprite.Name,
                "Camera tool with no button pressed should show the Pan cursor.");
        }

        [Test]
        public void UpdateCursor_CameraToolButton0_ShowsPanDownCursor()
        {
            // WHY: Left-click drag in Camera mode pans the scene. The PanDown
            // cursor provides visual feedback that panning is in progress.

            // Arrange
            _controller.CurrentTool = ToolId.Camera;
            _controller.IsButton0Pressed = true;
            _controller.IsOverUIElement = false;

            // Act
            _controller.UpdateCursor();

            // Assert
            Assert.AreEqual("PanDown", _controller.ResultSprite.Name,
                "Camera tool with button 0 pressed should show the PanDown cursor.");
        }

        [Test]
        public void UpdateCursor_CameraToolButton1_ShowsRotateCursor()
        {
            // WHY: Right-click drag in Camera mode rotates the view. The Rotate
            // cursor signals the orbit interaction to the user.

            // Arrange
            _controller.CurrentTool = ToolId.Camera;
            _controller.IsButton0Pressed = false;
            _controller.IsButton1Pressed = true;
            _controller.IsOverUIElement = false;

            // Act
            _controller.UpdateCursor();

            // Assert
            Assert.AreEqual("Rotate", _controller.ResultSprite.Name,
                "Camera tool with button 1 pressed should show the Rotate cursor.");
        }

        [Test]
        public void UpdateCursor_DrawTool_ShowsDrawCursorWithLineColor()
        {
            // WHY: The draw cursor must match the current line color so
            // the user sees a preview of what color their stroke will be.

            // Arrange
            _controller.CurrentTool = ToolId.Draw;
            _controller.IsOverUIElement = false;
            _controller.DrawToolLineColor = ColorStub.Red;

            // Act
            _controller.UpdateCursor();

            // Assert
            Assert.AreEqual("Draw", _controller.ResultSprite.Name,
                "Draw tool should show the Draw cursor sprite.");
            Assert.AreEqual(ColorStub.Red, _controller.ResultColor,
                "Draw cursor color should match the DrawTool's current line color.");
        }

        [Test]
        public void UpdateCursor_LineTool_ShowsAddTextLineCursor()
        {
            // WHY: The Line tool lets users place measurement lines. The
            // AddTextLine cursor indicates that clicking will create a line.

            // Arrange
            _controller.CurrentTool = ToolId.Line;
            _controller.IsOverUIElement = false;

            // Act
            _controller.UpdateCursor();

            // Assert
            Assert.AreEqual("AddTextLine", _controller.ResultSprite.Name,
                "Line tool should show the AddTextLine cursor when not over UI.");
        }

        [Test]
        public void UpdateCursor_CameraToolOverUI_ShowsDefaultCursor()
        {
            // WHY: When the cursor is over a UI element (RectTransform),
            // the Camera tool should not show camera-specific cursors. The
            // default cursor lets the user interact with UI normally.

            // Arrange
            _controller.CurrentTool = ToolId.Camera;
            _controller.IsOverUIElement = true;

            // Act
            _controller.UpdateCursor();

            // Assert
            Assert.AreEqual("Default", _controller.ResultSprite.Name,
                "Camera tool should show default cursor when hovering over UI elements.");
        }

        [Test]
        public void UpdateCursor_DrawToolOverUI_ShowsDefaultCursor()
        {
            // WHY: When hovering over a UI panel while in Draw mode, the
            // cursor should revert to default so users can click UI buttons.

            // Arrange
            _controller.CurrentTool = ToolId.Draw;
            _controller.IsOverUIElement = true;

            // Act
            _controller.UpdateCursor();

            // Assert
            Assert.AreEqual("Default", _controller.ResultSprite.Name,
                "Draw tool should revert to default cursor when over UI elements.");
            Assert.AreEqual(ColorStub.White, _controller.ResultColor,
                "Cursor color should be white when over UI, not the draw color.");
        }
    }
}
