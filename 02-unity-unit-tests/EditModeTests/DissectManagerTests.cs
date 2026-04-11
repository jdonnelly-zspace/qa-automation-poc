// =============================================================================
// DissectManagerTests.cs - Edit Mode Unit Tests for DissectManager
// =============================================================================
// TARGET CLASS: DissectManager
//   Real file: Assets/VivedUpgrades/DissectManager.cs
//
// WHAT IT TESTS:
//   Model dissection and explosion system. Validates that Reassemble resets
//   dissected models, Explode activates dissection with explosion animation,
//   null/guard conditions are respected, undo/redo actions are pushed for
//   each operation, and the cutting plane gizmo is deactivated on explode.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real DissectManager is a ZSingleton<T> MonoBehaviour. These tests
//      exercise logic through lightweight POCO stubs so they compile
//      standalone in the POC without a Unity runtime.
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

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Minimal stand-in for ModelInfo metadata.</summary>
    public class ModelInfoStub
    {
        public bool IsDissectable { get; set; }
        public bool HasInternalFeatures { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Minimal stand-in for a model scene object supporting dissection.</summary>
    public class ModelObjectStub
    {
        public ModelInfoStub ModelInfo { get; set; }
        public bool DissectEnabled { get; set; }
        public bool IsDissected { get; private set; }
        public bool IsExploded { get; private set; }
        public bool TransformsReset { get; private set; }

        public void SetDissected(bool dissected, float duration, bool arg3, bool triggerExplode)
        {
            IsDissected = dissected;
            IsExploded = triggerExplode && dissected;
        }

        public void ResetTransforms(float duration)
        {
            TransformsReset = true;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Minimal stand-in for the cutting plane gizmo.</summary>
    public class CuttingPlaneGizmoStub
    {
        public bool IsActive { get; private set; } = true;

        public void ActivateCuttingPlane(bool active)
        {
            IsActive = active;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Tracks undo/redo actions pushed during dissect/explode operations.</summary>
    public class UndoRedoTrackerStub
    {
        public List<string> PushedActions { get; } = new List<string>();

        public void PushDissect(ModelObjectStub model, bool dissected)
        {
            PushedActions.Add($"Dissect:{dissected}");
        }

        public void PushExplode(ModelObjectStub model, bool explode)
        {
            PushedActions.Add($"Explode:{explode}");
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>
    /// Lightweight POCO mirroring the public API of DissectManager
    /// without requiring MonoBehaviour or ZSingleton.
    /// </summary>
    public class DissectManagerStub
    {
        private readonly List<ModelObjectStub> _selectedModels;
        private readonly CuttingPlaneGizmoStub _cuttingPlaneGizmo;
        private readonly UndoRedoTrackerStub _undoRedoTracker;

        private ModelObjectStub _modelObject;
        public bool SelectionCleared { get; private set; }

        public DissectManagerStub(
            List<ModelObjectStub> selectedModels,
            CuttingPlaneGizmoStub cuttingPlaneGizmo,
            UndoRedoTrackerStub undoRedoTracker)
        {
            _selectedModels = selectedModels;
            _cuttingPlaneGizmo = cuttingPlaneGizmo;
            _undoRedoTracker = undoRedoTracker;
        }

        /// <summary>Simulates selection-changed handler that sets _modelObject.</summary>
        public void HandleSelectionChanged()
        {
            if (_selectedModels.Count == 1)
            {
                ModelObjectStub mo = _selectedModels[0];
                if (mo != null && mo.ModelInfo != null)
                {
                    _modelObject = mo;
                    return;
                }
            }
            _modelObject = null;
        }

        public void Reassemble()
        {
            foreach (var model in _selectedModels)
            {
                if (model.ModelInfo != null && model.ModelInfo.IsDissectable)
                {
                    _undoRedoTracker.PushDissect(model, false);
                    model.SetDissected(false, 0.5f, false, false);
                    model.ResetTransforms(0.5f);
                }
            }
            SelectionCleared = true;
        }

        public void Explode()
        {
            if (_modelObject == null || !_modelObject.DissectEnabled)
            {
                return;
            }

            ApplyExplode(true);
        }

        private void ApplyExplode(bool explode)
        {
            if (_modelObject != null && explode && _modelObject.ModelInfo.HasInternalFeatures)
            {
                if (_cuttingPlaneGizmo != null)
                {
                    _cuttingPlaneGizmo.ActivateCuttingPlane(false);
                }
            }

            foreach (var model in _selectedModels)
            {
                if (model.ModelInfo != null && model.ModelInfo.IsDissectable)
                {
                    _undoRedoTracker.PushExplode(model, explode);
                    if (explode)
                    {
                        model.SetDissected(true, 0.5f, false, true);
                    }
                    else
                    {
                        model.SetDissected(false, 0.5f, false, false);
                    }
                }
            }
            SelectionCleared = true;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class DissectManagerTests
    {
        private List<ModelObjectStub> _selectedModels;
        private CuttingPlaneGizmoStub _cuttingPlaneGizmo;
        private UndoRedoTrackerStub _undoRedoTracker;
        private DissectManagerStub _manager;

        [SetUp]
        public void SetUp()
        {
            _selectedModels = new List<ModelObjectStub>();
            _cuttingPlaneGizmo = new CuttingPlaneGizmoStub();
            _undoRedoTracker = new UndoRedoTrackerStub();
            _manager = new DissectManagerStub(
                _selectedModels,
                _cuttingPlaneGizmo,
                _undoRedoTracker);
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
            _selectedModels = null;
            _cuttingPlaneGizmo = null;
            _undoRedoTracker = null;
        }

        // WHY: Students use Reassemble to reset a dissected model back to its
        // original state. Each affected model must be undissected and its
        // transforms restored so the 3D view looks correct.
        [Test]
        public void Reassemble_ResetsDissectedModels_AndClearsSelection()
        {
            // Arrange
            var model = new ModelObjectStub
            {
                ModelInfo = new ModelInfoStub { IsDissectable = true },
                DissectEnabled = true
            };
            model.SetDissected(true, 0f, false, false);
            _selectedModels.Add(model);

            // Act
            _manager.Reassemble();

            // Assert
            Assert.IsFalse(model.IsDissected,
                "Reassemble must set dissected state to false so the model appears whole.");
            Assert.IsTrue(model.TransformsReset,
                "Reassemble must call ResetTransforms to restore part positions.");
            Assert.IsTrue(_manager.SelectionCleared,
                "Selection should be cleared after reassembly to deselect the model.");
        }

        // WHY: Undo/redo support is critical so students can reverse accidental
        // reassembles without losing their work.
        [Test]
        public void Reassemble_PushesUndoAction_ForEachDissectableModel()
        {
            // Arrange
            _selectedModels.Add(new ModelObjectStub
            {
                ModelInfo = new ModelInfoStub { IsDissectable = true }
            });
            _selectedModels.Add(new ModelObjectStub
            {
                ModelInfo = new ModelInfoStub { IsDissectable = true }
            });

            // Act
            _manager.Reassemble();

            // Assert
            Assert.AreEqual(2, _undoRedoTracker.PushedActions.Count,
                "An undo action should be pushed for each dissectable model so each can be individually undone.");
        }

        // WHY: Non-dissectable models (e.g., simple shapes) should be silently
        // skipped during reassemble to avoid runtime errors.
        [Test]
        public void Reassemble_SkipsNonDissectableModels_NoError()
        {
            // Arrange
            _selectedModels.Add(new ModelObjectStub
            {
                ModelInfo = new ModelInfoStub { IsDissectable = false }
            });

            // Act
            _manager.Reassemble();

            // Assert
            Assert.AreEqual(0, _undoRedoTracker.PushedActions.Count,
                "Non-dissectable models should be skipped; no undo action should be pushed.");
        }

        // WHY: Explode is a guard-protected operation. If no model is selected
        // or DissectEnabled is false, nothing should happen to prevent
        // undefined behavior.
        [Test]
        public void Explode_DoesNothing_WhenModelObjectIsNull()
        {
            // Arrange - do not call HandleSelectionChanged, so _modelObject is null

            // Act
            _manager.Explode();

            // Assert
            Assert.AreEqual(0, _undoRedoTracker.PushedActions.Count,
                "Explode should be a no-op when no model is tracked (null guard).");
        }

        // WHY: The DissectEnabled flag controls whether a model supports
        // dissection at all. If disabled, Explode must not proceed.
        [Test]
        public void Explode_DoesNothing_WhenDissectEnabledIsFalse()
        {
            // Arrange
            var model = new ModelObjectStub
            {
                ModelInfo = new ModelInfoStub { IsDissectable = true },
                DissectEnabled = false
            };
            _selectedModels.Add(model);
            _manager.HandleSelectionChanged();

            // Act
            _manager.Explode();

            // Assert
            Assert.IsFalse(model.IsExploded,
                "Explode must not activate when DissectEnabled is false on the tracked model.");
        }

        // WHY: When a model with internal features is exploded, the cutting
        // plane gizmo must be hidden to avoid visual conflicts with the
        // exploded parts.
        [Test]
        public void Explode_DeactivatesCuttingPlane_WhenModelHasInternalFeatures()
        {
            // Arrange
            var model = new ModelObjectStub
            {
                ModelInfo = new ModelInfoStub { IsDissectable = true, HasInternalFeatures = true },
                DissectEnabled = true
            };
            _selectedModels.Add(model);
            _manager.HandleSelectionChanged();

            // Act
            _manager.Explode();

            // Assert
            Assert.IsFalse(_cuttingPlaneGizmo.IsActive,
                "Cutting plane gizmo should be deactivated when exploding a model with internal features.");
        }

        // WHY: A valid explode must mark the model as dissected AND exploded,
        // and push an undo action for full reversibility.
        [Test]
        public void Explode_SetsModelDissectedAndExploded_WhenValid()
        {
            // Arrange
            var model = new ModelObjectStub
            {
                ModelInfo = new ModelInfoStub { IsDissectable = true, HasInternalFeatures = false },
                DissectEnabled = true
            };
            _selectedModels.Add(model);
            _manager.HandleSelectionChanged();

            // Act
            _manager.Explode();

            // Assert
            Assert.IsTrue(model.IsDissected,
                "Explode should set the model to dissected state.");
            Assert.IsTrue(model.IsExploded,
                "Explode should trigger the explosion animation on the model.");
            Assert.AreEqual(1, _undoRedoTracker.PushedActions.Count,
                "Explode should push exactly one undo action for reversibility.");
        }

        // WHY: HandleSelectionChanged must only track a single model with
        // valid ModelInfo. Multi-select should clear the tracked model.
        [Test]
        public void HandleSelectionChanged_ClearsModelObject_WhenMultipleSelected()
        {
            // Arrange
            _selectedModels.Add(new ModelObjectStub
            {
                ModelInfo = new ModelInfoStub { IsDissectable = true },
                DissectEnabled = true
            });
            _selectedModels.Add(new ModelObjectStub
            {
                ModelInfo = new ModelInfoStub { IsDissectable = true },
                DissectEnabled = true
            });

            // Act
            _manager.HandleSelectionChanged();
            _manager.Explode();

            // Assert
            Assert.AreEqual(0, _undoRedoTracker.PushedActions.Count,
                "When multiple models are selected, _modelObject should be null and Explode should be a no-op.");
        }
    }
}
