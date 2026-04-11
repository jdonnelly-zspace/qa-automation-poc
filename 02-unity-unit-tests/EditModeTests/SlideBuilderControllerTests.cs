// =============================================================================
// SlideBuilderControllerTests.cs - Edit Mode Unit Tests for SlideBuilderController
// =============================================================================
// TARGET CLASS: SlideBuilderController
//   Real file: Assets/CommonA3/zSpace/Scripts/Controllers/SlideBuilderController.cs
//
// WHAT IT TESTS:
//   Abstract slide-builder controller that manages scenes, slides, questions,
//   and student-answer persistence. Validates scene reordering, slide lookup,
//   question retrieval, student-answer loading, and the Record property toggle.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real SlideBuilderController is an abstract singleton MonoBehaviour.
//      These tests exercise the logic through a concrete test subclass so they
//      compile standalone in the POC without a Unity runtime.
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
    /// Minimal stand-in for the Scene class used by SlideBuilderController.
    /// </summary>
    public class Scene
    {
        public string Name { get; set; }

        public Scene(string name = "")
        {
            Name = name;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for the Slide class returned by GetSlideByScene.
    /// </summary>
    public class Slide
    {
        public Scene Scene { get; set; }
        public int Index { get; set; }

        public Slide(Scene scene, int index)
        {
            Scene = scene;
            Index = index;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for the Question class used by SlideBuilderController.
    /// </summary>
    public class Question
    {
        public string Text { get; set; }

        public Question(string text = "")
        {
            Text = text;
        }

        /// <summary>
        /// Minimal stand-in for the nested StudentAnswer type.
        /// </summary>
        public class StudentAnswer
        {
            public string QuestionText { get; set; }
            public string Answer { get; set; }

            public StudentAnswer(string questionText, string answer)
            {
                QuestionText = questionText;
                Answer = answer;
            }
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Concrete test implementation of the abstract SlideBuilderController.
    /// Mirrors the public API without requiring MonoBehaviour or ZSingleton.
    /// </summary>
    public class SlideBuilderControllerStub
    {
        private List<Scene> _scenes = new List<Scene>();
        private List<Slide> _slides = new List<Slide>();
        private List<Question> _questions = new List<Question>();
        private List<Question.StudentAnswer> _loadedStudentAnswers;
        private bool _isVisible;

        public bool Record { get; set; } = true;

        public bool IsVisible
        {
            get { return _isVisible; }
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
        }

        public void AddScene(Scene scene)
        {
            _scenes.Add(scene);
        }

        public void AddSceneSlide(Scene scene, int slideIndex)
        {
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            _slides.Add(new Slide(scene, slideIndex));
        }

        public List<Scene> ReorganizeSceneOrder()
        {
            // Returns a copy sorted by name to simulate reordering logic
            var sorted = new List<Scene>(_scenes);
            sorted.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return sorted;
        }

        public Slide GetSlideByScene(Scene scene)
        {
            foreach (var slide in _slides)
            {
                if (slide.Scene == scene)
                {
                    return slide;
                }
            }

            return null;
        }

        public List<Question> GetQuestions()
        {
            return new List<Question>(_questions);
        }

        public void AddQuestion(Question question)
        {
            _questions.Add(question);
        }

        public void LoadStudentAnswersFromSaveData(List<Question.StudentAnswer> studentAnswers)
        {
            _loadedStudentAnswers = studentAnswers;
        }

        public List<Question.StudentAnswer> GetLoadedStudentAnswers()
        {
            return _loadedStudentAnswers;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class SlideBuilderControllerTests
    {
        private SlideBuilderControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new SlideBuilderControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void Record_DefaultsToTrue_EnsuresSlideChangesAreTracked()
        {
            // WHY: The Record property gates whether slide changes are persisted.
            //       Defaulting to true ensures no silent data loss on new instances.
            Assert.IsTrue(_controller.Record,
                "Record should default to true so slide changes are tracked from the start.");
        }

        [Test]
        public void Record_SetToFalse_PausesTracking()
        {
            // WHY: Presenters sometimes need to suppress recording while previewing
            //       slides, so toggling Record off must be reliable.

            // Act
            _controller.Record = false;

            // Assert
            Assert.IsFalse(_controller.Record,
                "Record should be false after explicitly disabling tracking.");
        }

        [Test]
        public void AddSceneSlide_CreatesSlide_CanBeRetrievedByScene()
        {
            // WHY: Adding a slide for a scene is the primary authoring action.
            //       If the slide cannot be retrieved by its scene, the presentation
            //       flow is broken.

            // Arrange
            var scene = new Scene("Anatomy Overview");

            // Act
            _controller.AddSceneSlide(scene, 0);

            // Assert
            Slide result = _controller.GetSlideByScene(scene);
            Assert.IsNotNull(result,
                "GetSlideByScene should return the slide that was added for the given scene.");
            Assert.AreEqual(0, result.Index,
                "The returned slide should have the same index that was provided during AddSceneSlide.");
        }

        [Test]
        public void AddSceneSlide_NullScene_ThrowsArgumentNullException()
        {
            // WHY: Guard-clause validation prevents corrupt data from entering
            //       the slide collection, which would cause downstream null refs.

            Assert.Throws<ArgumentNullException>(() => _controller.AddSceneSlide(null, 0),
                "AddSceneSlide should throw ArgumentNullException when scene is null.");
        }

        [Test]
        public void GetSlideByScene_UnknownScene_ReturnsNull()
        {
            // WHY: Callers need a safe way to check whether a scene has an
            //       associated slide without triggering exceptions.

            // Arrange
            var unknownScene = new Scene("Missing Scene");

            // Act
            Slide result = _controller.GetSlideByScene(unknownScene);

            // Assert
            Assert.IsNull(result,
                "GetSlideByScene should return null for a scene that has no associated slide.");
        }

        [Test]
        public void ReorganizeSceneOrder_ReturnsSortedScenes()
        {
            // WHY: Scene reordering drives the presentation flow. If the order
            //       is wrong, students see content in the wrong sequence.

            // Arrange
            _controller.AddScene(new Scene("Zebra"));
            _controller.AddScene(new Scene("Apple"));
            _controller.AddScene(new Scene("Mango"));

            // Act
            List<Scene> ordered = _controller.ReorganizeSceneOrder();

            // Assert
            Assert.AreEqual(3, ordered.Count,
                "Reorganized list should contain all scenes.");
            Assert.AreEqual("Apple", ordered[0].Name,
                "First scene should be 'Apple' after alphabetical reordering.");
            Assert.AreEqual("Mango", ordered[1].Name,
                "Second scene should be 'Mango' after alphabetical reordering.");
            Assert.AreEqual("Zebra", ordered[2].Name,
                "Third scene should be 'Zebra' after alphabetical reordering.");
        }

        [Test]
        public void GetQuestions_ReturnsAllAddedQuestions()
        {
            // WHY: Assessment relies on the question list being complete.
            //       Missing questions would lead to incomplete student evaluations.

            // Arrange
            _controller.AddQuestion(new Question("What is the femur?"));
            _controller.AddQuestion(new Question("Label the heart."));

            // Act
            List<Question> questions = _controller.GetQuestions();

            // Assert
            Assert.AreEqual(2, questions.Count,
                "GetQuestions should return all questions that were added.");
        }

        [Test]
        public void LoadStudentAnswers_StoresAnswersForRetrieval()
        {
            // WHY: When resuming a saved session, student answers must be
            //       faithfully restored so progress is not lost.

            // Arrange
            var answers = new List<Question.StudentAnswer>
            {
                new Question.StudentAnswer("Q1", "Femur"),
                new Question.StudentAnswer("Q2", "Left ventricle")
            };

            // Act
            _controller.LoadStudentAnswersFromSaveData(answers);

            // Assert
            List<Question.StudentAnswer> loaded = _controller.GetLoadedStudentAnswers();
            Assert.IsNotNull(loaded,
                "Loaded student answers should not be null after calling LoadStudentAnswersFromSaveData.");
            Assert.AreEqual(2, loaded.Count,
                "All student answers passed to LoadStudentAnswersFromSaveData should be stored.");
            Assert.AreEqual("Femur", loaded[0].Answer,
                "First student answer content should match what was loaded.");
        }
    }
}
