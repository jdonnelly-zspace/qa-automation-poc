// =============================================================================
// GlobalSettingsManagerTests.cs - Edit Mode Unit Tests for GlobalSettingsManager
// =============================================================================
// TARGET CLASS: GlobalSettingsManager
//   Real file: Assets/StudioA3/Scripts/UI/ContextMenu/GlobalSettingsManager.cs
//
// WHAT IT TESTS:
//   Pure data container for global settings that govern label visibility,
//   cutting plane activation, and dissectable state across anatomy lessons.
//   Validates default values, ApplySettings mutation, ResetToDefaults, and
//   independence of each setting from the others.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/enum marked with the "TODO: DELETE this stub" comment
//      and replace the using directives with the real namespaces.
//   3. The real GlobalSettingsManager may be a MonoBehaviour or ScriptableObject.
//      The stub here exercises only the data logic without Unity runtime.
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
    /// Controls which anatomical labels are displayed to the student.
    /// </summary>
    public enum LabelSettings
    {
        ShowNearest,
        ShowAll,
        ShowNone
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight stand-in for the real GlobalSettingsManager. Holds the three
    /// global settings that affect every anatomy model in a scene.
    /// </summary>
    public class GlobalSettingsManagerStub
    {
        public LabelSettings LabelSetting { get; private set; }
        public bool CuttingPlaneSetting { get; private set; }
        public bool DissectableSetting { get; private set; }

        public GlobalSettingsManagerStub()
        {
            ResetToDefaults();
        }

        public static GlobalSettingsManagerStub GetDefaults()
        {
            return new GlobalSettingsManagerStub();
        }

        public void ApplySettings(LabelSettings labelSetting, bool cutting, bool dissect)
        {
            LabelSetting = labelSetting;
            CuttingPlaneSetting = cutting;
            DissectableSetting = dissect;
        }

        public void ResetToDefaults()
        {
            LabelSetting = LabelSettings.ShowNearest;
            CuttingPlaneSetting = false;
            DissectableSetting = false;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class GlobalSettingsManagerTests
    {
        private GlobalSettingsManagerStub _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new GlobalSettingsManagerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _settings = null;
        }

        [Test]
        public void DefaultLabelSetting_IsShowNearest()
        {
            // WHY: The default label mode in anatomy lessons is ShowNearest so students
            // see only the label closest to their stylus, reducing visual clutter.

            // Assert
            Assert.AreEqual(LabelSettings.ShowNearest, _settings.LabelSetting,
                "Default LabelSetting should be ShowNearest so students are not overwhelmed by labels on first load.");
        }

        [Test]
        public void DefaultCuttingPlaneSetting_IsFalse()
        {
            // WHY: The cutting plane is an advanced tool; it must default to off so
            // younger students do not accidentally slice a model on lesson start.

            // Assert
            Assert.IsFalse(_settings.CuttingPlaneSetting,
                "Default CuttingPlaneSetting should be false to prevent accidental model slicing.");
        }

        [Test]
        public void DefaultDissectableSetting_IsFalse()
        {
            // WHY: Dissection must be explicitly enabled by the teacher or lesson plan;
            // defaulting to true would let students disassemble models prematurely.

            // Assert
            Assert.IsFalse(_settings.DissectableSetting,
                "Default DissectableSetting should be false so dissection requires explicit activation.");
        }

        [Test]
        public void ApplySettings_UpdatesAllThreeProperties()
        {
            // WHY: When a teacher toggles settings in the context menu, all three must
            // update atomically to keep the scene in a consistent state.

            // Act
            _settings.ApplySettings(LabelSettings.ShowAll, true, true);

            // Assert
            Assert.AreEqual(LabelSettings.ShowAll, _settings.LabelSetting,
                "ApplySettings should update LabelSetting to the provided value.");
            Assert.IsTrue(_settings.CuttingPlaneSetting,
                "ApplySettings should update CuttingPlaneSetting to true.");
            Assert.IsTrue(_settings.DissectableSetting,
                "ApplySettings should update DissectableSetting to true.");
        }

        [Test]
        public void ResetToDefaults_RestoresAllSettings()
        {
            // WHY: The "Reset" button in the context menu must reliably return every
            // setting to its factory value, regardless of the current state.

            // Arrange - apply non-default values
            _settings.ApplySettings(LabelSettings.ShowNone, true, true);

            // Act
            _settings.ResetToDefaults();

            // Assert
            Assert.AreEqual(LabelSettings.ShowNearest, _settings.LabelSetting,
                "ResetToDefaults should restore LabelSetting to ShowNearest.");
            Assert.IsFalse(_settings.CuttingPlaneSetting,
                "ResetToDefaults should restore CuttingPlaneSetting to false.");
            Assert.IsFalse(_settings.DissectableSetting,
                "ResetToDefaults should restore DissectableSetting to false.");
        }

        [Test]
        public void SettingsAreIndependent_ChangingOneDoesNotAffectOthers()
        {
            // WHY: A teacher enabling the cutting plane should not accidentally toggle
            // labels or dissection; each setting must be stored independently.

            // Arrange - start with known non-default state
            _settings.ApplySettings(LabelSettings.ShowAll, false, false);

            // Act - change only cutting plane
            _settings.ApplySettings(LabelSettings.ShowAll, true, false);

            // Assert
            Assert.AreEqual(LabelSettings.ShowAll, _settings.LabelSetting,
                "Changing CuttingPlaneSetting should not affect LabelSetting.");
            Assert.IsTrue(_settings.CuttingPlaneSetting,
                "CuttingPlaneSetting should be updated to true.");
            Assert.IsFalse(_settings.DissectableSetting,
                "Changing CuttingPlaneSetting should not affect DissectableSetting.");
        }

        [Test]
        public void AllLabelSettingsEnumValues_AreValid()
        {
            // WHY: If a new enum value is added without updating the manager, the
            // ApplySettings path must still accept it without error.

            // Act & Assert
            foreach (LabelSettings value in Enum.GetValues(typeof(LabelSettings)))
            {
                _settings.ApplySettings(value, false, false);
                Assert.AreEqual(value, _settings.LabelSetting,
                    $"ApplySettings should accept LabelSettings.{value} without error.");
            }
        }

        [Test]
        public void GetDefaults_ReturnsNewInstanceWithDefaultValues()
        {
            // WHY: Factory method must produce a clean instance so callers comparing
            // current settings against defaults get accurate results.

            // Act
            var defaults = GlobalSettingsManagerStub.GetDefaults();

            // Assert
            Assert.IsNotNull(defaults,
                "GetDefaults should return a non-null instance.");
            Assert.AreEqual(LabelSettings.ShowNearest, defaults.LabelSetting,
                "GetDefaults instance should have ShowNearest label setting.");
            Assert.IsFalse(defaults.CuttingPlaneSetting,
                "GetDefaults instance should have CuttingPlaneSetting = false.");
            Assert.IsFalse(defaults.DissectableSetting,
                "GetDefaults instance should have DissectableSetting = false.");
        }
    }
}
