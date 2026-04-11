// =============================================================================
// ActivityGalleryModelControllerTests.cs - Edit Mode Unit Tests
// =============================================================================
// TARGET CLASS: ActivityGalleryModelController
//   Real file: Assets/VivedUpgrades/ActivityGallery/Scripts/ActivityGalleryModelController.cs
//
// WHAT IT TESTS:
//   Activity gallery UI controller. Validates Show/Hide visibility toggling,
//   event firing, LoadActivity null-guard, TryLoadLiteActivity approval
//   checking, IsVisible property tracking, and empty-filename rejection.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real ActivityGalleryModelController is a ZSingleton<T>. These
//      tests exercise logic through lightweight POCO stubs so they compile
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
    /// <summary>Minimal stand-in for ActivityInfo metadata.</summary>
    public class ActivityInfoStub
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SavePath { get; set; }
        public string QuickLaunchCode { get; set; }
        public string Description { get; set; }
        public string ThumbnailPath { get; set; }
        public bool IsAvailable { get; set; }
        public List<SubjectTopicPairStub> SubjectTopicPairs { get; set; } = new List<SubjectTopicPairStub>();
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    public class SubjectTopicPairStub
    {
        public string Topic { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Minimal stand-in for ActivityPackManager approval checking.</summary>
    public class ActivityPackManagerStub
    {
        public bool IsActivityApproved { get; set; } = true;

        public bool IsActivityIncludedInApprovedList(string activityId)
        {
            return IsActivityApproved;
        }

        public ActivityInfoStub GetActivityByGuid(string guid)
        {
            return null;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>Minimal stand-in for the gallery view.</summary>
    public class ActivityGalleryModelStub
    {
        public bool IsShown { get; private set; }

        public void Show(float duration) { IsShown = true; }
        public void Hide(float duration) { IsShown = false; }
    }

    // TODO: DELETE this stub when integrating into the Unity project -- use the real class instead.
    /// <summary>
    /// Lightweight POCO mirroring the public API of ActivityGalleryModelController
    /// without requiring MonoBehaviour or ZSingleton.
    /// </summary>
    public class ActivityGalleryModelControllerStub
    {
        private readonly ActivityGalleryModelStub _activityGallery;
        private readonly ActivityPackManagerStub _activityPackManager;

        public bool IsVisible { get; private set; }
        public int OnShowFiredCount { get; private set; }
        public int OnHideFiredCount { get; private set; }
        public string LastLoadedFilename { get; private set; }
        public bool LoadAttempted { get; private set; }

        public event Action OnShow;
        public event Action OnHide;

        public ActivityGalleryModelControllerStub(
            ActivityGalleryModelStub activityGallery,
            ActivityPackManagerStub activityPackManager)
        {
            _activityGallery = activityGallery;
            _activityPackManager = activityPackManager;
            OnShow += () => OnShowFiredCount++;
            OnHide += () => OnHideFiredCount++;
        }

        public void Show()
        {
            _activityGallery.Show(0.3f);
            IsVisible = true;
            OnShow?.Invoke();
        }

        public void Hide()
        {
            _activityGallery.Hide(0.3f);
            IsVisible = false;
            OnHide?.Invoke();
        }

        public void LoadActivity(string filename, string activityName, Action<int> callback, bool skipDialogs = false)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return;
            }

            LoadAttempted = true;
            LastLoadedFilename = filename;
        }

        public bool TryLoadLiteActivity(string fileName)
        {
            // Simulate extraction of activity ID from file name
            string activityId = fileName;
            return _activityPackManager.IsActivityIncludedInApprovedList(activityId);
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ActivityGalleryModelControllerTests
    {
        private ActivityGalleryModelControllerStub _controller;
        private ActivityGalleryModelStub _galleryModel;
        private ActivityPackManagerStub _activityPackManager;

        [SetUp]
        public void SetUp()
        {
            _galleryModel = new ActivityGalleryModelStub();
            _activityPackManager = new ActivityPackManagerStub();
            _controller = new ActivityGalleryModelControllerStub(
                _galleryModel,
                _activityPackManager);
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
            _galleryModel = null;
            _activityPackManager = null;
        }

        // WHY: The activity gallery must be visible for students to browse
        // activities. Show() must update the view and flag IsVisible.
        [Test]
        public void Show_SetsIsVisibleTrue_AndShowsGalleryView()
        {
            // Arrange - gallery starts hidden
            Assert.IsFalse(_controller.IsVisible,
                "Controller should start with IsVisible=false.");

            // Act
            _controller.Show();

            // Assert
            Assert.IsTrue(_controller.IsVisible,
                "IsVisible must be true after Show() so other systems know the gallery is open.");
            Assert.IsTrue(_galleryModel.IsShown,
                "The gallery view model must be shown to render the UI.");
        }

        // WHY: Hide() must dismiss the gallery and update IsVisible so
        // scene interaction can resume.
        [Test]
        public void Hide_SetsIsVisibleFalse_AndHidesGalleryView()
        {
            // Arrange
            _controller.Show();

            // Act
            _controller.Hide();

            // Assert
            Assert.IsFalse(_controller.IsVisible,
                "IsVisible must be false after Hide() so scene interaction is unblocked.");
            Assert.IsFalse(_galleryModel.IsShown,
                "The gallery view model must be hidden.");
        }

        // WHY: Other systems subscribe to OnShow/OnHide to coordinate UI
        // state (e.g., blocking scene interaction). Events must fire reliably.
        [Test]
        public void Show_FiresOnShowEvent_ExactlyOnce()
        {
            // Act
            _controller.Show();

            // Assert
            Assert.AreEqual(1, _controller.OnShowFiredCount,
                "OnShow event must fire exactly once per Show() call for correct subscriber notification.");
        }

        // WHY: OnHide must fire on Hide() so subscribers can restore scene state.
        [Test]
        public void Hide_FiresOnHideEvent_ExactlyOnce()
        {
            // Arrange
            _controller.Show();

            // Act
            _controller.Hide();

            // Assert
            Assert.AreEqual(1, _controller.OnHideFiredCount,
                "OnHide event must fire exactly once per Hide() call.");
        }

        // WHY: LoadActivity receives filenames from external sources (gallery
        // tiles, command line). Null/empty filenames must be rejected early
        // to prevent downstream errors.
        [Test]
        public void LoadActivity_DoesNothing_WhenFilenameIsNullOrEmpty()
        {
            // Act
            _controller.LoadActivity(null, "Test Activity", null);
            _controller.LoadActivity("", "Test Activity", null);

            // Assert
            Assert.IsFalse(_controller.LoadAttempted,
                "LoadActivity must return immediately for null or empty filename to prevent file system errors.");
        }

        // WHY: Valid filenames must proceed to the load pipeline so the
        // user can open their selected activity.
        [Test]
        public void LoadActivity_ProceedsWithLoad_WhenFilenameIsValid()
        {
            // Act
            _controller.LoadActivity("activities/science-101.sa3", "Science 101", null);

            // Assert
            Assert.IsTrue(_controller.LoadAttempted,
                "LoadActivity should proceed when a valid filename is provided.");
            Assert.AreEqual("activities/science-101.sa3", _controller.LastLoadedFilename,
                "The correct filename should be forwarded to the load pipeline.");
        }

        // WHY: Lite-licensed users can only open approved activities. If
        // the activity is not on the approved list, loading must be blocked.
        [Test]
        public void TryLoadLiteActivity_ReturnsFalse_WhenActivityNotApproved()
        {
            // Arrange
            _activityPackManager.IsActivityApproved = false;

            // Act
            bool result = _controller.TryLoadLiteActivity("unapproved-activity-id");

            // Assert
            Assert.IsFalse(result,
                "TryLoadLiteActivity must return false for non-approved activities to enforce Lite license restrictions.");
        }

        // WHY: Approved activities must pass through for Lite users so they
        // can access their entitled content.
        [Test]
        public void TryLoadLiteActivity_ReturnsTrue_WhenActivityIsApproved()
        {
            // Arrange
            _activityPackManager.IsActivityApproved = true;

            // Act
            bool result = _controller.TryLoadLiteActivity("approved-activity-id");

            // Assert
            Assert.IsTrue(result,
                "TryLoadLiteActivity must return true for approved activities so Lite users can load them.");
        }
    }
}
