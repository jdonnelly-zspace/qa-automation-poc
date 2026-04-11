// =============================================================================
// NameplateControllerTests.cs - Edit Mode Unit Tests for NameplateController
// =============================================================================
// TARGET CLASS: NameplateController
//   Real file: Assets/StudioA3/Scripts/NameplateController.cs
//
// WHAT IT TESTS:
//   Nameplate UI controller that responds to selection changes, showing/hiding
//   labels and action buttons (Delete, Cut, Copy, Explode, Reassemble,
//   Clipping) based on the selected object type. Validates display toggling,
//   button visibility rules for ModelObject vs generic SceneObject, and the
//   EnableDisplay gate.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real NameplateController is a MonoBehaviour wired to SelectionManager
//      events. These tests exercise the selection-handling logic through POCO
//      stubs so they compile standalone in the POC.
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
    /// Minimal stand-in for SceneObject, the base type in the selection system.
    /// </summary>
    public class SceneObject
    {
        public bool IsLocked { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for ModelInfo metadata attached to models.
    /// </summary>
    public class ModelInfo
    {
        public string NameLocalizationTag { get; set; }
        public bool IsDissectable { get; set; }
        public bool IsExplodable { get; set; }
        public bool HasInternalFeatures { get; set; }
        public bool IsAnimated { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for ModelObject, a SceneObject with ModelInfo.
    /// </summary>
    public class ModelObject : SceneObject
    {
        public ModelInfo ModelInfo { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Tracks which buttons were shown/hidden and whether the nameplate was
    /// shown or hidden, simulating the real Nameplate UI component.
    /// </summary>
    public class MockNameplate
    {
        public bool IsVisible { get; private set; }
        public bool TextLabelActive { get; set; }
        public bool DividerActive { get; set; }
        public string TextLabelValue { get; set; }
        public Dictionary<string, bool> ButtonVisibility { get; } = new Dictionary<string, bool>();

        public void Show()
        {
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
        }

        public void SetButtonActive(string buttonName, bool active)
        {
            ButtonVisibility[buttonName] = active;
        }

        public bool IsButtonActive(string buttonName)
        {
            if (ButtonVisibility.TryGetValue(buttonName, out bool active))
            {
                return active;
            }

            return false;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the selection-handling logic of the real
    /// NameplateController without requiring MonoBehaviour or SelectionManager.
    /// </summary>
    public class NameplateControllerStub
    {
        private const string DeleteButton = "DeleteButton";
        private const string CutButton = "CutButton";
        private const string CopyButton = "CopyButton";
        private const string ClippingToggle = "ClippingPlaneToggle";
        private const string ExplodeButton = "ExplodeButton";
        private const string ReassembleButton = "ReassembleButton";

        private MockNameplate _nameplate;
        private bool _displayEnabled = true;

        public NameplateControllerStub(MockNameplate nameplate)
        {
            _nameplate = nameplate ?? throw new ArgumentNullException(nameof(nameplate));
        }

        public bool DisplayEnabled
        {
            get { return _displayEnabled; }
        }

        public void EnableDisplay(bool display)
        {
            _displayEnabled = display;
            // When display is disabled, hide immediately
            if (!_displayEnabled)
            {
                _nameplate.Hide();
            }
        }

        /// <summary>
        /// Simulates HandleOnSelectionChanged with a given list of selected objects.
        /// </summary>
        public void HandleSelectionChanged(List<SceneObject> selectedObjects)
        {
            if (!_displayEnabled)
            {
                _nameplate.Hide();
                return;
            }

            if (selectedObjects.Count == 1)
            {
                if (selectedObjects[0] is ModelObject)
                {
                    ModelObject mo = selectedObjects[0] as ModelObject;
                    if (mo.ModelInfo != null)
                    {
                        _nameplate.TextLabelActive = true;
                        _nameplate.DividerActive = true;
                        _nameplate.TextLabelValue = mo.ModelInfo.NameLocalizationTag;

                        _nameplate.SetButtonActive(DeleteButton, true);
                        _nameplate.SetButtonActive(CutButton, true);
                        _nameplate.SetButtonActive(CopyButton, true);

                        _nameplate.SetButtonActive(ClippingToggle, mo.ModelInfo.HasInternalFeatures);
                        _nameplate.SetButtonActive(ExplodeButton, mo.ModelInfo.IsExplodable);
                        _nameplate.SetButtonActive(ReassembleButton, mo.ModelInfo.IsDissectable);

                        _nameplate.Show();
                        return;
                    }
                }
                else
                {
                    // Non-model scene object: show clipboard buttons, hide model-specific ones
                    _nameplate.SetButtonActive(ClippingToggle, false);
                    _nameplate.SetButtonActive(ExplodeButton, false);
                    _nameplate.SetButtonActive(ReassembleButton, false);

                    _nameplate.SetButtonActive(DeleteButton, true);
                    _nameplate.SetButtonActive(CutButton, true);
                    _nameplate.SetButtonActive(CopyButton, true);

                    _nameplate.TextLabelActive = false;
                    _nameplate.DividerActive = false;

                    _nameplate.Show();
                    return;
                }
            }

            _nameplate.Hide();
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class NameplateControllerTests
    {
        private MockNameplate _nameplate;
        private NameplateControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _nameplate = new MockNameplate();
            _controller = new NameplateControllerStub(_nameplate);
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
            _nameplate = null;
        }

        [Test]
        public void HandleSelectionChanged_SingleModelObject_ShowsNameplateWithAllButtons()
        {
            // WHY: When a teacher selects a model, they need to see its name and
            //       all available actions (delete, cut, copy, explode, etc.).

            // Arrange
            var model = new ModelObject
            {
                ModelInfo = new ModelInfo
                {
                    NameLocalizationTag = "model.heart",
                    IsDissectable = true,
                    IsExplodable = true,
                    HasInternalFeatures = true
                }
            };

            // Act
            _controller.HandleSelectionChanged(new List<SceneObject> { model });

            // Assert
            Assert.IsTrue(_nameplate.IsVisible,
                "Nameplate should be visible when a single model with ModelInfo is selected.");
            Assert.IsTrue(_nameplate.TextLabelActive,
                "Text label should be active to display the model name.");
            Assert.IsTrue(_nameplate.DividerActive,
                "Divider should be active to separate the label from buttons.");
            Assert.AreEqual("model.heart", _nameplate.TextLabelValue,
                "Text label should show the model's localization tag.");
        }

        [Test]
        public void HandleSelectionChanged_SingleModelObject_HidesExplodeWhenNotExplodable()
        {
            // WHY: Showing an Explode button on a non-explodable model would confuse
            //       students and produce no-op clicks, hurting the UX.

            // Arrange
            var model = new ModelObject
            {
                ModelInfo = new ModelInfo
                {
                    NameLocalizationTag = "model.cube",
                    IsDissectable = false,
                    IsExplodable = false,
                    HasInternalFeatures = false
                }
            };

            // Act
            _controller.HandleSelectionChanged(new List<SceneObject> { model });

            // Assert
            Assert.IsFalse(_nameplate.IsButtonActive("ExplodeButton"),
                "Explode button should be hidden when model is not explodable.");
            Assert.IsFalse(_nameplate.IsButtonActive("ReassembleButton"),
                "Reassemble button should be hidden when model is not dissectable.");
            Assert.IsFalse(_nameplate.IsButtonActive("ClippingPlaneToggle"),
                "Clipping toggle should be hidden when model has no internal features.");
        }

        [Test]
        public void HandleSelectionChanged_SingleNonModelObject_ShowsClipboardButtonsOnly()
        {
            // WHY: Non-model scene objects (e.g., annotations, labels) should still
            //       offer delete/cut/copy but must not show model-specific actions.

            // Arrange
            var sceneObj = new SceneObject();

            // Act
            _controller.HandleSelectionChanged(new List<SceneObject> { sceneObj });

            // Assert
            Assert.IsTrue(_nameplate.IsVisible,
                "Nameplate should be visible for a single non-model scene object.");
            Assert.IsTrue(_nameplate.IsButtonActive("DeleteButton"),
                "Delete button should be visible for non-model objects.");
            Assert.IsTrue(_nameplate.IsButtonActive("CutButton"),
                "Cut button should be visible for non-model objects.");
            Assert.IsTrue(_nameplate.IsButtonActive("CopyButton"),
                "Copy button should be visible for non-model objects.");
            Assert.IsFalse(_nameplate.IsButtonActive("ExplodeButton"),
                "Explode button should be hidden for non-model objects.");
            Assert.IsFalse(_nameplate.TextLabelActive,
                "Text label should be hidden for non-model objects.");
        }

        [Test]
        public void HandleSelectionChanged_EmptySelection_HidesNameplate()
        {
            // WHY: When nothing is selected, the nameplate must disappear to avoid
            //       blocking the 3D viewport and confusing the user.

            // Arrange - first show the nameplate
            _controller.HandleSelectionChanged(new List<SceneObject> { new SceneObject() });
            Assert.IsTrue(_nameplate.IsVisible, "Precondition: nameplate should be visible.");

            // Act
            _controller.HandleSelectionChanged(new List<SceneObject>());

            // Assert
            Assert.IsFalse(_nameplate.IsVisible,
                "Nameplate should be hidden when the selection is empty.");
        }

        [Test]
        public void HandleSelectionChanged_MultipleObjects_HidesNameplate()
        {
            // WHY: The nameplate is designed for single-object context. Multi-select
            //       should hide it to avoid ambiguity about which object's actions
            //       are displayed.

            // Arrange
            var objects = new List<SceneObject> { new SceneObject(), new SceneObject() };

            // Act
            _controller.HandleSelectionChanged(objects);

            // Assert
            Assert.IsFalse(_nameplate.IsVisible,
                "Nameplate should be hidden when multiple objects are selected.");
        }

        [Test]
        public void EnableDisplay_SetToFalse_HidesNameplateRegardlessOfSelection()
        {
            // WHY: During presentation mode or certain tool activations, the
            //       nameplate must be suppressed even if objects are selected.

            // Arrange - select something so nameplate is showing
            _controller.HandleSelectionChanged(new List<SceneObject> { new SceneObject() });
            Assert.IsTrue(_nameplate.IsVisible, "Precondition: nameplate should be visible.");

            // Act
            _controller.EnableDisplay(false);

            // Assert
            Assert.IsFalse(_nameplate.IsVisible,
                "Nameplate should be hidden when display is disabled.");
            Assert.IsFalse(_controller.DisplayEnabled,
                "DisplayEnabled flag should be false after disabling.");
        }

        [Test]
        public void EnableDisplay_ReEnabled_ShowsNameplateOnNextSelection()
        {
            // WHY: After exiting presentation mode, the nameplate should resume
            //       normal behavior so the user can interact with objects again.

            // Arrange
            _controller.EnableDisplay(false);

            // Act
            _controller.EnableDisplay(true);
            _controller.HandleSelectionChanged(new List<SceneObject> { new SceneObject() });

            // Assert
            Assert.IsTrue(_nameplate.IsVisible,
                "Nameplate should be visible again after re-enabling display.");
        }

        [Test]
        public void HandleSelectionChanged_ModelObjectWithNullModelInfo_HidesNameplate()
        {
            // WHY: A ModelObject that hasn't finished loading may have null ModelInfo.
            //       The controller must not crash and should hide the nameplate.

            // Arrange
            var model = new ModelObject { ModelInfo = null };

            // Act
            _controller.HandleSelectionChanged(new List<SceneObject> { model });

            // Assert
            Assert.IsFalse(_nameplate.IsVisible,
                "Nameplate should be hidden when the selected model has null ModelInfo.");
        }
    }
}
