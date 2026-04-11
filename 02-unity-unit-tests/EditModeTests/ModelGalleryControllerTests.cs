// =============================================================================
// ModelGalleryControllerTests.cs - Edit Mode Unit Tests for ModelGalleryController
// =============================================================================
// TARGET CLASS: ModelGalleryController
//   Real file: Assets/StudioA3/Scripts/UI/ModelGalleryController.cs
//
// WHAT IT TESTS:
//   Model selection gallery with category, animated, dissectable, and internals
//   filters. Validates that individual filters, combined filters, search text
//   matching, custom sorting, and category-change pagination reset all behave
//   correctly.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment and
//      replace the using directives with the real namespaces.
//   3. The real ModelGalleryController is a MonoBehaviour. These tests exercise
//      the logic through a lightweight POCO stub so they compile standalone in
//      the POC without a Unity runtime.
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
    /// Minimal stand-in for the real ModelInfo data class.
    /// </summary>
    public class ModelInfo
    {
        public string Guid;
        public string Name;
        public string Category;
        public bool IsAnimated;
        public bool IsDissectable;
        public bool HasInternals;
        public string SearchableText;

        public ModelInfo()
        {
            SearchableText = "";
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API of the real
    /// ModelGalleryController, without requiring MonoBehaviour.
    /// </summary>
    public class ModelGalleryControllerStub
    {
        public List<ModelInfo> AllModels = new List<ModelInfo>();
        public string SearchFilter = "";
        public string CurrentCategory = "";
        public bool OnlyAnimated = false;
        public bool OnlyDissectable = false;
        public bool OnlyHasInternals = false;
        public int CurrentPage = 0;
        public int ItemsPerPage = 8;

        private List<ModelInfo> _validModels = new List<ModelInfo>();

        /// <summary>
        /// Returns true if the model matches all currently active filters.
        /// </summary>
        public bool ValidateModel(ModelInfo model)
        {
            if (model == null)
                return false;

            // Category filter
            if (!string.IsNullOrEmpty(CurrentCategory) &&
                !string.Equals(model.Category, CurrentCategory, StringComparison.OrdinalIgnoreCase))
                return false;

            // Boolean filters
            if (OnlyAnimated && !model.IsAnimated)
                return false;
            if (OnlyDissectable && !model.IsDissectable)
                return false;
            if (OnlyHasInternals && !model.HasInternals)
                return false;

            // Search text filter (case-insensitive)
            if (!string.IsNullOrEmpty(SearchFilter))
            {
                string lowerSearch = SearchFilter.ToLowerInvariant();
                string lowerName = (model.Name ?? "").ToLowerInvariant();
                string lowerSearchable = (model.SearchableText ?? "").ToLowerInvariant();
                if (!lowerName.Contains(lowerSearch) && !lowerSearchable.Contains(lowerSearch))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the current category filter and resets page to 0.
        /// </summary>
        public void SetCategory(string category)
        {
            CurrentCategory = category ?? "";
            CurrentPage = 0;
        }

        /// <summary>
        /// Builds the filtered list of valid models based on current filters.
        /// </summary>
        public void SetValidModels()
        {
            _validModels = AllModels.Where(m => ValidateModel(m)).ToList();
        }

        /// <summary>
        /// Returns the current filtered model list.
        /// </summary>
        public List<ModelInfo> GetValidModels()
        {
            return _validModels;
        }

        /// <summary>
        /// Custom string comparison that sorts alphabetic strings before numeric ones.
        /// </summary>
        public int CustomCompare(string a, string b)
        {
            bool aStartsWithDigit = a.Length > 0 && char.IsDigit(a[0]);
            bool bStartsWithDigit = b.Length > 0 && char.IsDigit(b[0]);

            if (aStartsWithDigit && !bStartsWithDigit)
                return 1;
            if (!aStartsWithDigit && bStartsWithDigit)
                return -1;

            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ModelGalleryControllerTests
    {
        private ModelGalleryControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new ModelGalleryControllerStub();

            _controller.AllModels = new List<ModelInfo>
            {
                new ModelInfo
                {
                    Guid = "model-human-heart",
                    Name = "Human Heart",
                    Category = "Anatomy",
                    IsAnimated = true,
                    IsDissectable = true,
                    HasInternals = true,
                    SearchableText = "human heart cardiovascular organ anatomy"
                },
                new ModelInfo
                {
                    Guid = "model-solar-system",
                    Name = "Solar System",
                    Category = "Astronomy",
                    IsAnimated = true,
                    IsDissectable = false,
                    HasInternals = false,
                    SearchableText = "solar system planets orbits astronomy"
                },
                new ModelInfo
                {
                    Guid = "model-frog",
                    Name = "Frog",
                    Category = "Anatomy",
                    IsAnimated = false,
                    IsDissectable = true,
                    HasInternals = true,
                    SearchableText = "frog amphibian dissection anatomy"
                },
                new ModelInfo
                {
                    Guid = "model-volcano",
                    Name = "Volcano",
                    Category = "Geology",
                    IsAnimated = true,
                    IsDissectable = false,
                    HasInternals = true,
                    SearchableText = "volcano eruption lava geology"
                },
                new ModelInfo
                {
                    Guid = "model-dna",
                    Name = "DNA Double Helix",
                    Category = "Biology",
                    IsAnimated = true,
                    IsDissectable = false,
                    HasInternals = false,
                    SearchableText = "dna double helix genetics biology"
                },
                new ModelInfo
                {
                    Guid = "model-skull",
                    Name = "Human Skull",
                    Category = "Anatomy",
                    IsAnimated = false,
                    IsDissectable = true,
                    HasInternals = false,
                    SearchableText = "skull cranium bones anatomy"
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        // ---------------------------------------------------------------------
        // Test: ValidateModel with no filters returns true
        // ---------------------------------------------------------------------
        // WHY: The default gallery state shows all models. If the no-filter
        //      case incorrectly hides models, the gallery appears empty on
        //      launch -- students cannot begin their lesson.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateModel_NoFiltersSet_ReturnsTrue()
        {
            // Arrange -- all filters at defaults (empty/false)
            var model = _controller.AllModels[0];

            // Act
            bool result = _controller.ValidateModel(model);

            // Assert
            Assert.IsTrue(result,
                "ValidateModel should return true when no filters are active");
        }

        // ---------------------------------------------------------------------
        // Test: ValidateModel filters by category
        // ---------------------------------------------------------------------
        // WHY: Teachers browse models by subject category to find curriculum-
        //      aligned content. Incorrect category filtering breaks lesson
        //      planning workflow.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateModel_CategoryFilter_ReturnsOnlyMatchingCategory()
        {
            // Arrange
            _controller.CurrentCategory = "Anatomy";
            var heartModel = _controller.AllModels.Find(m => m.Guid == "model-human-heart");
            var solarModel = _controller.AllModels.Find(m => m.Guid == "model-solar-system");

            // Act & Assert
            Assert.IsTrue(_controller.ValidateModel(heartModel),
                "Human Heart is in the 'Anatomy' category and should pass the filter");
            Assert.IsFalse(_controller.ValidateModel(solarModel),
                "Solar System is in 'Astronomy' and should be filtered out by 'Anatomy' category");
        }

        // ---------------------------------------------------------------------
        // Test: ValidateModel filters by animated flag
        // ---------------------------------------------------------------------
        // WHY: Animated models are used for demonstrations of processes (e.g.,
        //      heartbeat, planetary orbits). Teachers need to find only animated
        //      models when preparing a dynamic lesson.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateModel_OnlyAnimated_ReturnsAnimatedModels()
        {
            // Arrange
            _controller.OnlyAnimated = true;
            var heartModel = _controller.AllModels.Find(m => m.Guid == "model-human-heart"); // animated
            var frogModel = _controller.AllModels.Find(m => m.Guid == "model-frog"); // not animated

            // Act & Assert
            Assert.IsTrue(_controller.ValidateModel(heartModel),
                "Human Heart is animated and should pass the OnlyAnimated filter");
            Assert.IsFalse(_controller.ValidateModel(frogModel),
                "Frog is not animated and should be filtered out when OnlyAnimated is true");
        }

        // ---------------------------------------------------------------------
        // Test: ValidateModel filters by dissectable flag
        // ---------------------------------------------------------------------
        // WHY: Dissectable models let students pull apart layers to see internal
        //      structures. This is a key zSpace interactive feature that teachers
        //      specifically look for when planning hands-on lessons.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateModel_OnlyDissectable_ReturnsDissectableModels()
        {
            // Arrange
            _controller.OnlyDissectable = true;
            var frogModel = _controller.AllModels.Find(m => m.Guid == "model-frog"); // dissectable
            var dnaModel = _controller.AllModels.Find(m => m.Guid == "model-dna"); // not dissectable

            // Act & Assert
            Assert.IsTrue(_controller.ValidateModel(frogModel),
                "Frog is dissectable and should pass the OnlyDissectable filter");
            Assert.IsFalse(_controller.ValidateModel(dnaModel),
                "DNA Double Helix is not dissectable and should be filtered out");
        }

        // ---------------------------------------------------------------------
        // Test: ValidateModel filters by has-internals flag
        // ---------------------------------------------------------------------
        // WHY: Models with internals allow students to explore hidden structures
        //      (organs inside a body, magma inside a volcano). Filtering by this
        //      helps teachers find models with deeper exploration capabilities.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateModel_OnlyHasInternals_ReturnsModelsWithInternals()
        {
            // Arrange
            _controller.OnlyHasInternals = true;
            var volcanoModel = _controller.AllModels.Find(m => m.Guid == "model-volcano"); // has internals
            var dnaModel = _controller.AllModels.Find(m => m.Guid == "model-dna"); // no internals

            // Act & Assert
            Assert.IsTrue(_controller.ValidateModel(volcanoModel),
                "Volcano has internals and should pass the OnlyHasInternals filter");
            Assert.IsFalse(_controller.ValidateModel(dnaModel),
                "DNA Double Helix does not have internals and should be filtered out");
        }

        // ---------------------------------------------------------------------
        // Test: Combined filters (animated + dissectable) work together
        // ---------------------------------------------------------------------
        // WHY: Teachers often combine filters to narrow down to models that
        //      support both animation and dissection for the richest interactive
        //      experience. Both constraints must be satisfied simultaneously.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateModel_CombinedAnimatedAndDissectable_BothMustMatch()
        {
            // Arrange -- require both animated AND dissectable
            _controller.OnlyAnimated = true;
            _controller.OnlyDissectable = true;

            var heartModel = _controller.AllModels.Find(m => m.Guid == "model-human-heart"); // both
            var solarModel = _controller.AllModels.Find(m => m.Guid == "model-solar-system"); // animated only
            var frogModel = _controller.AllModels.Find(m => m.Guid == "model-frog"); // dissectable only

            // Act & Assert
            Assert.IsTrue(_controller.ValidateModel(heartModel),
                "Human Heart is both animated and dissectable and should pass combined filters");
            Assert.IsFalse(_controller.ValidateModel(solarModel),
                "Solar System is animated but not dissectable and should fail combined filters");
            Assert.IsFalse(_controller.ValidateModel(frogModel),
                "Frog is dissectable but not animated and should fail combined filters");
        }

        // ---------------------------------------------------------------------
        // Test: Search text filters by model name
        // ---------------------------------------------------------------------
        // WHY: Students and teachers type model names to quickly find specific
        //      content. Search must match against the model name and searchable
        //      text for a smooth user experience.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateModel_SearchText_FiltersModelsByName()
        {
            // Arrange -- search for "human" (case-insensitive)
            _controller.SearchFilter = "human";

            var heartModel = _controller.AllModels.Find(m => m.Guid == "model-human-heart");
            var skullModel = _controller.AllModels.Find(m => m.Guid == "model-skull");
            var volcanoModel = _controller.AllModels.Find(m => m.Guid == "model-volcano");

            // Act & Assert
            Assert.IsTrue(_controller.ValidateModel(heartModel),
                "Human Heart name contains 'human' and should match the search");
            Assert.IsTrue(_controller.ValidateModel(skullModel),
                "Human Skull name contains 'human' and should match the search");
            Assert.IsFalse(_controller.ValidateModel(volcanoModel),
                "Volcano does not contain 'human' and should be filtered out");
        }

        // ---------------------------------------------------------------------
        // Test: CustomCompare sorts alphabetic before numeric
        // ---------------------------------------------------------------------
        // WHY: Model names starting with numbers (e.g., "3D Skeleton") should
        //      sort after alphabetic names for a consistent browsing order that
        //      matches user expectations.
        // ---------------------------------------------------------------------
        [Test]
        public void CustomCompare_AlphabeticBeforeNumeric_SortsCorrectly()
        {
            // Act
            int alphaVsNumeric = _controller.CustomCompare("Frog", "3D Skeleton");
            int numericVsAlpha = _controller.CustomCompare("3D Skeleton", "Frog");

            // Assert
            Assert.Less(alphaVsNumeric, 0,
                "Alphabetic 'Frog' should sort before numeric '3D Skeleton'");
            Assert.Greater(numericVsAlpha, 0,
                "Numeric '3D Skeleton' should sort after alphabetic 'Frog'");
        }

        // ---------------------------------------------------------------------
        // Test: Setting category resets page to 0
        // ---------------------------------------------------------------------
        // WHY: When a user switches categories, they expect to see page 1 of the
        //      new category. If the page index is not reset, they may land on an
        //      out-of-bounds page and see no results -- a confusing UX bug.
        // ---------------------------------------------------------------------
        [Test]
        public void SetCategory_ResetsCurrentPageToZero()
        {
            // Arrange -- simulate the user browsing to page 3
            _controller.CurrentPage = 3;

            // Act -- switch to a different category
            _controller.SetCategory("Biology");

            // Assert
            Assert.AreEqual(0, _controller.CurrentPage,
                "Setting a new category should reset CurrentPage to 0");
            Assert.AreEqual("Biology", _controller.CurrentCategory,
                "CurrentCategory should be updated to the new category value");
        }
    }
}
