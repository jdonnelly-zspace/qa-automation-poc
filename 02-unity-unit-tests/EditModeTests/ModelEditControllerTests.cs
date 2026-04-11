// =============================================================================
// ModelEditControllerTests.cs - Edit Mode Unit Tests for ModelEditController
// =============================================================================
// TARGET CLASS: ModelEditController
//   Real file: Assets/StudioA3/Scripts/UI/ModelEditController.cs
//
// WHAT IT TESTS:
//   Controller that manages model-editing UI: dissect toggle, cutting-plane
//   creation, and animation toggle. Responds to selection changes to enable
//   or disable dissect/cutting/animated button groups based on selected model
//   capabilities (IsDissectable, HasInternalFeatures, IsAnimated). Validates
//   button-group visibility, disassembly toggling, and animated-model handling.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real ModelEditController is a MonoBehaviour wrapped in #if false.
//      These tests exercise the selection-checking and toggle logic through
//      POCO stubs so they compile standalone in the POC.
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
    /// Minimal stand-in for a CanvasGroup used to show/hide UI button panels.
    /// </summary>
    public class MockCanvasGroup
    {
        public bool Interactable { get; set; }
        public bool BlocksRaycasts { get; set; }
        public float Alpha { get; set; }

        public MockCanvasGroup()
        {
            Interactable = false;
            BlocksRaycasts = false;
            Alpha = 0f;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for ModelObject with dissectable, internal-features,
    /// and animated capabilities, used by ModelEditController's selection checks.
    /// </summary>
    public class EditableModelObject : SceneObject
    {
        public ModelInfo ModelInfo { get; set; }
        public bool IsDissected { get; set; }
        public float AnimatorSpeed { get; set; } = 0f;
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the selection-checking and toggle logic of
    /// the real ModelEditController without requiring MonoBehaviour or
    /// SelectionManager singleton.
    /// </summary>
    public class ModelEditControllerStub
    {
        public MockCanvasGroup DissectButtonCanvasGroup { get; set; }
        public MockCanvasGroup CuttingButtonCanvasGroup { get; set; }
        public MockCanvasGroup AnimatedToggleCanvasGroup { get; set; }

        private List<SceneObject> _targets = new List<SceneObject>();
        private bool _dissectToggleState;
        private bool _animatedToggleState;

        public ModelEditControllerStub()
        {
            DissectButtonCanvasGroup = new MockCanvasGroup();
            CuttingButtonCanvasGroup = new MockCanvasGroup();
            AnimatedToggleCanvasGroup = new MockCanvasGroup();
        }

        public bool DissectToggleState
        {
            get { return _dissectToggleState; }
        }

        public bool AnimatedToggleState
        {
            get { return _animatedToggleState; }
        }

        public List<SceneObject> Targets
        {
            get { return _targets; }
        }

        /// <summary>
        /// Simulates CheckForDissectableModels from the real controller.
        /// </summary>
        public void CheckForDissectableModels(List<SceneObject> selectedObjects)
        {
            _targets = selectedObjects;

            if (_targets.Count > 0)
            {
                for (int i = 0; i < _targets.Count; i++)
                {
                    if (_targets[i] is EditableModelObject)
                    {
                        EditableModelObject m = _targets[i] as EditableModelObject;
                        if (m.ModelInfo != null && m.ModelInfo.IsDissectable)
                        {
                            DissectButtonCanvasGroup.Interactable = true;
                            DissectButtonCanvasGroup.BlocksRaycasts = true;
                            DissectButtonCanvasGroup.Alpha = 1;

                            if (m.IsDissected)
                            {
                                _dissectToggleState = true;
                                return;
                            }
                        }
                    }
                }

                if (DissectButtonCanvasGroup.BlocksRaycasts)
                {
                    return;
                }
            }
            else
            {
                _dissectToggleState = false;
            }

            DissectButtonCanvasGroup.BlocksRaycasts = false;
            DissectButtonCanvasGroup.Alpha = 0;
        }

        /// <summary>
        /// Simulates CheckForInternalsModels from the real controller.
        /// </summary>
        public void CheckForInternalsModels(List<SceneObject> selectedObjects)
        {
            _targets = selectedObjects;

            if (_targets.Count == 1)
            {
                if (_targets[0] is EditableModelObject)
                {
                    EditableModelObject m = _targets[0] as EditableModelObject;
                    if (m.ModelInfo != null && m.ModelInfo.HasInternalFeatures)
                    {
                        CuttingButtonCanvasGroup.Interactable = true;
                        CuttingButtonCanvasGroup.BlocksRaycasts = true;
                        CuttingButtonCanvasGroup.Alpha = 1;
                        return;
                    }
                }
            }

            CuttingButtonCanvasGroup.BlocksRaycasts = false;
            CuttingButtonCanvasGroup.Alpha = 0;
        }

        /// <summary>
        /// Simulates CheckForAnimatedModels from the real controller.
        /// </summary>
        public void CheckForAnimatedModels(List<SceneObject> selectedObjects)
        {
            _targets = selectedObjects;

            if (_targets.Count > 0)
            {
                for (int i = 0; i < _targets.Count; i++)
                {
                    if (_targets[i] is EditableModelObject)
                    {
                        EditableModelObject m = _targets[i] as EditableModelObject;
                        if (m.ModelInfo != null && m.ModelInfo.IsAnimated)
                        {
                            AnimatedToggleCanvasGroup.Interactable = true;
                            AnimatedToggleCanvasGroup.BlocksRaycasts = true;
                            AnimatedToggleCanvasGroup.Alpha = 1;

                            if (m.AnimatorSpeed > 0)
                            {
                                _animatedToggleState = true;
                                return;
                            }
                        }
                    }
                }
            }
            else
            {
                _animatedToggleState = false;
            }

            AnimatedToggleCanvasGroup.BlocksRaycasts = false;
            AnimatedToggleCanvasGroup.Alpha = 0;
        }

        /// <summary>
        /// Simulates the Disassemble toggle handler.
        /// </summary>
        public void Disassemble(bool dissect)
        {
            foreach (var sceneObject in _targets)
            {
                if (sceneObject is EditableModelObject)
                {
                    EditableModelObject m = sceneObject as EditableModelObject;
                    if (m.ModelInfo != null && m.ModelInfo.IsDissectable)
                    {
                        m.IsDissected = dissect;
                    }
                }
            }
        }

        /// <summary>
        /// Simulates the Animate toggle handler.
        /// </summary>
        public void Animate(bool animate)
        {
            foreach (var sceneObject in _targets)
            {
                if (sceneObject is EditableModelObject)
                {
                    EditableModelObject m = sceneObject as EditableModelObject;
                    if (m.ModelInfo != null && m.ModelInfo.IsAnimated)
                    {
                        m.AnimatorSpeed = animate ? 1.0f : 0.0f;
                    }
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ModelEditControllerTests
    {
        private ModelEditControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new ModelEditControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void CheckForDissectableModels_DissectableModel_EnablesDissectPanel()
        {
            // WHY: The dissect button panel must become interactable when a dissectable
            //       model is selected, so teachers can demonstrate anatomy breakdowns.

            // Arrange
            var model = new EditableModelObject
            {
                ModelInfo = new ModelInfo { IsDissectable = true }
            };

            // Act
            _controller.CheckForDissectableModels(new List<SceneObject> { model });

            // Assert
            Assert.IsTrue(_controller.DissectButtonCanvasGroup.Interactable,
                "Dissect canvas group should be interactable for dissectable models.");
            Assert.IsTrue(_controller.DissectButtonCanvasGroup.BlocksRaycasts,
                "Dissect canvas group should block raycasts to receive clicks.");
            Assert.AreEqual(1f, _controller.DissectButtonCanvasGroup.Alpha,
                "Dissect canvas group alpha should be 1 (fully visible).");
        }

        [Test]
        public void CheckForDissectableModels_NonDissectableModel_DisablesDissectPanel()
        {
            // WHY: Showing the dissect panel for a model that cannot be dissected
            //       would mislead users and produce no-op interactions.

            // Arrange
            var model = new EditableModelObject
            {
                ModelInfo = new ModelInfo { IsDissectable = false }
            };

            // Act
            _controller.CheckForDissectableModels(new List<SceneObject> { model });

            // Assert
            Assert.IsFalse(_controller.DissectButtonCanvasGroup.BlocksRaycasts,
                "Dissect canvas group should not block raycasts for non-dissectable models.");
            Assert.AreEqual(0f, _controller.DissectButtonCanvasGroup.Alpha,
                "Dissect canvas group alpha should be 0 (hidden) for non-dissectable models.");
        }

        [Test]
        public void CheckForDissectableModels_EmptySelection_ResetsDissectToggle()
        {
            // WHY: When the user deselects everything, the dissect toggle must reset
            //       so it does not carry stale state to the next selection.

            // Act
            _controller.CheckForDissectableModels(new List<SceneObject>());

            // Assert
            Assert.IsFalse(_controller.DissectToggleState,
                "Dissect toggle state should be false when selection is empty.");
            Assert.IsFalse(_controller.DissectButtonCanvasGroup.BlocksRaycasts,
                "Dissect canvas group should not block raycasts when nothing is selected.");
        }

        [Test]
        public void CheckForInternalsModels_ModelWithInternalFeatures_EnablesCuttingPanel()
        {
            // WHY: The cutting-plane button allows cross-section views of models
            //       with internal anatomy. It must only appear for qualifying models.

            // Arrange
            var model = new EditableModelObject
            {
                ModelInfo = new ModelInfo { HasInternalFeatures = true }
            };

            // Act
            _controller.CheckForInternalsModels(new List<SceneObject> { model });

            // Assert
            Assert.IsTrue(_controller.CuttingButtonCanvasGroup.Interactable,
                "Cutting canvas group should be interactable for models with internal features.");
            Assert.AreEqual(1f, _controller.CuttingButtonCanvasGroup.Alpha,
                "Cutting canvas group should be fully visible.");
        }

        [Test]
        public void CheckForInternalsModels_MultipleObjects_DisablesCuttingPanel()
        {
            // WHY: Cutting planes target a single model. With multiple objects
            //       selected, the cutting button must be hidden to avoid ambiguity.

            // Arrange
            var model1 = new EditableModelObject
            {
                ModelInfo = new ModelInfo { HasInternalFeatures = true }
            };
            var model2 = new EditableModelObject
            {
                ModelInfo = new ModelInfo { HasInternalFeatures = true }
            };

            // Act
            _controller.CheckForInternalsModels(new List<SceneObject> { model1, model2 });

            // Assert
            Assert.IsFalse(_controller.CuttingButtonCanvasGroup.BlocksRaycasts,
                "Cutting canvas group should not block raycasts when multiple objects are selected.");
            Assert.AreEqual(0f, _controller.CuttingButtonCanvasGroup.Alpha,
                "Cutting canvas group should be hidden when multiple objects are selected.");
        }

        [Test]
        public void CheckForAnimatedModels_AnimatedModelPlaying_SetsToggleOn()
        {
            // WHY: If a model is already animating (speed > 0), the toggle must
            //       reflect that so the UI is in sync with the runtime state.

            // Arrange
            var model = new EditableModelObject
            {
                ModelInfo = new ModelInfo { IsAnimated = true },
                AnimatorSpeed = 1.0f
            };

            // Act
            _controller.CheckForAnimatedModels(new List<SceneObject> { model });

            // Assert
            Assert.IsTrue(_controller.AnimatedToggleState,
                "Animated toggle should be ON when the model's animator speed is > 0.");
            Assert.AreEqual(1f, _controller.AnimatedToggleCanvasGroup.Alpha,
                "Animated canvas group should be fully visible for animated models.");
        }

        [Test]
        public void Disassemble_ToggleOn_MarksDissectableModelsAsDissected()
        {
            // WHY: When a teacher activates dissect mode, all dissectable models in
            //       the selection must enter dissected state for the lesson to work.

            // Arrange
            var dissectable = new EditableModelObject
            {
                ModelInfo = new ModelInfo { IsDissectable = true }
            };
            var nonDissectable = new EditableModelObject
            {
                ModelInfo = new ModelInfo { IsDissectable = false }
            };
            _controller.CheckForDissectableModels(new List<SceneObject> { dissectable, nonDissectable });

            // Act
            _controller.Disassemble(true);

            // Assert
            Assert.IsTrue(dissectable.IsDissected,
                "Dissectable model should be marked as dissected after Disassemble(true).");
            Assert.IsFalse(nonDissectable.IsDissected,
                "Non-dissectable model should not be affected by Disassemble.");
        }

        [Test]
        public void Animate_ToggleOn_SetsAnimatorSpeedToOne()
        {
            // WHY: Animated models (e.g., beating heart) must start playing when
            //       the animation toggle is activated to support real-time demos.

            // Arrange
            var animated = new EditableModelObject
            {
                ModelInfo = new ModelInfo { IsAnimated = true },
                AnimatorSpeed = 0f
            };
            _controller.CheckForAnimatedModels(new List<SceneObject> { animated });

            // Act
            _controller.Animate(true);

            // Assert
            Assert.AreEqual(1.0f, animated.AnimatorSpeed,
                "Animated model's speed should be 1.0 after Animate(true).");
        }
    }
}
