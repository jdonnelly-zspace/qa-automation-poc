// =============================================================================
// ContentPackManagerTests.cs - Edit Mode Unit Tests for ContentPackManager
// =============================================================================
// TARGET CLASS: ContentPackManager
//   Real file: Assets/CommonA3/zSpace/Scripts/ContentPack/ContentPackManager.cs
//
// WHAT IT TESTS:
//   The singleton that loads content packs (activities + models) and manages
//   licensing availability. Validates category/topic filtering, licensing state
//   updates that mark packs unavailable, aggregation of referenced supplement
//   IDs, and graceful handling of empty content collections.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real ContentPackManager is a singleton MonoBehaviour that loads
//      packs from StreamingAssets. This stub uses in-memory lists so the
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
    /// Mirrors a single activity entry within a content pack.
    /// </summary>
    public class ContentActivity
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public string Topic { get; set; }
        public bool IsAvailable { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors a content pack containing activities and licensing metadata.
    /// </summary>
    public class ContentPack
    {
        public string Name { get; set; }
        public string LicensingModeId { get; set; }
        public List<string> LicensingSupplementIds { get; set; } = new List<string>();
        public List<ContentActivity> Activities { get; set; } = new List<ContentActivity>();
        public bool IsAvailable { get; set; } = true;
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight stand-in for the real ContentPackManager singleton.
    /// Replicates the filtering, licensing, and aggregation API.
    /// </summary>
    public class ContentPackManagerStub
    {
        private readonly List<ContentPack> _contentPacks = new List<ContentPack>();

        public void AddContentPack(ContentPack pack)
        {
            _contentPacks.Add(pack);
        }

        public List<ContentActivity> GetActivityByCategory(string category)
        {
            return _contentPacks
                .SelectMany(p => p.Activities)
                .Where(a => string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<ContentActivity> GetActivityByTopic(string topic)
        {
            return _contentPacks
                .SelectMany(p => p.Activities)
                .Where(a => string.Equals(a.Topic, topic, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Updates availability of all content packs based on the current
        /// licensing state. A pack is available when its licensing mode ID
        /// matches and all its required supplement IDs are present.
        /// </summary>
        public void UpdateAllContentAvailabilityForLicensingState(
            string modeId, IReadOnlyList<string> supplementIds)
        {
            var supplementSet = new HashSet<string>(supplementIds);

            foreach (ContentPack pack in _contentPacks)
            {
                bool modeMatch = string.Equals(pack.LicensingModeId, modeId,
                    StringComparison.OrdinalIgnoreCase);
                bool supplementsOk = pack.LicensingSupplementIds
                    .All(id => supplementSet.Contains(id));

                pack.IsAvailable = modeMatch && supplementsOk;

                foreach (ContentActivity activity in pack.Activities)
                {
                    activity.IsAvailable = pack.IsAvailable;
                }
            }
        }

        /// <summary>
        /// Aggregates all referenced licensing supplement IDs across every
        /// loaded content pack. Used by the licensing system to know which
        /// supplements to check.
        /// </summary>
        public IReadOnlyList<string> AllReferencedLicensingSupplementIds
        {
            get
            {
                return _contentPacks
                    .SelectMany(p => p.LicensingSupplementIds)
                    .Distinct()
                    .ToList()
                    .AsReadOnly();
            }
        }

        public int ContentPackCount => _contentPacks.Count;
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ContentPackManagerTests
    {
        private ContentPackManagerStub _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new ContentPackManagerStub();

            // WHY: Seed with realistic zSpace content packs that span multiple
            // categories and licensing configurations to exercise filtering and
            // licensing logic.
            _manager.AddContentPack(new ContentPack
            {
                Name = "Science Essentials",
                LicensingModeId = "mode_standard",
                LicensingSupplementIds = new List<string>(),
                Activities = new List<ContentActivity>
                {
                    new ContentActivity { Title = "Solar System Explorer", Category = "Science", Topic = "Astronomy", IsAvailable = true },
                    new ContentActivity { Title = "Volcano Formation", Category = "Science", Topic = "Geology", IsAvailable = true }
                }
            });

            _manager.AddContentPack(new ContentPack
            {
                Name = "Art Studio",
                LicensingModeId = "mode_standard",
                LicensingSupplementIds = new List<string> { "supplement_art_tools" },
                Activities = new List<ContentActivity>
                {
                    new ContentActivity { Title = "3D Sculpture Workshop", Category = "Art", Topic = "Sculpting", IsAvailable = true }
                }
            });

            _manager.AddContentPack(new ContentPack
            {
                Name = "Advanced Biology",
                LicensingModeId = "mode_premium",
                LicensingSupplementIds = new List<string> { "supplement_dissection", "supplement_art_tools" },
                Activities = new List<ContentActivity>
                {
                    new ContentActivity { Title = "Frog Dissection Lab", Category = "Science", Topic = "Anatomy", IsAvailable = true },
                    new ContentActivity { Title = "Human Heart Explorer", Category = "Science", Topic = "Anatomy", IsAvailable = true }
                }
            });
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
        }

        [Test]
        public void GetActivityByCategory_ReturnsCorrectActivities_ForScience()
        {
            // WHY: Teachers browse content by category; Science should return
            // activities from multiple packs that share the same category.

            // Act
            List<ContentActivity> results = _manager.GetActivityByCategory("Science");

            // Assert
            Assert.AreEqual(4, results.Count,
                "Should return all 4 Science activities across multiple content packs.");
            Assert.IsTrue(results.All(a => a.Category == "Science"),
                "Every returned activity should belong to the Science category.");
        }

        [Test]
        public void GetActivityByCategory_ReturnsEmpty_ForUnknownCategory()
        {
            // WHY: The UI must not crash when a category filter matches nothing.

            // Act
            List<ContentActivity> results = _manager.GetActivityByCategory("Mathematics");

            // Assert
            Assert.IsNotNull(results,
                "Result should never be null, even when no activities match.");
            Assert.AreEqual(0, results.Count,
                "No activities should match an unknown category.");
        }

        [Test]
        public void GetActivityByTopic_ReturnsMatchingActivities_ForAnatomy()
        {
            // WHY: Topic filtering lets students drill into specific subject
            // areas; Anatomy spans multiple activities in the Advanced Biology pack.

            // Act
            List<ContentActivity> results = _manager.GetActivityByTopic("Anatomy");

            // Assert
            Assert.AreEqual(2, results.Count,
                "Two activities (Frog Dissection Lab, Human Heart Explorer) have the Anatomy topic.");
            Assert.IsTrue(results.Any(a => a.Title == "Frog Dissection Lab"),
                "Frog Dissection Lab should be included in Anatomy results.");
            Assert.IsTrue(results.Any(a => a.Title == "Human Heart Explorer"),
                "Human Heart Explorer should be included in Anatomy results.");
        }

        [Test]
        public void UpdateAvailability_MarksPacksUnavailable_WhenModeDoesNotMatch()
        {
            // WHY: Schools with a standard license should not see premium content.
            // Licensing enforcement prevents access to unpurchased packs.

            // Act - activate standard mode only, no supplements
            _manager.UpdateAllContentAvailabilityForLicensingState(
                "mode_standard", new List<string>());

            // Assert
            List<ContentActivity> scienceBasic = _manager.GetActivityByTopic("Astronomy");
            List<ContentActivity> anatomyPremium = _manager.GetActivityByTopic("Anatomy");

            Assert.IsTrue(scienceBasic[0].IsAvailable,
                "Solar System Explorer requires mode_standard with no supplements, should be available.");
            Assert.IsFalse(anatomyPremium[0].IsAvailable,
                "Frog Dissection Lab requires mode_premium, should be unavailable under mode_standard.");
        }

        [Test]
        public void UpdateAvailability_MarksPackUnavailable_WhenSupplementsMissing()
        {
            // WHY: Some content packs require add-on supplements. Even if the
            // mode matches, missing supplements must block availability.

            // Act - standard mode with no supplements (Art Studio needs supplement_art_tools)
            _manager.UpdateAllContentAvailabilityForLicensingState(
                "mode_standard", new List<string>());

            // Assert
            List<ContentActivity> artResults = _manager.GetActivityByCategory("Art");
            Assert.AreEqual(1, artResults.Count,
                "Should find the Art activity.");
            Assert.IsFalse(artResults[0].IsAvailable,
                "3D Sculpture Workshop requires supplement_art_tools which is not provided.");
        }

        [Test]
        public void UpdateAvailability_MarksAllAvailable_WhenFullyLicensed()
        {
            // WHY: A fully licensed school should see all content available.

            // Act - premium mode with all supplements
            _manager.UpdateAllContentAvailabilityForLicensingState(
                "mode_premium", new List<string> { "supplement_art_tools", "supplement_dissection" });

            // Assert
            List<ContentActivity> anatomy = _manager.GetActivityByTopic("Anatomy");
            Assert.IsTrue(anatomy.All(a => a.IsAvailable),
                "All Anatomy activities should be available with premium mode and all supplements.");
        }

        [Test]
        public void AllReferencedLicensingSupplementIds_AggregatesFromAllPacks()
        {
            // WHY: The licensing system needs a complete set of supplement IDs
            // to validate against the license server. Duplicates must be removed.

            // Act
            IReadOnlyList<string> allIds = _manager.AllReferencedLicensingSupplementIds;

            // Assert
            Assert.AreEqual(2, allIds.Count,
                "Should contain exactly 2 distinct supplement IDs (supplement_art_tools, supplement_dissection).");
            Assert.IsTrue(allIds.Contains("supplement_art_tools"),
                "supplement_art_tools is referenced by Art Studio and Advanced Biology packs.");
            Assert.IsTrue(allIds.Contains("supplement_dissection"),
                "supplement_dissection is referenced by Advanced Biology pack.");
        }

        [Test]
        public void EmptyContentPacks_HandledGracefully()
        {
            // WHY: On first launch or in error states the manager may have no
            // content packs loaded. All operations must degrade gracefully.

            // Arrange - fresh empty manager
            var emptyManager = new ContentPackManagerStub();

            // Act & Assert
            List<ContentActivity> results = emptyManager.GetActivityByCategory("Science");
            Assert.IsNotNull(results,
                "GetActivityByCategory should return an empty list, not null.");
            Assert.AreEqual(0, results.Count,
                "No activities should be returned from an empty manager.");

            IReadOnlyList<string> supplementIds = emptyManager.AllReferencedLicensingSupplementIds;
            Assert.IsNotNull(supplementIds,
                "AllReferencedLicensingSupplementIds should return an empty list, not null.");
            Assert.AreEqual(0, supplementIds.Count,
                "No supplement IDs should exist in an empty manager.");

            // Licensing update on empty manager should not throw
            Assert.DoesNotThrow(() =>
                emptyManager.UpdateAllContentAvailabilityForLicensingState(
                    "mode_standard", new List<string>()),
                "Licensing update on an empty manager should not throw.");
        }
    }
}
