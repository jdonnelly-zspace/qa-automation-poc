// =============================================================================
// ContentValidationTests.cs - Edit Mode Tests for Activity Content Integrity
// =============================================================================
// PURPOSE: Validates that activity content data (JSON/XML files, asset references,
//          metadata) is well-formed and complete WITHOUT running the game. These
//          tests bridge Prototype #1 (build-time content validation) into the
//          Unity Test Runner so they appear alongside code tests in a single report.
//
// WHY THIS MATTERS: Content issues (missing titles, broken asset paths, empty
//          descriptions) are the #1 source of bugs reported by teachers. Catching
//          them at edit time prevents broken activities from shipping.
//
// TEMPLATE NOTICE: Search for "TODO" to find every adaptation point. The file
//          paths, data formats, and field names WILL differ in your project.
//
// HOW TO USE:
//   1. Copy into Assets/Tests/EditModeTests/ in your Unity project
//   2. Update paths and data structures to match your activity data format
//   3. These tests run automatically in the Test Runner and in CI
// =============================================================================

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// TODO: Add using directives for your activity data model namespace
// Example: using zSpace.Activities.Data;

namespace QAAutomation.EditModeTests
{
    /// <summary>
    /// Validates activity content data files at edit time. Catches missing
    /// metadata, broken references, and malformed data before the application
    /// is built or deployed.
    /// </summary>
    [TestFixture]
    public class ContentValidationTests
    {
        // TODO: Update this path to where your activity data files live
        // This should be relative to the Unity project's Assets folder
        // Example: "Assets/StreamingAssets/Activities"
        // Example: "Assets/Resources/ActivityData"
        private const string ActivityDataPath = "Assets/StreamingAssets/Activities";

        // Loaded activity data for tests to validate
        private List<ActivityMetadataStub> _allActivities;

        [SetUp]
        public void SetUp()
        {
            // TODO: Replace this with your actual data loading logic.
            // In a real project, you would load JSON/XML files from disk:
            //
            //   string fullPath = Path.Combine(Application.dataPath, "StreamingAssets/Activities");
            //   var files = Directory.GetFiles(fullPath, "*.json", SearchOption.AllDirectories);
            //   _allActivities = files.Select(f => JsonUtility.FromJson<ActivityMetadata>(File.ReadAllText(f))).ToList();
            //
            // For this POC template, we use sample data to demonstrate the pattern.
            _allActivities = GetSampleActivityMetadata();
        }

        [TearDown]
        public void TearDown()
        {
            _allActivities = null;
        }

        // ---------------------------------------------------------------------
        // Test: Activity data files can be found and loaded
        // ---------------------------------------------------------------------
        // WHY: If the data folder is missing or empty, EVERY activity is broken.
        //      This is the first check that should run. In CI, it catches cases
        //      where data files were accidentally excluded from the build.
        // ---------------------------------------------------------------------
        [Test]
        public void ActivityDataFiles_Exist_AndCanBeLoaded()
        {
            // Assert -- we should have at least one activity
            Assert.IsNotNull(_allActivities,
                "Activity data list should not be null -- check data loading logic");
            Assert.Greater(_allActivities.Count, 0,
                "At least one activity should be loadable from the data directory. " +
                $"Check that activity files exist in: {ActivityDataPath}");

            // Log the count for visibility in test results
            Debug.Log($"[ContentValidation] Found {_allActivities.Count} activities to validate");
        }

        // ---------------------------------------------------------------------
        // Test: Every activity has a non-empty title and description
        // ---------------------------------------------------------------------
        // WHY: Empty titles show as blank tiles in the gallery. Empty descriptions
        //      leave teachers unable to understand what an activity teaches.
        //      These are the most common content authoring mistakes.
        // ---------------------------------------------------------------------
        [Test]
        public void AllActivities_HaveNonEmptyTitleAndDescription()
        {
            var failures = new List<string>();

            foreach (var activity in _allActivities)
            {
                if (string.IsNullOrWhiteSpace(activity.Title))
                {
                    failures.Add($"Activity '{activity.Id}' has an empty or null Title");
                }

                if (string.IsNullOrWhiteSpace(activity.Description))
                {
                    failures.Add($"Activity '{activity.Id}' has an empty or null Description");
                }
            }

            // Report ALL failures at once (not just the first one)
            Assert.IsEmpty(failures,
                "The following activities have missing metadata:\n  - " +
                string.Join("\n  - ", failures));
        }

        // ---------------------------------------------------------------------
        // Test: Every activity has a valid subject and grade level
        // ---------------------------------------------------------------------
        // WHY: Subject and grade level are used for filtering in the gallery
        //      and for standards alignment reporting. Invalid values cause
        //      activities to be invisible in filtered views.
        // ---------------------------------------------------------------------
        [Test]
        public void AllActivities_HaveValidSubjectAndGradeLevel()
        {
            // TODO: Update these to match your actual valid values
            var validSubjects = new HashSet<string>
            {
                "Biology", "Chemistry", "Physics", "Earth Science",
                "Anatomy", "Engineering", "Mathematics"
            };

            var validGradeLevels = new HashSet<string>
            {
                "K-2", "3-5", "6-8", "9-12"
            };

            var failures = new List<string>();

            foreach (var activity in _allActivities)
            {
                if (!validSubjects.Contains(activity.Subject))
                {
                    failures.Add(
                        $"Activity '{activity.Id}' has invalid Subject '{activity.Subject}'. " +
                        $"Valid values: {string.Join(", ", validSubjects)}");
                }

                if (!validGradeLevels.Contains(activity.GradeLevel))
                {
                    failures.Add(
                        $"Activity '{activity.Id}' has invalid GradeLevel '{activity.GradeLevel}'. " +
                        $"Valid values: {string.Join(", ", validGradeLevels)}");
                }
            }

            Assert.IsEmpty(failures,
                "The following activities have invalid subject or grade level:\n  - " +
                string.Join("\n  - ", failures));
        }

        // ---------------------------------------------------------------------
        // Test: Activity IDs are unique (no duplicates)
        // ---------------------------------------------------------------------
        // WHY: Duplicate IDs cause one activity to silently overwrite another
        //      in the gallery, making content disappear. This happens when
        //      content authors copy-paste activity files and forget to change
        //      the ID.
        // ---------------------------------------------------------------------
        [Test]
        public void AllActivities_HaveUniqueIds()
        {
            var seenIds = new HashSet<string>();
            var duplicates = new List<string>();

            foreach (var activity in _allActivities)
            {
                if (!seenIds.Add(activity.Id))
                {
                    duplicates.Add(activity.Id);
                }
            }

            Assert.IsEmpty(duplicates,
                "Duplicate activity IDs found (each ID must be unique):\n  - " +
                string.Join("\n  - ", duplicates));
        }

        // ---------------------------------------------------------------------
        // Test: Activity thumbnail paths reference files that exist
        // ---------------------------------------------------------------------
        // WHY: Missing thumbnails cause broken images in the gallery UI, which
        //      looks unprofessional and confuses users. This test checks that
        //      every referenced thumbnail actually exists on disk.
        //
        // NOTE: This test accesses the filesystem, so it only works in the
        //       Editor (which is fine for Edit Mode tests). It won't run
        //       in a standalone build.
        // ---------------------------------------------------------------------
        [Test]
        public void AllActivities_HaveThumbnailsThatExist()
        {
            // TODO: Enable this test once you wire it to real data.
            // For the POC, we demonstrate the PATTERN with a simulated check.

            var missingThumbnails = new List<string>();

            foreach (var activity in _allActivities)
            {
                if (string.IsNullOrWhiteSpace(activity.ThumbnailPath))
                {
                    missingThumbnails.Add($"Activity '{activity.Id}' has no thumbnail path set");
                    continue;
                }

                // TODO: Uncomment this block when using real file paths:
                //
                // string fullPath = Path.Combine(Application.dataPath, activity.ThumbnailPath);
                // if (!File.Exists(fullPath))
                // {
                //     missingThumbnails.Add(
                //         $"Activity '{activity.Id}' references missing thumbnail: {activity.ThumbnailPath}");
                // }
            }

            Assert.IsEmpty(missingThumbnails,
                "Activities with missing or invalid thumbnails:\n  - " +
                string.Join("\n  - ", missingThumbnails));
        }

        // =====================================================================
        // SAMPLE DATA -- Replace with actual file loading in your project
        // =====================================================================
        // TODO: Delete this method and load real data in SetUp() instead.
        // =====================================================================

        private List<ActivityMetadataStub> GetSampleActivityMetadata()
        {
            return new List<ActivityMetadataStub>
            {
                new ActivityMetadataStub
                {
                    Id = "bio-cell-structure",
                    Title = "Cell Structure",
                    Description = "Explore the parts of an animal cell in 3D.",
                    Subject = "Biology",
                    GradeLevel = "9-12",
                    ThumbnailPath = "Thumbnails/bio-cell-structure.png"
                },
                new ActivityMetadataStub
                {
                    Id = "chem-periodic-table",
                    Title = "Periodic Table Explorer",
                    Description = "Interact with elements in a 3D periodic table.",
                    Subject = "Chemistry",
                    GradeLevel = "9-12",
                    ThumbnailPath = "Thumbnails/chem-periodic-table.png"
                },
                new ActivityMetadataStub
                {
                    Id = "phys-simple-machines",
                    Title = "Simple Machines",
                    Description = "Build and test levers, pulleys, and inclined planes.",
                    Subject = "Physics",
                    GradeLevel = "6-8",
                    ThumbnailPath = "Thumbnails/phys-simple-machines.png"
                }
            };
        }
    }

    // =========================================================================
    // STUB CLASS -- Replace with your actual ActivityMetadata data class
    // =========================================================================
    // TODO: DELETE this stub and reference your real data class.
    // =========================================================================

    public class ActivityMetadataStub
    {
        public string Id;
        public string Title;
        public string Description;
        public string Subject;
        public string GradeLevel;
        public string ThumbnailPath;
    }
}
