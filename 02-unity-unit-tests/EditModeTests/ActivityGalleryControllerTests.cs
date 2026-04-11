// =============================================================================
// ActivityGalleryControllerTests.cs - Edit Mode Unit Tests for ActivityGalleryController
// =============================================================================
// TARGET CLASS: ActivityGalleryController
//   Real file: Assets/StudioA3/ActivityGalleryController.cs
//
// WHAT IT TESTS:
//   Activity selection gallery with filtering, search, pagination, and sorting.
//   Validates that search text matching, topic-based filtering, combined filters,
//   alphabetic-before-numeric sorting, and page count calculations all behave
//   correctly.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment and
//      replace the using directives with the real namespaces.
//   3. The real ActivityGalleryController is a MonoBehaviour. These tests exercise
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
    /// Minimal stand-in for the real ActivityInfo data class.
    /// </summary>
    public class ActivityInfo
    {
        public string Id;
        public string Name;
        public string Category;
        public List<string> Topics;
        public string SearchableText;

        public ActivityInfo()
        {
            Topics = new List<string>();
            SearchableText = "";
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API of the real
    /// ActivityGalleryController, without requiring MonoBehaviour.
    /// </summary>
    public class ActivityGalleryControllerStub
    {
        public List<ActivityInfo> AllActivities = new List<ActivityInfo>();
        public string SearchFilter = "";
        public HashSet<string> ToggledTopics = new HashSet<string>();
        public int CurrentPage = 0;
        public int ItemsPerPage = 6;

        private List<ActivityInfo> _validActivities = new List<ActivityInfo>();

        /// <summary>
        /// Returns true if the activity matches the current search filter and
        /// toggled topic filters.
        /// </summary>
        public bool ValidateActivity(ActivityInfo activity)
        {
            if (activity == null)
                return false;

            // Check search filter (case-insensitive)
            if (!string.IsNullOrEmpty(SearchFilter))
            {
                string lowerSearch = SearchFilter.ToLowerInvariant();
                string lowerSearchable = (activity.SearchableText ?? "").ToLowerInvariant();
                string lowerName = (activity.Name ?? "").ToLowerInvariant();
                if (!lowerSearchable.Contains(lowerSearch) && !lowerName.Contains(lowerSearch))
                    return false;
            }

            // Check topic filters
            if (ToggledTopics.Count > 0)
            {
                if (activity.Topics == null || !activity.Topics.Any(t => ToggledTopics.Contains(t)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Builds the filtered list of valid activities based on current filters.
        /// </summary>
        public void SetValidActivities()
        {
            _validActivities = AllActivities.Where(a => ValidateActivity(a)).ToList();
        }

        /// <summary>
        /// Returns the total number of pages needed to display valid activities.
        /// </summary>
        public int GetPageCount()
        {
            if (_validActivities.Count == 0)
                return 0;
            return (int)Math.Ceiling((double)_validActivities.Count / ItemsPerPage);
        }

        /// <summary>
        /// Returns the current filtered activity list.
        /// </summary>
        public List<ActivityInfo> GetValidActivities()
        {
            return _validActivities;
        }

        /// <summary>
        /// Custom string comparison that sorts alphabetic strings before numeric ones.
        /// </summary>
        public int CustomCompare(string a, string b)
        {
            bool aStartsWithDigit = a.Length > 0 && char.IsDigit(a[0]);
            bool bStartsWithDigit = b.Length > 0 && char.IsDigit(b[0]);

            if (aStartsWithDigit && !bStartsWithDigit)
                return 1; // numeric after alpha
            if (!aStartsWithDigit && bStartsWithDigit)
                return -1; // alpha before numeric

            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ActivityGalleryControllerTests
    {
        private ActivityGalleryControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new ActivityGalleryControllerStub();

            _controller.AllActivities = new List<ActivityInfo>
            {
                new ActivityInfo
                {
                    Id = "solar-system-tour",
                    Name = "Solar System Tour",
                    Category = "Astronomy",
                    Topics = new List<string> { "Planets", "Space" },
                    SearchableText = "solar system tour planets orbits space"
                },
                new ActivityInfo
                {
                    Id = "heart-dissection",
                    Name = "Heart Dissection",
                    Category = "Biology",
                    Topics = new List<string> { "Anatomy", "Organs" },
                    SearchableText = "heart dissection anatomy cardiovascular"
                },
                new ActivityInfo
                {
                    Id = "volcano-simulation",
                    Name = "Volcano Simulation",
                    Category = "Geology",
                    Topics = new List<string> { "Geology", "Planets" },
                    SearchableText = "volcano eruption lava tectonic simulation"
                },
                new ActivityInfo
                {
                    Id = "cell-explorer",
                    Name = "Cell Explorer",
                    Category = "Biology",
                    Topics = new List<string> { "Cells", "Anatomy" },
                    SearchableText = "cell membrane nucleus mitochondria biology"
                },
                new ActivityInfo
                {
                    Id = "periodic-table",
                    Name = "Periodic Table Builder",
                    Category = "Chemistry",
                    Topics = new List<string> { "Elements", "Chemistry" },
                    SearchableText = "periodic table elements atoms chemistry"
                },
                new ActivityInfo
                {
                    Id = "dna-structure",
                    Name = "DNA Structure",
                    Category = "Biology",
                    Topics = new List<string> { "Genetics", "Cells" },
                    SearchableText = "dna double helix genetics nucleotide"
                },
                new ActivityInfo
                {
                    Id = "simple-machines",
                    Name = "Simple Machines Lab",
                    Category = "Physics",
                    Topics = new List<string> { "Mechanics", "Forces" },
                    SearchableText = "lever pulley inclined plane simple machines"
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        // ---------------------------------------------------------------------
        // Test: ValidateActivity returns true when no filters are set
        // ---------------------------------------------------------------------
        // WHY: The default gallery state shows all activities. If the no-filter
        //      case incorrectly hides activities, students see an empty gallery
        //      on launch -- a critical first-time user experience failure.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateActivity_NoFiltersSet_ReturnsTrue()
        {
            // Arrange -- no search text, no toggled topics (defaults)
            var activity = _controller.AllActivities[0];

            // Act
            bool result = _controller.ValidateActivity(activity);

            // Assert
            Assert.IsTrue(result,
                "ValidateActivity should return true when no search or topic filters are active");
        }

        // ---------------------------------------------------------------------
        // Test: ValidateActivity filters by search text case-insensitively
        // ---------------------------------------------------------------------
        // WHY: Students and teachers type search queries in mixed case. If search
        //      is case-sensitive, a query for "heart" won't find "Heart Dissection"
        //      which defeats the purpose of search.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateActivity_SearchTextMatch_FiltersCaseInsensitive()
        {
            // Arrange -- search for "HEART" in uppercase
            _controller.SearchFilter = "HEART";
            var heartActivity = _controller.AllActivities.Find(a => a.Id == "heart-dissection");
            var volcanoActivity = _controller.AllActivities.Find(a => a.Id == "volcano-simulation");

            // Act & Assert
            Assert.IsTrue(_controller.ValidateActivity(heartActivity),
                "ValidateActivity should match 'HEART' against 'heart dissection' case-insensitively");
            Assert.IsFalse(_controller.ValidateActivity(volcanoActivity),
                "ValidateActivity should reject activities that do not match the search text");
        }

        // ---------------------------------------------------------------------
        // Test: ValidateActivity filters by toggled topics
        // ---------------------------------------------------------------------
        // WHY: Teachers filter activities by curriculum topic to find content
        //      aligned with their lesson plan. Incorrect topic filtering causes
        //      teachers to miss relevant activities or see irrelevant ones.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateActivity_TopicFilter_ReturnsOnlyMatchingTopics()
        {
            // Arrange -- toggle the "Anatomy" topic
            _controller.ToggledTopics.Add("Anatomy");

            var heartActivity = _controller.AllActivities.Find(a => a.Id == "heart-dissection");
            var cellActivity = _controller.AllActivities.Find(a => a.Id == "cell-explorer");
            var volcanoActivity = _controller.AllActivities.Find(a => a.Id == "volcano-simulation");

            // Act & Assert
            Assert.IsTrue(_controller.ValidateActivity(heartActivity),
                "Heart Dissection has the 'Anatomy' topic and should pass the filter");
            Assert.IsTrue(_controller.ValidateActivity(cellActivity),
                "Cell Explorer has the 'Anatomy' topic and should pass the filter");
            Assert.IsFalse(_controller.ValidateActivity(volcanoActivity),
                "Volcano Simulation does not have the 'Anatomy' topic and should be filtered out");
        }

        // ---------------------------------------------------------------------
        // Test: Combined search text + topic filter works correctly
        // ---------------------------------------------------------------------
        // WHY: Users often combine search with topic filters. Both constraints
        //      must be satisfied simultaneously or results become misleading.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateActivity_CombinedSearchAndTopic_BothMustMatch()
        {
            // Arrange -- search for "cell" AND require "Anatomy" topic
            _controller.SearchFilter = "cell";
            _controller.ToggledTopics.Add("Anatomy");

            var cellActivity = _controller.AllActivities.Find(a => a.Id == "cell-explorer");
            var heartActivity = _controller.AllActivities.Find(a => a.Id == "heart-dissection");
            var dnaActivity = _controller.AllActivities.Find(a => a.Id == "dna-structure");

            // Act & Assert
            Assert.IsTrue(_controller.ValidateActivity(cellActivity),
                "Cell Explorer matches both 'cell' search and 'Anatomy' topic");
            Assert.IsFalse(_controller.ValidateActivity(heartActivity),
                "Heart Dissection has 'Anatomy' topic but does not match 'cell' search text");
            Assert.IsFalse(_controller.ValidateActivity(dnaActivity),
                "DNA Structure does not have 'Anatomy' topic even though its text may mention cells");
        }

        // ---------------------------------------------------------------------
        // Test: CustomCompare sorts alphabetic strings before numeric strings
        // ---------------------------------------------------------------------
        // WHY: Activity names that start with numbers (e.g., "3D Heart Model")
        //      should appear after alphabetic names in the gallery for consistent
        //      browsing. This matches user expectations from file explorers.
        // ---------------------------------------------------------------------
        [Test]
        public void CustomCompare_AlphabeticBeforeNumeric_SortsCorrectly()
        {
            // Act
            int alphaVsNumeric = _controller.CustomCompare("Anatomy Lab", "3D Heart");
            int numericVsAlpha = _controller.CustomCompare("3D Heart", "Anatomy Lab");
            int alphaVsAlpha = _controller.CustomCompare("Anatomy Lab", "Biology Basics");

            // Assert
            Assert.Less(alphaVsNumeric, 0,
                "Alphabetic 'Anatomy Lab' should sort before numeric '3D Heart'");
            Assert.Greater(numericVsAlpha, 0,
                "Numeric '3D Heart' should sort after alphabetic 'Anatomy Lab'");
            Assert.Less(alphaVsAlpha, 0,
                "Among alphabetic strings, 'Anatomy Lab' should sort before 'Biology Basics'");
        }

        // ---------------------------------------------------------------------
        // Test: Pagination calculates correct page count
        // ---------------------------------------------------------------------
        // WHY: Incorrect page count causes either missing activities on the last
        //      page or empty pages that confuse users. The math must handle
        //      non-even divisions correctly (ceiling division).
        // ---------------------------------------------------------------------
        [Test]
        public void GetPageCount_WithSevenActivities_ReturnsCorrectPageCount()
        {
            // Arrange -- 7 activities, 6 per page = 2 pages
            _controller.ItemsPerPage = 6;
            _controller.SetValidActivities(); // builds filtered list (all 7 pass, no filters)

            // Act
            int pageCount = _controller.GetPageCount();

            // Assert
            Assert.AreEqual(2, pageCount,
                "7 activities with 6 per page should yield 2 pages (ceiling division)");
        }

        // ---------------------------------------------------------------------
        // Test: Empty search filter shows all activities
        // ---------------------------------------------------------------------
        // WHY: Clearing the search box should restore the full gallery view.
        //      If empty search accidentally filters everything out, students
        //      are stranded with an empty gallery.
        // ---------------------------------------------------------------------
        [Test]
        public void SetValidActivities_EmptySearch_ShowsAllActivities()
        {
            // Arrange -- explicitly set empty search
            _controller.SearchFilter = "";
            _controller.ToggledTopics.Clear();

            // Act
            _controller.SetValidActivities();

            // Assert
            Assert.AreEqual(7, _controller.GetValidActivities().Count,
                "Empty search with no topic filters should show all 7 activities");
        }

        // ---------------------------------------------------------------------
        // Test: No matching activities returns empty filtered list
        // ---------------------------------------------------------------------
        // WHY: When search yields no results, the UI must display an appropriate
        //      "no results" message. The filtered list must be genuinely empty
        //      (not null) to avoid NullReferenceExceptions in the UI layer.
        // ---------------------------------------------------------------------
        [Test]
        public void SetValidActivities_NoMatchingSearch_ReturnsEmptyList()
        {
            // Arrange -- search for something that matches nothing
            _controller.SearchFilter = "quantum entanglement xyz";

            // Act
            _controller.SetValidActivities();

            // Assert
            Assert.AreEqual(0, _controller.GetValidActivities().Count,
                "Search for non-existent text should return zero matching activities");
            Assert.AreEqual(0, _controller.GetPageCount(),
                "Page count should be 0 when no activities match the search");
        }
    }
}
