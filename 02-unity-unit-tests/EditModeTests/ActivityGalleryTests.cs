// =============================================================================
// ActivityGalleryTests.cs - Edit Mode Unit Tests for Activity Gallery System
// =============================================================================
// PURPOSE: Demonstrates how to write Edit Mode tests for the ActivityGalleryController,
//          which loads and manages educational activities in zSpace applications.
//
// TEMPLATE NOTICE: This file is a POC template. Search for "TODO" comments to find
//          every place you need to adapt to match your actual class names, methods,
//          and namespaces. The test PATTERNS are the important part -- the specific
//          assertions will change once wired to real code.
//
// HOW TO USE:
//   1. Copy this file into Assets/Tests/EditModeTests/ in your Unity project
//   2. Update the "using" directives to reference your actual namespaces
//   3. Replace the stub class at the bottom with references to your real classes
//   4. Update .asmdef references to include your project's assembly
//   5. Run via Window > General > Test Runner > EditMode tab
// =============================================================================

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// TODO: Replace this with the actual namespace where ActivityGalleryController lives
// Example: using zSpace.Activities;
// Example: using zSpace.UI.Gallery;

namespace QAAutomation.EditModeTests
{
    /// <summary>
    /// Tests for the Activity Gallery system that loads and presents educational
    /// activities to students. These tests run in Edit Mode (no Play Mode required),
    /// making them fast and suitable for CI pipelines.
    /// </summary>
    [TestFixture]
    public class ActivityGalleryTests
    {
        // ---------------------------------------------------------------------
        // Test fixtures -- created fresh before each test, torn down after
        // ---------------------------------------------------------------------

        // TODO: Replace with your actual ActivityGalleryController type
        private ActivityGalleryControllerStub _gallery;
        private List<ActivityDataStub> _sampleActivities;

        /// <summary>
        /// Runs before EACH test method. Creates a clean gallery instance with
        /// known test data so tests are isolated and deterministic.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _gallery = new ActivityGalleryControllerStub();

            // Build a known set of sample activities for testing
            _sampleActivities = new List<ActivityDataStub>
            {
                new ActivityDataStub
                {
                    Id = "bio-cell-structure",
                    Title = "Cell Structure",
                    Subject = "Biology",
                    GradeLevel = "9-12"
                },
                new ActivityDataStub
                {
                    Id = "chem-periodic-table",
                    Title = "Periodic Table Explorer",
                    Subject = "Chemistry",
                    GradeLevel = "9-12"
                },
                new ActivityDataStub
                {
                    Id = "phys-simple-machines",
                    Title = "Simple Machines",
                    Subject = "Physics",
                    GradeLevel = "6-8"
                },
                new ActivityDataStub
                {
                    Id = "bio-heart-anatomy",
                    Title = "Heart Anatomy",
                    Subject = "Biology",
                    GradeLevel = "9-12"
                }
            };
        }

        /// <summary>
        /// Runs after EACH test method. Cleans up any resources to prevent
        /// test pollution. Essential when tests create Unity objects.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _gallery = null;
            _sampleActivities = null;
        }

        // ---------------------------------------------------------------------
        // Test: Loading activities populates the gallery correctly
        // ---------------------------------------------------------------------
        // WHY: The gallery is the primary entry point for students. If activities
        //      fail to load, the entire application is unusable. This is the most
        //      critical happy-path test.
        // ---------------------------------------------------------------------
        [Test]
        public void LoadActivities_WithValidData_PopulatesGalleryWithCorrectCount()
        {
            // Arrange -- feed the gallery our known sample data
            _gallery.LoadActivities(_sampleActivities);

            // Act -- query how many activities the gallery thinks it has
            int count = _gallery.ActivityCount;

            // Assert -- should match exactly what we loaded
            Assert.AreEqual(4, count,
                "Gallery should contain exactly 4 activities after loading sample data");
        }

        // ---------------------------------------------------------------------
        // Test: Filtering by subject returns only matching activities
        // ---------------------------------------------------------------------
        // WHY: Teachers filter activities by subject area. If filtering is broken,
        //      teachers cannot find relevant content, which is a top user complaint
        //      scenario. We test both the count AND the content of results.
        // ---------------------------------------------------------------------
        [Test]
        public void FilterBySubject_Biology_ReturnsOnlyBiologyActivities()
        {
            // Arrange
            _gallery.LoadActivities(_sampleActivities);

            // Act -- filter to just Biology activities
            // TODO: Replace with your actual filter method name
            List<ActivityDataStub> results = _gallery.FilterBySubject("Biology");

            // Assert -- we loaded 2 Biology activities (Cell Structure, Heart Anatomy)
            Assert.AreEqual(2, results.Count,
                "Filtering for 'Biology' should return exactly 2 activities");

            // Also verify the correct activities were returned, not just the count
            Assert.IsTrue(results.Exists(a => a.Id == "bio-cell-structure"),
                "Biology filter results should include 'Cell Structure'");
            Assert.IsTrue(results.Exists(a => a.Id == "bio-heart-anatomy"),
                "Biology filter results should include 'Heart Anatomy'");
        }

        // ---------------------------------------------------------------------
        // Test: Loading null data is handled gracefully (no crash)
        // ---------------------------------------------------------------------
        // WHY: In production, activity data comes from files on disk or network
        //      resources that can fail. The gallery must handle null/missing data
        //      without throwing an unhandled exception that crashes the app.
        //      This is a DEFENSIVE test -- it guards against regressions.
        // ---------------------------------------------------------------------
        [Test]
        public void LoadActivities_WithNullData_DoesNotThrowAndSetsCountToZero()
        {
            // Act & Assert -- loading null should NOT throw
            // TODO: Adapt based on how your real class handles null input
            Assert.DoesNotThrow(() => _gallery.LoadActivities(null),
                "Gallery should handle null activity data gracefully without throwing");

            // The gallery should report zero activities, not be in a broken state
            Assert.AreEqual(0, _gallery.ActivityCount,
                "Gallery should have 0 activities after loading null data");
        }

        // ---------------------------------------------------------------------
        // Test: Retrieving an activity by ID returns the correct activity
        // ---------------------------------------------------------------------
        // WHY: When a student clicks on an activity tile, the system looks it up
        //      by ID. If the wrong activity is returned, the student gets the
        //      wrong lesson -- a serious content integrity issue.
        // ---------------------------------------------------------------------
        [Test]
        public void GetActivityById_WithValidId_ReturnsCorrectActivity()
        {
            // Arrange
            _gallery.LoadActivities(_sampleActivities);

            // Act -- look up a specific activity
            // TODO: Replace with your actual lookup method
            ActivityDataStub result = _gallery.GetActivityById("chem-periodic-table");

            // Assert
            Assert.IsNotNull(result,
                "Looking up a valid activity ID should return a non-null result");
            Assert.AreEqual("Periodic Table Explorer", result.Title,
                "Returned activity should have the correct title");
            Assert.AreEqual("Chemistry", result.Subject,
                "Returned activity should have the correct subject");
        }

        // ---------------------------------------------------------------------
        // Test: Retrieving an activity with a non-existent ID returns null
        // ---------------------------------------------------------------------
        // WHY: Guards against crashes when invalid IDs are passed, which can
        //      happen from stale bookmarks, deep links, or data corruption.
        // ---------------------------------------------------------------------
        [Test]
        public void GetActivityById_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _gallery.LoadActivities(_sampleActivities);

            // Act
            ActivityDataStub result = _gallery.GetActivityById("nonexistent-id-12345");

            // Assert
            Assert.IsNull(result,
                "Looking up a non-existent activity ID should return null, not throw");
        }
    }

    // =========================================================================
    // STUB CLASSES -- Replace these with references to your actual classes
    // =========================================================================
    // These stubs exist so the test file compiles on its own for the POC demo.
    // In your real project, DELETE this entire section and use "using" directives
    // to reference your actual ActivityGalleryController and ActivityData classes.
    // =========================================================================

    // TODO: DELETE this stub and reference your real ActivityGalleryController
    public class ActivityGalleryControllerStub
    {
        private List<ActivityDataStub> _activities = new List<ActivityDataStub>();

        public int ActivityCount => _activities.Count;

        public void LoadActivities(List<ActivityDataStub> activities)
        {
            _activities = activities ?? new List<ActivityDataStub>();
        }

        public List<ActivityDataStub> FilterBySubject(string subject)
        {
            return _activities.FindAll(a => a.Subject == subject);
        }

        public ActivityDataStub GetActivityById(string id)
        {
            return _activities.Find(a => a.Id == id);
        }
    }

    // TODO: DELETE this stub and reference your real ActivityData class
    public class ActivityDataStub
    {
        public string Id;
        public string Title;
        public string Subject;
        public string GradeLevel;
    }
}
