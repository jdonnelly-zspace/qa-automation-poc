// =============================================================================
// ToolMenuControllerTests.cs - Edit Mode Unit Tests for ToolMenuController
// =============================================================================
// TARGET CLASS: ToolMenuController
//   Real file: Assets/StudioA3/Scripts/UI/ToolMenuController.cs
//
// WHAT IT TESTS:
//   Tool menu that maps toolbar button names to ToolId values and controls
//   draw palette visibility. Tests validate that each button resolves to the
//   correct tool IDs, that unknown buttons return safely, and that the draw
//   palette is only visible when the Draw tool is active.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real ToolMenuController is a MonoBehaviour that manages Unity UI
//      buttons and palette GameObjects. The stub here exercises only the
//      button-to-tool mapping and palette visibility logic without Unity runtime.
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
    /// Identifies individual tools available in the 3D workspace.
    /// </summary>
    public enum ToolId
    {
        Null,
        Move,
        Camera,
        Draw,
        Text,
        Line,
        XZMove,
        Rotate,
        Scale,
        Select
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the button-to-tool mapping and palette visibility logic of the
    /// real ToolMenuController without requiring MonoBehaviour or Unity UI.
    /// </summary>
    public class ToolMenuControllerStub
    {
        private readonly Dictionary<string, List<ToolId>> _buttonToolMap;
        public bool PaletteVisible { get; private set; }

        public ToolMenuControllerStub()
        {
            _buttonToolMap = new Dictionary<string, List<ToolId>>(StringComparer.OrdinalIgnoreCase)
            {
                { "MoveButton", new List<ToolId> { ToolId.Move } },
                { "CameraButton", new List<ToolId> { ToolId.Camera } },
                { "DrawButton", new List<ToolId> { ToolId.Draw, ToolId.Select } },
                { "TextButton", new List<ToolId> { ToolId.Text } },
                { "LineButton", new List<ToolId> { ToolId.Line } },
                { "RotateButton", new List<ToolId> { ToolId.Rotate } },
                { "ScaleButton", new List<ToolId> { ToolId.Scale } }
            };
            PaletteVisible = false;
        }

        /// <summary>
        /// Returns the tool IDs associated with the given button name, or an
        /// empty list if the button is not recognized.
        /// </summary>
        public List<ToolId> GetToolsForButton(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName))
            {
                return new List<ToolId>();
            }

            if (_buttonToolMap.TryGetValue(buttonName, out List<ToolId> tools))
            {
                return new List<ToolId>(tools);
            }

            return new List<ToolId>();
        }

        public void SetPaletteVisible(bool visible)
        {
            PaletteVisible = visible;
        }

        /// <summary>
        /// Returns true only when the Draw tool is active, because only Draw
        /// uses the color/brush palette.
        /// </summary>
        public bool IsPaletteVisibleForTool(string buttonName)
        {
            return string.Equals(buttonName, "DrawButton", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns all button names registered in the map.
        /// </summary>
        public IEnumerable<string> GetAllButtonNames()
        {
            return _buttonToolMap.Keys;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ToolMenuControllerTests
    {
        private ToolMenuControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new ToolMenuControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void GetToolsForButton_MoveButton_ReturnsMoveToolId()
        {
            // WHY: The Move tool lets students reposition 3D models in the scene;
            // mapping it to the wrong ToolId would activate a different tool.

            // Act
            List<ToolId> tools = _controller.GetToolsForButton("MoveButton");

            // Assert
            Assert.AreEqual(1, tools.Count,
                "MoveButton should map to exactly one tool.");
            Assert.Contains(ToolId.Move, tools,
                "MoveButton should map to the Move ToolId.");
        }

        [Test]
        public void GetToolsForButton_CameraButton_ReturnsCameraToolId()
        {
            // WHY: The Camera tool controls the viewport; students orbit and
            // zoom to examine 3D models from different angles.

            // Act
            List<ToolId> tools = _controller.GetToolsForButton("CameraButton");

            // Assert
            Assert.AreEqual(1, tools.Count,
                "CameraButton should map to exactly one tool.");
            Assert.Contains(ToolId.Camera, tools,
                "CameraButton should map to the Camera ToolId.");
        }

        [Test]
        public void GetToolsForButton_DrawButton_ReturnsDrawAndSelectToolIds()
        {
            // WHY: Draw mode activates both the Draw tool and the Select tool so
            // students can draw annotations and then select/move them immediately.

            // Act
            List<ToolId> tools = _controller.GetToolsForButton("DrawButton");

            // Assert
            Assert.AreEqual(2, tools.Count,
                "DrawButton should map to two tools (Draw and Select).");
            Assert.Contains(ToolId.Draw, tools,
                "DrawButton should include the Draw ToolId.");
            Assert.Contains(ToolId.Select, tools,
                "DrawButton should include the Select ToolId.");
        }

        [Test]
        public void GetToolsForButton_TextButton_ReturnsTextToolId()
        {
            // WHY: The Text tool places labels on 3D models for annotation;
            // mapping it incorrectly would break the labeling workflow.

            // Act
            List<ToolId> tools = _controller.GetToolsForButton("TextButton");

            // Assert
            Assert.AreEqual(1, tools.Count,
                "TextButton should map to exactly one tool.");
            Assert.Contains(ToolId.Text, tools,
                "TextButton should map to the Text ToolId.");
        }

        [Test]
        public void GetToolsForButton_LineButton_ReturnsLineToolId()
        {
            // WHY: The Line tool draws leader lines from labels to model parts;
            // incorrect mapping would prevent students from creating callouts.

            // Act
            List<ToolId> tools = _controller.GetToolsForButton("LineButton");

            // Assert
            Assert.AreEqual(1, tools.Count,
                "LineButton should map to exactly one tool.");
            Assert.Contains(ToolId.Line, tools,
                "LineButton should map to the Line ToolId.");
        }

        [Test]
        public void GetToolsForButton_UnknownButton_ReturnsEmptyList()
        {
            // WHY: A misnamed or dynamically generated button should not crash
            // the tool system; returning empty lets the caller handle gracefully.

            // Act
            List<ToolId> tools = _controller.GetToolsForButton("NonexistentButton");

            // Assert
            Assert.IsEmpty(tools,
                "Unknown button name should return an empty tool list.");
        }

        [Test]
        public void IsPaletteVisibleForTool_TrueOnlyForDraw()
        {
            // WHY: The color/brush palette is specific to the Draw tool; showing
            // it for Move or Camera would clutter the UI and confuse students.

            // Act & Assert
            Assert.IsTrue(_controller.IsPaletteVisibleForTool("DrawButton"),
                "Palette should be visible when the Draw tool is active.");
            Assert.IsFalse(_controller.IsPaletteVisibleForTool("MoveButton"),
                "Palette should be hidden when the Move tool is active.");
            Assert.IsFalse(_controller.IsPaletteVisibleForTool("CameraButton"),
                "Palette should be hidden when the Camera tool is active.");
            Assert.IsFalse(_controller.IsPaletteVisibleForTool("TextButton"),
                "Palette should be hidden when the Text tool is active.");
        }

        [Test]
        public void AllExpectedButtons_AreMapped()
        {
            // WHY: If a developer adds a toolbar button but forgets to register it
            // in the map, that button silently does nothing — this test catches omissions.

            // Arrange
            var expectedButtons = new List<string>
            {
                "MoveButton", "CameraButton", "DrawButton",
                "TextButton", "LineButton", "RotateButton", "ScaleButton"
            };

            // Act
            var mappedButtons = new List<string>(_controller.GetAllButtonNames());

            // Assert
            foreach (string expected in expectedButtons)
            {
                Assert.IsTrue(mappedButtons.Contains(expected),
                    $"Expected button '{expected}' should be registered in the tool map.");
            }
            Assert.AreEqual(expectedButtons.Count, mappedButtons.Count,
                "Tool map should contain exactly the expected number of button mappings.");
        }
    }
}
