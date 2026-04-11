// =============================================================================
// TutorialControllerTests.cs - Edit Mode Unit Tests for TutorialController
// =============================================================================
// TARGET CLASS: TutorialController
//   Real file: Assets/StudioA3/Scripts/UI/TutorialController.cs
//
// WHAT IT TESTS:
//   First-run tutorial system in Studio. Validates visibility toggling,
//   tutorial group selection (Models, Activities, Content), page navigation
//   (next/back), page number formatting, first-run flag behavior, and
//   close/dismiss logic.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real TutorialController is a MonoBehaviour that depends on
//      TutorialWindow, TutorialHub, CameraNavigator, etc. These tests
//      exercise the page-navigation state machine through a lightweight
//      POCO stub so they compile standalone.
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

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for TutorialFrameInfo ScriptableObject data.
    /// </summary>
    public class TutorialFrameInfo
    {
        public string TitleTextKey { get; set; }
        public string BodyTextKey { get; set; }
        public bool UseToolTip { get; set; }
        public bool UseHighlight { get; set; }
        public string ToolTipTextKey { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the tutorial navigation state machine
    /// from the real TutorialController, without requiring MonoBehaviour.
    /// </summary>
    public class TutorialControllerStub
    {
        public string ModelTutorialHeaderString { get; set; }
        public string ActivitiesTutorialHeaderString { get; set; }
        public string ContentTutorialHeaderString { get; set; }

        public bool FirstRun { get; set; }

        private bool _isVisible;
        private int _currentPageNumber;
        private int _totalPages;
        private string _currentTutorialGroupTitle = "";
        private List<TutorialFrameInfo> _currentTutorialFrameInfos;

        private List<TutorialFrameInfo> _modelTutorialFrameInfos =
            new List<TutorialFrameInfo>();
        private List<TutorialFrameInfo> _activitiesTutorialFrameInfos =
            new List<TutorialFrameInfo>();
        private List<TutorialFrameInfo> _contentTutorialFrameInfos =
            new List<TutorialFrameInfo>();

        public bool IsVisible => _isVisible;
        public int CurrentPageNumber => _currentPageNumber;
        public int TotalPages => _totalPages;
        public string CurrentGroupTitle => _currentTutorialGroupTitle;

        public void SetModelFrames(List<TutorialFrameInfo> frames)
        {
            _modelTutorialFrameInfos = frames;
        }

        public void SetActivitiesFrames(List<TutorialFrameInfo> frames)
        {
            _activitiesTutorialFrameInfos = frames;
        }

        public void SetContentFrames(List<TutorialFrameInfo> frames)
        {
            _contentTutorialFrameInfos = frames;
        }

        public void ShowHub()
        {
            _isVisible = true;
            FirstRun = true;
        }

        public void SelectTutorialGroup(string groupButton)
        {
            switch (groupButton)
            {
                case "ModelsTutorialButton":
                    _currentTutorialGroupTitle = ModelTutorialHeaderString;
                    _currentTutorialFrameInfos = _modelTutorialFrameInfos;
                    break;
                case "ActivitiesTutorialButton":
                    _currentTutorialGroupTitle = ActivitiesTutorialHeaderString;
                    _currentTutorialFrameInfos = _activitiesTutorialFrameInfos;
                    break;
                case "ContentTutorialButton":
                    _currentTutorialGroupTitle = ContentTutorialHeaderString;
                    _currentTutorialFrameInfos = _contentTutorialFrameInfos;
                    break;
            }

            _totalPages = _currentTutorialFrameInfos.Count;
            _currentPageNumber = 0;
        }

        public string GetPageNumberLabel()
        {
            return string.Format("{0}/{1}", _currentPageNumber + 1, _totalPages);
        }

        /// <summary>
        /// Advances to the next page. Returns true if advanced, false if
        /// already at the last page (tutorial closes).
        /// </summary>
        public bool NextPage()
        {
            if (_currentPageNumber + 1 == _totalPages)
            {
                Close();
                return false;
            }

            _currentPageNumber++;
            return true;
        }

        /// <summary>
        /// Goes back one page. Returns true if moved back, false if
        /// already at the first page (tutorial closes).
        /// </summary>
        public bool PreviousPage()
        {
            if (_currentPageNumber == 0)
            {
                Close();
                return false;
            }

            _currentPageNumber--;
            return true;
        }

        public void Close()
        {
            _currentPageNumber = 0;
            FirstRun = false;
            _isVisible = false;
        }

        public void HandleHelpButtonPressed()
        {
            if (!_isVisible)
            {
                _isVisible = true;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class TutorialControllerTests
    {
        private TutorialControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new TutorialControllerStub();
            _controller.ModelTutorialHeaderString = "Models";
            _controller.ActivitiesTutorialHeaderString = "Activities";
            _controller.ContentTutorialHeaderString = "Content";

            // Set up sample tutorial frames
            _controller.SetModelFrames(new List<TutorialFrameInfo>
            {
                new TutorialFrameInfo { TitleTextKey = "model-page-1" },
                new TutorialFrameInfo { TitleTextKey = "model-page-2" },
                new TutorialFrameInfo { TitleTextKey = "model-page-3" }
            });

            _controller.SetActivitiesFrames(new List<TutorialFrameInfo>
            {
                new TutorialFrameInfo { TitleTextKey = "activity-page-1" },
                new TutorialFrameInfo { TitleTextKey = "activity-page-2" }
            });

            _controller.SetContentFrames(new List<TutorialFrameInfo>
            {
                new TutorialFrameInfo { TitleTextKey = "content-page-1" }
            });
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void SelectTutorialGroup_SetsCorrectTitle_ForModelsGroup()
        {
            // WHY: When a user clicks "Models" in the tutorial hub, the tutorial
            //      window header must show the Models title so they know which
            //      section they are viewing.

            // Act
            _controller.SelectTutorialGroup("ModelsTutorialButton");

            // Assert
            Assert.AreEqual("Models", _controller.CurrentGroupTitle,
                "Selecting the Models group should set the group title to the model header string.");
            Assert.AreEqual(3, _controller.TotalPages,
                "Total pages should match the number of model tutorial frames.");
        }

        [Test]
        public void SelectTutorialGroup_ResetsPageToZero_OnNewGroupSelection()
        {
            // WHY: If a user was on page 3 of Models, then switches to Activities,
            //      the page counter must reset to 0 so they start from the beginning.

            // Arrange
            _controller.SelectTutorialGroup("ModelsTutorialButton");
            _controller.NextPage();
            _controller.NextPage(); // now on page 2

            // Act
            _controller.SelectTutorialGroup("ActivitiesTutorialButton");

            // Assert
            Assert.AreEqual(0, _controller.CurrentPageNumber,
                "Selecting a new tutorial group must reset the page number to 0.");
            Assert.AreEqual("Activities", _controller.CurrentGroupTitle,
                "Group title should update to the Activities header.");
        }

        [Test]
        public void NextPage_AdvancesPageNumber_WhenNotOnLastPage()
        {
            // WHY: Users must be able to step through tutorial pages sequentially.
            //      A broken next button would trap them on one page.

            // Arrange
            _controller.SelectTutorialGroup("ModelsTutorialButton"); // 3 pages

            // Act
            bool advanced = _controller.NextPage();

            // Assert
            Assert.IsTrue(advanced,
                "NextPage should return true when there are more pages.");
            Assert.AreEqual(1, _controller.CurrentPageNumber,
                "Page number should increment from 0 to 1.");
        }

        [Test]
        public void NextPage_CloseTutorial_WhenOnLastPage()
        {
            // WHY: Pressing Next on the final page should dismiss the tutorial
            //      entirely, matching the real controller's behavior of hiding
            //      the window and clearing firstRun.

            // Arrange
            _controller.SelectTutorialGroup("ModelsTutorialButton"); // 3 pages
            _controller.NextPage(); // page 1
            _controller.NextPage(); // page 2 (last page)

            // Act
            bool advanced = _controller.NextPage(); // should close

            // Assert
            Assert.IsFalse(advanced,
                "NextPage should return false when already on the last page.");
            Assert.IsFalse(_controller.IsVisible,
                "Tutorial should be hidden after advancing past the last page.");
            Assert.IsFalse(_controller.FirstRun,
                "FirstRun should be set to false after completing the tutorial.");
        }

        [Test]
        public void PreviousPage_DecrementsPage_WhenNotOnFirstPage()
        {
            // WHY: Users need to go back to re-read previous tutorial content.

            // Arrange
            _controller.SelectTutorialGroup("ModelsTutorialButton");
            _controller.NextPage(); // page 1
            _controller.NextPage(); // page 2

            // Act
            bool movedBack = _controller.PreviousPage();

            // Assert
            Assert.IsTrue(movedBack,
                "PreviousPage should return true when not on the first page.");
            Assert.AreEqual(1, _controller.CurrentPageNumber,
                "Page number should decrement from 2 to 1.");
        }

        [Test]
        public void PreviousPage_ClosesTutorial_WhenOnFirstPage()
        {
            // WHY: The real controller closes the tutorial when Back is pressed
            //      on page 0, effectively dismissing the tutorial window.

            // Arrange
            _controller.SelectTutorialGroup("ModelsTutorialButton");

            // Act
            bool movedBack = _controller.PreviousPage();

            // Assert
            Assert.IsFalse(movedBack,
                "PreviousPage should return false when already on the first page.");
            Assert.IsFalse(_controller.IsVisible,
                "Tutorial should be hidden after pressing Back on the first page.");
        }

        [Test]
        public void GetPageNumberLabel_FormatsCorrectly_AsOneIndexed()
        {
            // WHY: The page label displays "1/3" style text to users. It must
            //      be 1-indexed (not 0-indexed) for a natural reading experience.

            // Arrange
            _controller.SelectTutorialGroup("ModelsTutorialButton"); // 3 pages
            _controller.NextPage(); // page 1 (displays as "2/3")

            // Act
            string label = _controller.GetPageNumberLabel();

            // Assert
            Assert.AreEqual("2/3", label,
                "Page label should display 1-indexed page number in 'current/total' format.");
        }

        [Test]
        public void HandleHelpButtonPressed_ShowsTutorial_WhenNotAlreadyVisible()
        {
            // WHY: The help button in the toolbar must re-open the tutorial hub
            //      so returning users can review the help content at any time.

            // Arrange
            Assert.IsFalse(_controller.IsVisible,
                "Tutorial should start hidden.");

            // Act
            _controller.HandleHelpButtonPressed();

            // Assert
            Assert.IsTrue(_controller.IsVisible,
                "Tutorial should become visible after pressing the help button.");
        }
    }
}
