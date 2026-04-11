// =============================================================================
// ToolManagerTests.cs - Edit Mode Unit Tests for ToolManager
// =============================================================================
// TARGET CLASS: ToolManager
//   Real file: Assets/CommonA3/zSpace/Scripts/Tools/ToolManager.cs
//
// WHAT IT TESTS:
//   The singleton that manages active tools in the Studio A3 editor. Validates
//   tool activation/deactivation, event firing for OnToolActivated and
//   OnToolDeactivated, multi-tool selection, and correct state queries.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/enum marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real ToolManager is a singleton MonoBehaviour. This stub exercises
//      the tool-switching logic through a lightweight POCO so it compiles
//      standalone in the POC without a Unity runtime.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real enum instead.
    /// <summary>
    /// Mirrors the real ToolId enum used to identify tools in the toolbar.
    /// </summary>
    public enum ToolId
    {
        Null,
        Move,
        Camera,
        Draw,
        Text,
        Line,
        Select,
        Rotate,
        Scale
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight stand-in for the real ToolManager singleton.
    /// Replicates tool activation, deactivation, and event APIs.
    /// </summary>
    public class ToolManagerStub
    {
        private readonly List<ToolId> _activeTools = new List<ToolId>();

        public event Action<ToolId> OnToolActivated;
        public event Action<ToolId> OnToolDeactivated;

        public ToolId GetCurrentTool()
        {
            return _activeTools.Count > 0 ? _activeTools[0] : ToolId.Null;
        }

        public List<ToolId> GetCurrentToolIds()
        {
            return new List<ToolId>(_activeTools);
        }

        public void SetCurrentTool(ToolId toolId)
        {
            // Deactivate all current tools first
            var previousTools = new List<ToolId>(_activeTools);
            _activeTools.Clear();

            foreach (ToolId prev in previousTools)
            {
                OnToolDeactivated?.Invoke(prev);
            }

            if (toolId != ToolId.Null)
            {
                _activeTools.Add(toolId);
                OnToolActivated?.Invoke(toolId);
            }
        }

        public void SetCurrentTools(List<ToolId> toolIds)
        {
            // Deactivate all current tools first
            var previousTools = new List<ToolId>(_activeTools);
            _activeTools.Clear();

            foreach (ToolId prev in previousTools)
            {
                OnToolDeactivated?.Invoke(prev);
            }

            foreach (ToolId toolId in toolIds)
            {
                if (toolId != ToolId.Null)
                {
                    _activeTools.Add(toolId);
                    OnToolActivated?.Invoke(toolId);
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ToolManagerTests
    {
        private ToolManagerStub _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new ToolManagerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
        }

        [Test]
        public void SetCurrentTool_ActivatesToolAndFiresEvent()
        {
            // WHY: When a student picks a tool from the toolbar, the system
            // must activate it and notify listeners (e.g., cursor, toolbar UI).

            // Arrange
            ToolId activatedTool = ToolId.Null;
            _manager.OnToolActivated += tool => activatedTool = tool;

            // Act
            _manager.SetCurrentTool(ToolId.Move);

            // Assert
            Assert.AreEqual(ToolId.Move, _manager.GetCurrentTool(),
                "Current tool should be Move after SetCurrentTool(Move).");
            Assert.AreEqual(ToolId.Move, activatedTool,
                "OnToolActivated should have fired with ToolId.Move.");
        }

        [Test]
        public void SetCurrentTool_DeactivatesPreviousTool_BeforeActivatingNew()
        {
            // WHY: Only one primary tool can be active at a time. The old tool
            // must be deactivated before the new one activates to avoid
            // conflicting input handlers.

            // Arrange
            _manager.SetCurrentTool(ToolId.Draw);
            ToolId deactivatedTool = ToolId.Null;
            _manager.OnToolDeactivated += tool => deactivatedTool = tool;

            // Act
            _manager.SetCurrentTool(ToolId.Text);

            // Assert
            Assert.AreEqual(ToolId.Draw, deactivatedTool,
                "OnToolDeactivated should have fired for Draw when switching to Text.");
            Assert.AreEqual(ToolId.Text, _manager.GetCurrentTool(),
                "Current tool should now be Text.");
        }

        [Test]
        public void GetCurrentTool_ReturnsNull_WhenNoneActive()
        {
            // WHY: The default state must be ToolId.Null so the UI knows no
            // tool is selected and can display an appropriate idle state.

            // Act & Assert
            Assert.AreEqual(ToolId.Null, _manager.GetCurrentTool(),
                "GetCurrentTool should return Null when no tool has been activated.");
        }

        [Test]
        public void SetCurrentTools_ActivatesMultipleTools()
        {
            // WHY: Some workflows activate multiple tools simultaneously
            // (e.g., Move + Rotate for transform mode).

            // Arrange
            var activatedTools = new List<ToolId>();
            _manager.OnToolActivated += tool => activatedTools.Add(tool);

            // Act
            _manager.SetCurrentTools(new List<ToolId> { ToolId.Move, ToolId.Rotate });

            // Assert
            List<ToolId> currentTools = _manager.GetCurrentToolIds();
            Assert.AreEqual(2, currentTools.Count,
                "Two tools should be active after SetCurrentTools.");
            Assert.Contains(ToolId.Move, currentTools,
                "Move should be in the active tool list.");
            Assert.Contains(ToolId.Rotate, currentTools,
                "Rotate should be in the active tool list.");
            Assert.AreEqual(2, activatedTools.Count,
                "OnToolActivated should have fired twice, once per tool.");
        }

        [Test]
        public void Deactivation_FiresOnToolDeactivated_ForEachOldTool()
        {
            // WHY: When switching from a multi-tool state, every old tool must
            // fire its deactivation event so all listeners can clean up.

            // Arrange
            _manager.SetCurrentTools(new List<ToolId> { ToolId.Move, ToolId.Scale });
            var deactivatedTools = new List<ToolId>();
            _manager.OnToolDeactivated += tool => deactivatedTools.Add(tool);

            // Act
            _manager.SetCurrentTool(ToolId.Camera);

            // Assert
            Assert.AreEqual(2, deactivatedTools.Count,
                "OnToolDeactivated should fire for both Move and Scale.");
            Assert.Contains(ToolId.Move, deactivatedTools,
                "Move should have been deactivated.");
            Assert.Contains(ToolId.Scale, deactivatedTools,
                "Scale should have been deactivated.");
        }

        [Test]
        public void GetCurrentToolIds_ReturnsAllActiveTools()
        {
            // WHY: UI elements like the toolbar need to highlight all currently
            // active tools, not just the first one.

            // Arrange
            _manager.SetCurrentTools(new List<ToolId> { ToolId.Line, ToolId.Select, ToolId.Draw });

            // Act
            List<ToolId> toolIds = _manager.GetCurrentToolIds();

            // Assert
            Assert.AreEqual(3, toolIds.Count,
                "Should return all 3 active tool IDs.");
            Assert.Contains(ToolId.Line, toolIds,
                "Line should be in active tools.");
            Assert.Contains(ToolId.Select, toolIds,
                "Select should be in active tools.");
            Assert.Contains(ToolId.Draw, toolIds,
                "Draw should be in active tools.");
        }

        [Test]
        public void SetCurrentTool_ToNull_DeactivatesAll()
        {
            // WHY: Setting the tool to Null is the canonical way to return to
            // idle state; all active tools must be deactivated.

            // Arrange
            _manager.SetCurrentTool(ToolId.Camera);
            var deactivatedTools = new List<ToolId>();
            _manager.OnToolDeactivated += tool => deactivatedTools.Add(tool);

            // Act
            _manager.SetCurrentTool(ToolId.Null);

            // Assert
            Assert.AreEqual(ToolId.Null, _manager.GetCurrentTool(),
                "Current tool should be Null after deactivating.");
            Assert.AreEqual(1, deactivatedTools.Count,
                "OnToolDeactivated should have fired for Camera.");
            Assert.AreEqual(ToolId.Camera, deactivatedTools[0],
                "Camera should have been the deactivated tool.");
        }

        [Test]
        public void SetCurrentTools_IgnoresNullEntries()
        {
            // WHY: Callers may pass Null entries in the list; they should be
            // silently ignored to prevent phantom tool activations.

            // Arrange
            var activatedTools = new List<ToolId>();
            _manager.OnToolActivated += tool => activatedTools.Add(tool);

            // Act
            _manager.SetCurrentTools(new List<ToolId> { ToolId.Null, ToolId.Move, ToolId.Null });

            // Assert
            List<ToolId> currentTools = _manager.GetCurrentToolIds();
            Assert.AreEqual(1, currentTools.Count,
                "Only Move should be active; Null entries should be ignored.");
            Assert.AreEqual(ToolId.Move, currentTools[0],
                "The single active tool should be Move.");
            Assert.AreEqual(1, activatedTools.Count,
                "OnToolActivated should fire only once for Move.");
        }
    }
}
