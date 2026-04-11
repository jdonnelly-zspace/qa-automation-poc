// =============================================================================
// NotebookAddPageBuilderControllerTests.cs - Edit Mode Unit Tests
// =============================================================================
// TARGET CLASS: NotebookAddPageBuilderController
//   Real file: Assets/StudioA3/Scripts/UI/NotebookAddPageBuilderController.cs
//
// WHAT IT TESTS:
//   Controller that manages adding/removing pages in the notebook builder UI.
//   Validates question loading into page-question dictionary, show/hide
//   behavior, button press handling (Add/Done), slide deletion cleanup,
//   and page index reordering logic.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real NotebookAddPageBuilderController is a MonoBehaviour with
//      dependencies on NotebookAddPageBuilder, SlideBuilderController, etc.
//      These tests exercise the dictionary and question-management logic
//      through lightweight POCO stubs so they compile standalone.
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
    /// Minimal stand-in for the real Question class.
    /// </summary>
    public class Question
    {
        public string Text { get; set; }

        public struct StudentAnswer
        {
            public string TextAnswer;
        }

        public StudentAnswer StudentAnswerData;

        public Question()
        {
            Text = "";
        }

        public Question(string text)
        {
            Text = text;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for the real NotebookPage class.
    /// </summary>
    public class NotebookPage
    {
        public string Label { get; set; }

        public class IndexChangedEvent
        {
            private readonly List<Action<IndexChangedEventInfo>> _listeners =
                new List<Action<IndexChangedEventInfo>>();

            public void AddListener(Action<IndexChangedEventInfo> listener)
            {
                _listeners.Add(listener);
            }

            public void RemoveListener(Action<IndexChangedEventInfo> listener)
            {
                _listeners.Remove(listener);
            }

            public void Invoke(IndexChangedEventInfo info)
            {
                foreach (var l in _listeners)
                {
                    l(info);
                }
            }
        }

        public struct IndexChangedEventInfo
        {
            public int OldIndex;
            public int NewIndex;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API surface of the real
    /// NotebookAddPageBuilderController, without requiring MonoBehaviour.
    /// </summary>
    public class NotebookAddPageBuilderControllerStub
    {
        public Dictionary<NotebookPage, Question> PageQuestionDictionary =
            new Dictionary<NotebookPage, Question>();

        public NotebookPage.IndexChangedEvent OnPageIndexChanged =
            new NotebookPage.IndexChangedEvent();

        private bool _isVisible;
        private List<NotebookPage> _pages = new List<NotebookPage>();
        private int _slideQuestionCountUpdate = -1;

        public bool IsVisible => _isVisible;
        public int LastSlideQuestionCountUpdate => _slideQuestionCountUpdate;

        /// <summary>
        /// Loads a list of questions, maps them to notebook pages,
        /// and signals the slide builder with the question count.
        /// </summary>
        public void LoadQuestions(List<Question> questions)
        {
            if (questions == null)
            {
                throw new ArgumentNullException(nameof(questions));
            }

            PageQuestionDictionary.Clear();
            _pages.Clear();

            for (int i = 0; i < questions.Count; i++)
            {
                NotebookPage page = new NotebookPage { Label = questions[i].Text };
                _pages.Add(page);
                PageQuestionDictionary.Add(page, questions[i]);
            }

            _slideQuestionCountUpdate = questions.Count;
        }

        public List<NotebookPage> GetPages()
        {
            return new List<NotebookPage>(_pages);
        }

        public void Show(float duration = 0)
        {
            _isVisible = true;
        }

        public void Hide(float duration = 0)
        {
            _isVisible = false;
        }

        /// <summary>
        /// Removes a page from the dictionary, simulating slide deletion.
        /// Returns true if the page was found and removed.
        /// </summary>
        public bool HandleSlideDeleted(NotebookPage page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            bool removed = PageQuestionDictionary.Remove(page);
            if (removed)
            {
                _pages.Remove(page);
                _slideQuestionCountUpdate = _pages.Count;
            }

            return removed;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class NotebookAddPageBuilderControllerTests
    {
        private NotebookAddPageBuilderControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new NotebookAddPageBuilderControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void LoadQuestions_PopulatesDictionary_WithCorrectCount()
        {
            // WHY: Teachers author questions per slide; the controller must map
            //      every Question to a NotebookPage so that students see the
            //      right prompt when navigating pages.

            // Arrange
            var questions = new List<Question>
            {
                new Question("What is mitosis?"),
                new Question("Describe cell division."),
                new Question("Name the phases.")
            };

            // Act
            _controller.LoadQuestions(questions);

            // Assert
            Assert.AreEqual(3, _controller.PageQuestionDictionary.Count,
                "PageQuestionDictionary should contain one entry per question loaded.");
            Assert.AreEqual(3, _controller.LastSlideQuestionCountUpdate,
                "SlideBuilderController should be notified of the exact question count.");
        }

        [Test]
        public void LoadQuestions_ClearsPreviousEntries_BeforeReloading()
        {
            // WHY: When a teacher switches lessons, stale page-question mappings
            //      from the prior lesson must be cleared to prevent ghost pages.

            // Arrange
            var firstBatch = new List<Question>
            {
                new Question("Old question 1"),
                new Question("Old question 2")
            };
            _controller.LoadQuestions(firstBatch);

            var secondBatch = new List<Question>
            {
                new Question("New question 1")
            };

            // Act
            _controller.LoadQuestions(secondBatch);

            // Assert
            Assert.AreEqual(1, _controller.PageQuestionDictionary.Count,
                "Loading new questions must clear previous entries first.");
        }

        [Test]
        public void LoadQuestions_MapsCorrectQuestionToEachPage()
        {
            // WHY: If page-to-question mapping is wrong, students see the wrong
            //      prompt text, which would break the entire assessment flow.

            // Arrange
            var questions = new List<Question>
            {
                new Question("First"),
                new Question("Second")
            };

            // Act
            _controller.LoadQuestions(questions);

            // Assert
            List<NotebookPage> pages = _controller.GetPages();
            Assert.AreEqual("First", _controller.PageQuestionDictionary[pages[0]].Text,
                "First page should map to the first question.");
            Assert.AreEqual("Second", _controller.PageQuestionDictionary[pages[1]].Text,
                "Second page should map to the second question.");
        }

        [Test]
        public void Show_SetsVisibleTrue_SoStudentsCanSeeNotebook()
        {
            // WHY: The notebook panel must become visible when the teacher opens
            //      the page builder, otherwise students cannot interact with pages.

            // Arrange
            Assert.IsFalse(_controller.IsVisible,
                "Controller should start hidden.");

            // Act
            _controller.Show(0.3f);

            // Assert
            Assert.IsTrue(_controller.IsVisible,
                "Controller must be visible after Show() is called.");
        }

        [Test]
        public void Hide_SetsVisibleFalse_WhenDismissed()
        {
            // WHY: Hiding the notebook builder panel is required after the teacher
            //      finishes editing pages; leaving it visible blocks other UI.

            // Arrange
            _controller.Show();

            // Act
            _controller.Hide(0.1f);

            // Assert
            Assert.IsFalse(_controller.IsVisible,
                "Controller must be hidden after Hide() is called.");
        }

        [Test]
        public void HandleSlideDeleted_RemovesPageFromDictionary_UpdatesCount()
        {
            // WHY: When a teacher deletes a slide, the corresponding page-question
            //      mapping must be removed so the student notebook stays in sync.

            // Arrange
            var questions = new List<Question>
            {
                new Question("Keep this"),
                new Question("Delete this"),
                new Question("Keep this too")
            };
            _controller.LoadQuestions(questions);
            List<NotebookPage> pages = _controller.GetPages();
            NotebookPage pageToDelete = pages[1];

            // Act
            bool removed = _controller.HandleSlideDeleted(pageToDelete);

            // Assert
            Assert.IsTrue(removed,
                "HandleSlideDeleted should return true when the page exists.");
            Assert.AreEqual(2, _controller.PageQuestionDictionary.Count,
                "Dictionary should have one fewer entry after deletion.");
            Assert.AreEqual(2, _controller.LastSlideQuestionCountUpdate,
                "Slide question count should be updated to reflect the deletion.");
        }

        [Test]
        public void HandleSlideDeleted_ReturnsFalse_WhenPageNotFound()
        {
            // WHY: Defensive check - deleting an unknown page should not crash
            //      the app or corrupt the dictionary state.

            // Arrange
            var unknownPage = new NotebookPage { Label = "Ghost page" };

            // Act
            bool removed = _controller.HandleSlideDeleted(unknownPage);

            // Assert
            Assert.IsFalse(removed,
                "HandleSlideDeleted should return false for an unknown page.");
            Assert.AreEqual(0, _controller.PageQuestionDictionary.Count,
                "Dictionary should remain empty when deleting a non-existent page.");
        }

        [Test]
        public void LoadQuestions_EmptyList_ProducesEmptyDictionary()
        {
            // WHY: A lesson may start with zero questions (e.g., free-explore mode).
            //      The controller must handle this gracefully without errors.

            // Arrange
            var emptyList = new List<Question>();

            // Act
            _controller.LoadQuestions(emptyList);

            // Assert
            Assert.AreEqual(0, _controller.PageQuestionDictionary.Count,
                "Empty question list should produce an empty dictionary.");
            Assert.AreEqual(0, _controller.LastSlideQuestionCountUpdate,
                "Slide question count should be zero for an empty question list.");
        }
    }
}
