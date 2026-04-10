// =============================================================================
// ActivityPackManagerTests.cs - Edit Mode Unit Tests for ActivityPackManager
// =============================================================================
// TARGET CLASS: ActivityPackManager
//   Real file: Assets/zSpace/StudioA3/Scripts/ActivityPack/ActivityPackManager.cs
//
// WHAT IT TESTS:
//   The system that loads, filters, and manages activity packs for the Studio
//   A3 educational experience. Validates category/topic filtering, index
//   bounds, and licensing availability logic.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real ActivityPackManager is a singleton MonoBehaviour that loads
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
    /// Mirrors the real ActivityData scriptable object fields used in tests.
    /// </summary>
    public class ActivityData
    {
        public string Guid { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Topic { get; set; }
        public string Subject { get; set; }
        public bool IsAvailable { get; set; }
        public List<string> ActivityModeIds { get; set; } = new List<string>();
        public List<string> RequiredSupplementIds { get; set; } = new List<string>();
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight stand-in for the real ActivityPackManager singleton.
    /// Replicates the filtering and availability API used by the UI layer.
    /// </summary>
    public class ActivityPackManagerStub
    {
        private readonly List<ActivityData> _activities = new List<ActivityData>();

        public void AddActivity(ActivityData activity)
        {
            _activities.Add(activity);
        }

        public ActivityData GetActivity(int index)
        {
            if (index < 0 || index >= _activities.Count)
            {
                return null;
            }

            return _activities[index];
        }

        public List<ActivityData> GetActivityByCategory(string category)
        {
            return _activities
                .Where(a => string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<ActivityData> GetActivityByTopic(string topic)
        {
            return _activities
                .Where(a => string.Equals(a.Topic, topic, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Mirrors the private ComputeActivityAvailabilityForLicensingState
        /// logic, exposed here as a public method for testability.
        /// An activity is available when ALL of its required modes are in
        /// the active set AND ALL of its required supplements are present.
        /// </summary>
        public void UpdateAllActivityAvailabilityForLicensingState(
            HashSet<string> activeModeIds,
            HashSet<string> availableSupplementIds)
        {
            foreach (ActivityData activity in _activities)
            {
                bool modesOk = activity.ActivityModeIds
                    .All(id => activeModeIds.Contains(id));
                bool supplementsOk = activity.RequiredSupplementIds
                    .All(id => availableSupplementIds.Contains(id));

                activity.IsAvailable = modesOk && supplementsOk;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ActivityPackManagerTests
    {
        private ActivityPackManagerStub _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new ActivityPackManagerStub();

            // Seed with representative activities
            _manager.AddActivity(new ActivityData
            {
                Guid = "aaa-111",
                Title = "Frog Dissection",
                Category = "Biology",
                Topic = "Anatomy",
                Subject = "Science",
                ActivityModeIds = new List<string> { "mode_ar" },
                RequiredSupplementIds = new List<string>()
            });

            _manager.AddActivity(new ActivityData
            {
                Guid = "bbb-222",
                Title = "Cell Structure",
                Category = "Biology",
                Topic = "Cells",
                Subject = "Science",
                ActivityModeIds = new List<string> { "mode_ar", "mode_vr" },
                RequiredSupplementIds = new List<string> { "supplement_microscope" }
            });

            _manager.AddActivity(new ActivityData
            {
                Guid = "ccc-333",
                Title = "Solar System",
                Category = "Astronomy",
                Topic = "Planets",
                Subject = "Science",
                ActivityModeIds = new List<string> { "mode_ar" },
                RequiredSupplementIds = new List<string>()
            });
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
        }

        [Test]
        public void GetActivityByCategory_ReturnsMatchingActivities()
        {
            // Act
            List<ActivityData> results = _manager.GetActivityByCategory("Biology");

            // Assert
            Assert.AreEqual(2, results.Count,
                "Should return both Biology activities.");
            Assert.IsTrue(results.All(a => a.Category == "Biology"),
                "Every returned activity should belong to the Biology category.");
        }

        [Test]
        public void GetActivityByCategory_ReturnsEmptyForUnknown()
        {
            // Act
            List<ActivityData> results = _manager.GetActivityByCategory("Chemistry");

            // Assert
            Assert.IsNotNull(results,
                "Result should never be null, even when nothing matches.");
            Assert.AreEqual(0, results.Count,
                "No activities should match an unknown category.");
        }

        [Test]
        public void GetActivityByTopic_ReturnsMatchingActivities()
        {
            // Act
            List<ActivityData> results = _manager.GetActivityByTopic("Anatomy");

            // Assert
            Assert.AreEqual(1, results.Count,
                "Exactly one activity has the Anatomy topic.");
            Assert.AreEqual("Frog Dissection", results[0].Title);
        }

        [Test]
        public void GetActivity_ReturnsNullForOutOfRangeIndex()
        {
            // Act & Assert
            Assert.IsNull(_manager.GetActivity(-1),
                "Negative index should return null.");
            Assert.IsNull(_manager.GetActivity(999),
                "Index beyond count should return null.");
        }

        [Test]
        public void ActivityAvailability_ChecksModesAndSupplements()
        {
            // Arrange - license only has mode_ar, and no supplements
            var activeModes = new HashSet<string> { "mode_ar" };
            var availableSupplements = new HashSet<string>();

            // Act
            _manager.UpdateAllActivityAvailabilityForLicensingState(
                activeModes, availableSupplements);

            // Assert
            ActivityData frog = _manager.GetActivity(0);   // requires mode_ar only
            ActivityData cell = _manager.GetActivity(1);   // requires mode_ar + mode_vr + supplement
            ActivityData solar = _manager.GetActivity(2);  // requires mode_ar only

            Assert.IsTrue(frog.IsAvailable,
                "Frog Dissection needs only mode_ar, which is active.");
            Assert.IsFalse(cell.IsAvailable,
                "Cell Structure needs mode_vr and supplement_microscope, both missing.");
            Assert.IsTrue(solar.IsAvailable,
                "Solar System needs only mode_ar, which is active.");

            // Now enable everything
            activeModes.Add("mode_vr");
            availableSupplements.Add("supplement_microscope");
            _manager.UpdateAllActivityAvailabilityForLicensingState(
                activeModes, availableSupplements);

            Assert.IsTrue(cell.IsAvailable,
                "Cell Structure should be available once all modes and supplements are present.");
        }
    }
}
