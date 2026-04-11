// =============================================================================
// ModelPackManagerTests.cs - Edit Mode Unit Tests for ModelPackManager
// =============================================================================
// TARGET CLASS: ModelPackManager
//   Real file: Assets/CommonA3/zSpace/Scripts/Model/ModelPackManager.cs
//
// WHAT IT TESTS:
//   The singleton that manages 3D model collections with licensing. Validates
//   index/guid lookup, tag/category filtering, distinct aggregation of
//   categories and tags, licensing availability updates, and graceful handling
//   of empty model lists.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real ModelPackManager is a singleton MonoBehaviour that loads model
//      packs from StreamingAssets. This stub uses an in-memory list so the
//      tests compile standalone.
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

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the real ModelInfo data class used to describe a 3D model.
    /// </summary>
    public class ModelInfo
    {
        public string Guid { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsAvailable { get; set; }
        public string RequiredLicensingModeId { get; set; }
        public List<string> RequiredLicensingSupplementIds { get; set; } = new List<string>();
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight stand-in for the real ModelPackManager singleton.
    /// Replicates lookup, filtering, aggregation, and licensing APIs.
    /// </summary>
    public class ModelPackManagerStub
    {
        private readonly List<ModelInfo> _models = new List<ModelInfo>();

        public bool IsLoaded { get; private set; }

        public IReadOnlyList<ModelInfo> Models => _models.AsReadOnly();
        public int ModelCount => _models.Count;

        public void LoadModels(IEnumerable<ModelInfo> models)
        {
            _models.AddRange(models);
            IsLoaded = true;
        }

        public ModelInfo GetModel(int index)
        {
            if (index < 0 || index >= _models.Count)
            {
                return null;
            }

            return _models[index];
        }

        public ModelInfo GetModelByGuid(string guid)
        {
            return _models.FirstOrDefault(m =>
                string.Equals(m.Guid, guid, StringComparison.OrdinalIgnoreCase));
        }

        public List<ModelInfo> GetModelsByTag(string tag)
        {
            return _models
                .Where(m => m.Tags.Any(t =>
                    string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public List<ModelInfo> GetModelsByCategory(string category)
        {
            return _models
                .Where(m => string.Equals(m.Category, category,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public IReadOnlyList<string> AllCategories
        {
            get
            {
                return _models
                    .Select(m => m.Category)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                    .AsReadOnly();
            }
        }

        public IReadOnlyList<string> AllTags
        {
            get
            {
                return _models
                    .SelectMany(m => m.Tags)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Updates availability of all models based on the current licensing
        /// state. A model is available when its required mode matches and all
        /// its required supplement IDs are present.
        /// </summary>
        public void UpdateAllModelAvailabilityForLicensingState(
            string modeId, IReadOnlyList<string> supplementIds)
        {
            var supplementSet = new HashSet<string>(supplementIds);

            foreach (ModelInfo model in _models)
            {
                bool modeMatch = string.IsNullOrEmpty(model.RequiredLicensingModeId) ||
                    string.Equals(model.RequiredLicensingModeId, modeId,
                        StringComparison.OrdinalIgnoreCase);
                bool supplementsOk = model.RequiredLicensingSupplementIds
                    .All(id => supplementSet.Contains(id));

                model.IsAvailable = modeMatch && supplementsOk;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ModelPackManagerTests
    {
        private ModelPackManagerStub _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new ModelPackManagerStub();

            // WHY: Seed with realistic zSpace 3D model data spanning multiple
            // categories, tags, and licensing requirements.
            _manager.LoadModels(new List<ModelInfo>
            {
                new ModelInfo
                {
                    Guid = "model-001",
                    Name = "Human Heart",
                    Category = "Anatomy",
                    Tags = new List<string> { "anatomy", "dissectable", "cardiovascular" },
                    IsAvailable = true,
                    RequiredLicensingModeId = "mode_standard",
                    RequiredLicensingSupplementIds = new List<string>()
                },
                new ModelInfo
                {
                    Guid = "model-002",
                    Name = "Human Brain",
                    Category = "Anatomy",
                    Tags = new List<string> { "anatomy", "dissectable", "nervous-system" },
                    IsAvailable = true,
                    RequiredLicensingModeId = "mode_standard",
                    RequiredLicensingSupplementIds = new List<string>()
                },
                new ModelInfo
                {
                    Guid = "model-003",
                    Name = "Solar System Orrery",
                    Category = "Astronomy",
                    Tags = new List<string> { "planets", "space", "interactive" },
                    IsAvailable = true,
                    RequiredLicensingModeId = "mode_standard",
                    RequiredLicensingSupplementIds = new List<string>()
                },
                new ModelInfo
                {
                    Guid = "model-004",
                    Name = "DNA Double Helix",
                    Category = "Biology",
                    Tags = new List<string> { "genetics", "dissectable" },
                    IsAvailable = true,
                    RequiredLicensingModeId = "mode_premium",
                    RequiredLicensingSupplementIds = new List<string> { "supplement_biology_advanced" }
                },
                new ModelInfo
                {
                    Guid = "model-005",
                    Name = "Clay Sculpture Base",
                    Category = "Art",
                    Tags = new List<string> { "sculpting", "interactive" },
                    IsAvailable = true,
                    RequiredLicensingModeId = "",
                    RequiredLicensingSupplementIds = new List<string>()
                }
            });
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
        }

        [Test]
        public void GetModel_ReturnsCorrectModel_ByIndex()
        {
            // WHY: The model gallery uses index-based access for pagination;
            // it must return the correct model at each position.

            // Act
            ModelInfo model = _manager.GetModel(0);

            // Assert
            Assert.IsNotNull(model,
                "GetModel(0) should return a model when models are loaded.");
            Assert.AreEqual("Human Heart", model.Name,
                "First model should be Human Heart.");
        }

        [Test]
        public void GetModel_ReturnsNull_ForOutOfRangeIndex()
        {
            // WHY: The gallery must handle boundary errors gracefully; a null
            // return lets the UI show an empty slot instead of crashing.

            // Act & Assert
            Assert.IsNull(_manager.GetModel(-1),
                "Negative index should return null.");
            Assert.IsNull(_manager.GetModel(999),
                "Index beyond model count should return null.");
        }

        [Test]
        public void GetModelByGuid_FindsCorrectModel()
        {
            // WHY: Activities reference models by GUID; the lookup must
            // return the exact model regardless of load order.

            // Act
            ModelInfo model = _manager.GetModelByGuid("model-003");

            // Assert
            Assert.IsNotNull(model,
                "Should find a model with GUID model-003.");
            Assert.AreEqual("Solar System Orrery", model.Name,
                "model-003 should be the Solar System Orrery.");
        }

        [Test]
        public void GetModelsByTag_FiltersCorrectly()
        {
            // WHY: The search panel lets students filter by tag; "dissectable"
            // should return all models that support interactive dissection.

            // Act
            List<ModelInfo> results = _manager.GetModelsByTag("dissectable");

            // Assert
            Assert.AreEqual(3, results.Count,
                "Three models are tagged 'dissectable' (Heart, Brain, DNA).");
            Assert.IsTrue(results.Any(m => m.Name == "Human Heart"),
                "Human Heart should be in dissectable results.");
            Assert.IsTrue(results.Any(m => m.Name == "Human Brain"),
                "Human Brain should be in dissectable results.");
            Assert.IsTrue(results.Any(m => m.Name == "DNA Double Helix"),
                "DNA Double Helix should be in dissectable results.");
        }

        [Test]
        public void GetModelsByCategory_FiltersCorrectly()
        {
            // WHY: The category sidebar groups models; Anatomy should show
            // exactly the heart and brain models.

            // Act
            List<ModelInfo> results = _manager.GetModelsByCategory("Anatomy");

            // Assert
            Assert.AreEqual(2, results.Count,
                "Two models belong to the Anatomy category.");
            Assert.IsTrue(results.All(m => m.Category == "Anatomy"),
                "All returned models should have category Anatomy.");
        }

        [Test]
        public void AllCategories_ReturnsDistinctCategories()
        {
            // WHY: The category filter dropdown must show each category exactly
            // once, even when multiple models share a category.

            // Act
            IReadOnlyList<string> categories = _manager.AllCategories;

            // Assert
            Assert.AreEqual(4, categories.Count,
                "Should have 4 distinct categories: Anatomy, Astronomy, Biology, Art.");
            Assert.IsTrue(categories.Contains("Anatomy"),
                "Anatomy should be in the category list.");
            Assert.IsTrue(categories.Contains("Astronomy"),
                "Astronomy should be in the category list.");
            Assert.IsTrue(categories.Contains("Biology"),
                "Biology should be in the category list.");
            Assert.IsTrue(categories.Contains("Art"),
                "Art should be in the category list.");
        }

        [Test]
        public void AllTags_ReturnsDistinctTags()
        {
            // WHY: The tag cloud must show each tag once; duplicates across
            // models must be collapsed.

            // Act
            IReadOnlyList<string> tags = _manager.AllTags;

            // Assert - we have: anatomy, dissectable, cardiovascular, nervous-system,
            // planets, space, interactive, genetics, sculpting = 9 distinct tags
            Assert.AreEqual(9, tags.Count,
                "Should have 9 distinct tags across all models.");
            Assert.IsTrue(tags.Contains("dissectable"),
                "'dissectable' appears on multiple models but should be listed once.");
            Assert.IsTrue(tags.Contains("interactive"),
                "'interactive' appears on multiple models but should be listed once.");
        }

        [Test]
        public void LicensingUpdate_MarksModelsAvailableOrUnavailable()
        {
            // WHY: When a school's license changes, model availability must
            // update in real time so students only see licensed content.

            // Act - standard mode, no supplements
            _manager.UpdateAllModelAvailabilityForLicensingState(
                "mode_standard", new List<string>());

            // Assert
            Assert.IsTrue(_manager.GetModelByGuid("model-001").IsAvailable,
                "Human Heart requires mode_standard with no supplements, should be available.");
            Assert.IsTrue(_manager.GetModelByGuid("model-003").IsAvailable,
                "Solar System Orrery requires mode_standard, should be available.");
            Assert.IsFalse(_manager.GetModelByGuid("model-004").IsAvailable,
                "DNA Double Helix requires mode_premium + supplement, should be unavailable.");
            Assert.IsTrue(_manager.GetModelByGuid("model-005").IsAvailable,
                "Clay Sculpture Base has no mode requirement, should always be available.");

            // Act - upgrade to premium with biology supplement
            _manager.UpdateAllModelAvailabilityForLicensingState(
                "mode_premium", new List<string> { "supplement_biology_advanced" });

            // Assert
            Assert.IsTrue(_manager.GetModelByGuid("model-004").IsAvailable,
                "DNA Double Helix should be available with premium mode and biology supplement.");
        }

        [Test]
        public void EmptyModelList_HandledGracefully()
        {
            // WHY: On first launch or if model packs fail to load, all queries
            // must return safe defaults without throwing exceptions.

            // Arrange - fresh empty manager
            var emptyManager = new ModelPackManagerStub();

            // Act & Assert
            Assert.AreEqual(0, emptyManager.ModelCount,
                "Model count should be zero on an empty manager.");
            Assert.IsNull(emptyManager.GetModel(0),
                "GetModel on an empty manager should return null.");
            Assert.IsNull(emptyManager.GetModelByGuid("nonexistent"),
                "GetModelByGuid on an empty manager should return null.");

            List<ModelInfo> tagResults = emptyManager.GetModelsByTag("anatomy");
            Assert.IsNotNull(tagResults,
                "GetModelsByTag should return an empty list, not null.");
            Assert.AreEqual(0, tagResults.Count,
                "No models should match any tag on an empty manager.");

            IReadOnlyList<string> categories = emptyManager.AllCategories;
            Assert.IsNotNull(categories,
                "AllCategories should return an empty list, not null.");
            Assert.AreEqual(0, categories.Count,
                "No categories should exist on an empty manager.");

            Assert.DoesNotThrow(() =>
                emptyManager.UpdateAllModelAvailabilityForLicensingState(
                    "mode_standard", new List<string>()),
                "Licensing update on an empty manager should not throw.");
        }
    }
}
