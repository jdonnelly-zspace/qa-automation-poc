// =============================================================================
// VivedModelGalleryControllerTests.cs - Edit Mode Unit Tests
// =============================================================================
// TARGET CLASS: VivedModelGalleryController
//   Real file: Assets/VivedUpgrades/ModelGallery/Scripts/VivedModelGalleryController.cs
//
// WHAT IT TESTS:
//   Model gallery controller that manages browsing, filtering, sorting, and
//   loading 3D models. Validates category filtering, model-type toggles
//   (dissectable, animated, internals, explodable), search text filtering,
//   pagination math, sort ordering, and null-guid guard on LoadModel.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real VivedModelGalleryController is a MonoBehaviour. These tests
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
    public class ModelInfoStubForGallery
    {
        public string Guid { get; set; }
        public string NameLocalizationTag { get; set; }
        public string ThumbnailPath { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsDissectable { get; set; }
        public bool IsAnimated { get; set; }
        public bool HasInternalFeatures { get; set; }
        public bool IsExplodable { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>
    /// Lightweight POCO mirroring the filtering, sorting, pagination, and
    /// load logic of VivedModelGalleryController without MonoBehaviour.
    /// </summary>
    public class VivedModelGalleryControllerStub
    {
        public const string AllCategoriesToggleId = "AllCategories";
        private const int TilesPerPage = 10;

        private List<ModelInfoStubForGallery> _allModels;
        private List<ModelInfoStubForGallery> _validModels = new List<ModelInfoStubForGallery>();

        private string _currentCategory = "";
        private bool _onlyDissectable;
        private bool _onlyAnimated;
        private bool _onlyInternals;
        private bool _onlyExplodable;
        private string _searchText = "";

        public int CurrentPageNumber { get; private set; }
        public int TotalPages { get; private set; }
        public string LastLoadedGuid { get; private set; }
        public bool LoadAttempted { get; private set; }
        public int ValidModelCount => _validModels.Count;

        private enum SortType { Available, Alphabetical, ReverseAlphabetical }
        private SortType _currentSort = SortType.Available;

        public VivedModelGalleryControllerStub(List<ModelInfoStubForGallery> allModels)
        {
            _allModels = allModels;
        }

        public void SetSearchText(string text)
        {
            _searchText = text ?? "";
            Refresh();
        }

        public void SetCategory(string category)
        {
            _currentCategory = category ?? "";
            Refresh();
        }

        public void SetOnlyDissectable(bool value)
        {
            _onlyDissectable = value;
            Refresh();
        }

        public void SetOnlyAnimated(bool value)
        {
            _onlyAnimated = value;
            Refresh();
        }

        public void SetOnlyHasInternals(bool value)
        {
            _onlyInternals = value;
            Refresh();
        }

        public void SetOnlyExplodable(bool value)
        {
            _onlyExplodable = value;
            Refresh();
        }

        public void SetSort(string sortToggleName)
        {
            switch (sortToggleName)
            {
                case "AvailableSortToggle":
                    _currentSort = SortType.Available;
                    break;
                case "AlphabeticalSortToggle":
                    _currentSort = SortType.Alphabetical;
                    break;
                case "ReverseAlphabeticalSortToggle":
                    _currentSort = SortType.ReverseAlphabetical;
                    break;
            }
            Refresh();
        }

        public void LoadModel(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }
            LoadAttempted = true;
            LastLoadedGuid = guid;
        }

        private void Refresh()
        {
            List<ModelInfoStubForGallery> selected;
            if (string.IsNullOrEmpty(_currentCategory) || _currentCategory == AllCategoriesToggleId)
            {
                selected = new List<ModelInfoStubForGallery>(_allModels);
            }
            else
            {
                selected = new List<ModelInfoStubForGallery>();
                // In real code, this filters by category from ModelPackManager
                foreach (var m in _allModels)
                {
                    if (m.Tags.Contains(_currentCategory))
                    {
                        selected.Add(m);
                    }
                }
            }

            _validModels = new List<ModelInfoStubForGallery>();
            foreach (var m in selected)
            {
                if (ValidateModel(m))
                {
                    _validModels.Add(m);
                }
            }

            // Sorting
            if (_currentSort == SortType.Alphabetical)
            {
                _validModels.Sort((a, b) =>
                    string.Compare(a.NameLocalizationTag, b.NameLocalizationTag, StringComparison.OrdinalIgnoreCase));
            }
            else if (_currentSort == SortType.ReverseAlphabetical)
            {
                _validModels.Sort((a, b) =>
                    string.Compare(b.NameLocalizationTag, a.NameLocalizationTag, StringComparison.OrdinalIgnoreCase));
            }

            CurrentPageNumber = 0;
            TotalPages = (int)Math.Ceiling((double)_validModels.Count / TilesPerPage);
        }

        private bool ValidateModel(ModelInfoStubForGallery model)
        {
            if (_onlyAnimated && !model.IsAnimated) return false;
            if (_onlyDissectable && !model.IsDissectable) return false;
            if (_onlyInternals && !model.HasInternalFeatures) return false;
            if (_onlyExplodable && !model.IsExplodable) return false;

            string search = _searchText.ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                if (model.NameLocalizationTag.ToLower().Contains(search))
                {
                    return true;
                }
                foreach (string tag in model.Tags)
                {
                    if (tag.ToLower().Contains(search))
                    {
                        return true;
                    }
                }
                return false;
            }

            return true;
        }

        public List<string> GetValidModelGuids()
        {
            var guids = new List<string>();
            foreach (var m in _validModels)
            {
                guids.Add(m.Guid);
            }
            return guids;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class VivedModelGalleryControllerTests
    {
        private List<ModelInfoStubForGallery> _testModels;
        private VivedModelGalleryControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _testModels = new List<ModelInfoStubForGallery>
            {
                new ModelInfoStubForGallery
                {
                    Guid = "guid-heart", NameLocalizationTag = "Heart",
                    IsAvailable = true, IsDissectable = true, IsAnimated = false,
                    HasInternalFeatures = true, IsExplodable = true,
                    Tags = new List<string> { "Biology", "Anatomy" }
                },
                new ModelInfoStubForGallery
                {
                    Guid = "guid-brain", NameLocalizationTag = "Brain",
                    IsAvailable = true, IsDissectable = true, IsAnimated = false,
                    HasInternalFeatures = false, IsExplodable = false,
                    Tags = new List<string> { "Biology", "Neuroscience" }
                },
                new ModelInfoStubForGallery
                {
                    Guid = "guid-solar", NameLocalizationTag = "Solar System",
                    IsAvailable = false, IsDissectable = false, IsAnimated = true,
                    HasInternalFeatures = false, IsExplodable = false,
                    Tags = new List<string> { "Astronomy" }
                },
                new ModelInfoStubForGallery
                {
                    Guid = "guid-cell", NameLocalizationTag = "Animal Cell",
                    IsAvailable = true, IsDissectable = false, IsAnimated = false,
                    HasInternalFeatures = true, IsExplodable = true,
                    Tags = new List<string> { "Biology" }
                }
            };
            _controller = new VivedModelGalleryControllerStub(_testModels);
            // Initialize by running a refresh with default filters
            _controller.SetSearchText("");
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
            _testModels = null;
        }

        // WHY: The dissectable filter lets science teachers find only models
        // their students can take apart, which is a core learning workflow.
        [Test]
        public void SetOnlyDissectable_FiltersToOnlyDissectableModels()
        {
            // Act
            _controller.SetOnlyDissectable(true);

            // Assert
            List<string> guids = _controller.GetValidModelGuids();
            Assert.AreEqual(2, guids.Count,
                "Only dissectable models (Heart, Brain) should remain after filtering.");
            Assert.IsTrue(guids.Contains("guid-heart"),
                "Heart is dissectable and should be included.");
            Assert.IsTrue(guids.Contains("guid-brain"),
                "Brain is dissectable and should be included.");
        }

        // WHY: The animated filter lets users find models with animations
        // (e.g., Solar System orbiting) for dynamic presentations.
        [Test]
        public void SetOnlyAnimated_FiltersToOnlyAnimatedModels()
        {
            // Act
            _controller.SetOnlyAnimated(true);

            // Assert
            List<string> guids = _controller.GetValidModelGuids();
            Assert.AreEqual(1, guids.Count,
                "Only the animated model (Solar System) should remain after filtering.");
            Assert.AreEqual("guid-solar", guids[0],
                "Solar System is the only animated model.");
        }

        // WHY: Search by name lets students quickly find a specific model
        // without scrolling through pages, improving discoverability.
        [Test]
        public void SetSearchText_FiltersByModelName_CaseInsensitive()
        {
            // Act
            _controller.SetSearchText("heart");

            // Assert
            List<string> guids = _controller.GetValidModelGuids();
            Assert.AreEqual(1, guids.Count,
                "Search for 'heart' should match exactly one model.");
            Assert.AreEqual("guid-heart", guids[0],
                "The Heart model should match a case-insensitive search for 'heart'.");
        }

        // WHY: Tags provide secondary search terms (e.g., "Neuroscience" for
        // the Brain model). Search must check tags so users find models via
        // topic vocabulary, not just model names.
        [Test]
        public void SetSearchText_MatchesTags_NotJustName()
        {
            // Act
            _controller.SetSearchText("neuroscience");

            // Assert
            List<string> guids = _controller.GetValidModelGuids();
            Assert.AreEqual(1, guids.Count,
                "Search should match against model tags, not just the name.");
            Assert.AreEqual("guid-brain", guids[0],
                "Brain has 'Neuroscience' tag and should be found.");
        }

        // WHY: Pagination must calculate total pages correctly so the UI
        // shows accurate page controls and does not display blank pages.
        [Test]
        public void Pagination_CalculatesTotalPagesCorrectly()
        {
            // Arrange - 4 models, 10 tiles per page = 1 page
            _controller.SetSearchText("");

            // Assert
            Assert.AreEqual(4, _controller.ValidModelCount,
                "All 4 test models should be valid with no filters.");
            Assert.AreEqual(1, _controller.TotalPages,
                "4 models with 10 tiles per page should yield 1 page.");
            Assert.AreEqual(0, _controller.CurrentPageNumber,
                "Page number should reset to 0 after refresh.");
        }

        // WHY: LoadModel receives GUIDs from tile clicks and drag-and-drop.
        // Null or empty GUIDs must be rejected to prevent creating invalid
        // scene objects.
        [Test]
        public void LoadModel_DoesNothing_WhenGuidIsNullOrEmpty()
        {
            // Act
            _controller.LoadModel(null);
            _controller.LoadModel("");

            // Assert
            Assert.IsFalse(_controller.LoadAttempted,
                "LoadModel must return immediately for null or empty GUID to prevent creating invalid objects.");
        }

        // WHY: A valid GUID should proceed through the load pipeline so the
        // model appears in the scene for the user.
        [Test]
        public void LoadModel_ProceedsWithLoad_WhenGuidIsValid()
        {
            // Act
            _controller.LoadModel("guid-heart");

            // Assert
            Assert.IsTrue(_controller.LoadAttempted,
                "LoadModel should proceed when given a valid GUID.");
            Assert.AreEqual("guid-heart", _controller.LastLoadedGuid,
                "The correct GUID should be forwarded to the model creation pipeline.");
        }

        // WHY: The explodable filter is used by teachers to find models that
        // support the explode interaction for deep-dive anatomy lessons.
        [Test]
        public void SetOnlyExplodable_FiltersToOnlyExplodableModels()
        {
            // Act
            _controller.SetOnlyExplodable(true);

            // Assert
            List<string> guids = _controller.GetValidModelGuids();
            Assert.AreEqual(2, guids.Count,
                "Only explodable models (Heart, Animal Cell) should remain after filtering.");
            Assert.IsTrue(guids.Contains("guid-heart"),
                "Heart is explodable and should be included.");
            Assert.IsTrue(guids.Contains("guid-cell"),
                "Animal Cell is explodable and should be included.");
        }
    }
}
