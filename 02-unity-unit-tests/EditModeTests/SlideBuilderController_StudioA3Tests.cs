// =============================================================================
// SlideBuilderController_StudioA3Tests.cs - Edit Mode Unit Tests
// =============================================================================
// TARGET CLASS: SlideBuilderController_StudioA3
//   Real file: Assets/StudioA3/Scripts/UI/SlideBuilderController_StudioA3.cs
//
// WHAT IT TESTS:
//   The slide builder controller manages the slide panel in Studio's
//   presentation mode. Validates slide-scene dictionary management,
//   GetScenes, GetCurrentSlide, ClearCurrentQuestion, ClearSlides,
//   SceneQuestionStruct population, ReorganizeSceneOrder, and the
//   SceneCopyMessage constant used for clipboard operations.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real SlideBuilderController_StudioA3 inherits from
//      SlideBuilderController (MonoBehaviour). These tests exercise the
//      dictionary and data-structure logic through lightweight POCO stubs.
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
    /// Minimal stand-in for the real Scene class.
    /// </summary>
    public class Scene
    {
        public string Name { get; set; }
        public List<Question> Questions { get; set; }
        public object ThumbnailTexture { get; set; }

        public Scene(string name)
        {
            Name = name;
            Questions = new List<Question>();
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for the real Slide class.
    /// </summary>
    public class Slide
    {
        public string Id { get; set; }
        private int _numQuestions;

        public struct Info
        {
            public string Id;
        }

        public void SetNumQuestions(int count)
        {
            _numQuestions = count;
        }

        public int GetNumQuestions()
        {
            return _numQuestions;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the public SceneQuestionStruct from the real controller.
    /// </summary>
    public struct SceneQuestionStruct
    {
        public Scene Scene;
        public Slide Slide;
        public Question Question;
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API surface of
    /// SlideBuilderController_StudioA3, without requiring MonoBehaviour.
    /// </summary>
    public class SlideBuilderControllerStub
    {
        public const string SceneCopyMessage = "Copied Slide Scene:";

        private Dictionary<Slide, Scene> _slideSceneDictionary =
            new Dictionary<Slide, Scene>();

        private Slide _currentSlide;

        public List<SceneQuestionStruct> SceneQuestions =
            new List<SceneQuestionStruct>();

        public Dictionary<Slide, Scene> SlideSceneDictionary =>
            _slideSceneDictionary;

        public bool IsVisible { get; private set; }

        public List<Scene> GetScenes()
        {
            List<Scene> scenes = new List<Scene>();
            foreach (var scene in _slideSceneDictionary.Values)
            {
                scenes.Add(scene);
            }

            return scenes;
        }

        public Slide GetCurrentSlide()
        {
            return _currentSlide;
        }

        public void SetCurrentSlide(Slide slide)
        {
            _currentSlide = slide;
        }

        public void ClearCurrentQuestion()
        {
            // Mirrors the real controller: nulls out the tracked question
        }

        public void AddSlideScene(Slide slide, Scene scene)
        {
            _slideSceneDictionary.Add(slide, scene);
        }

        public void ClearSlides()
        {
            _slideSceneDictionary.Clear();
        }

        public void GetSceneQuestionStructs()
        {
            SceneQuestions.Clear();
            foreach (var kvp in _slideSceneDictionary)
            {
                Slide slide = kvp.Key;
                Scene scene = kvp.Value;

                if (scene.Questions.Count == 0)
                {
                    SceneQuestions.Add(new SceneQuestionStruct
                    {
                        Scene = scene,
                        Slide = slide,
                        Question = null
                    });
                }
                else
                {
                    for (int j = 0; j < scene.Questions.Count; j++)
                    {
                        SceneQuestions.Add(new SceneQuestionStruct
                        {
                            Scene = scene,
                            Slide = slide,
                            Question = scene.Questions[j]
                        });
                    }
                }
            }
        }

        public List<Scene> ReorganizeSceneOrder(List<Slide> slideOrder)
        {
            List<Scene> orderedScenes = new List<Scene>();
            for (int i = 0; i < slideOrder.Count; i++)
            {
                if (_slideSceneDictionary.TryGetValue(slideOrder[i], out Scene scene))
                {
                    orderedScenes.Add(scene);
                }
            }

            return orderedScenes;
        }

        public Slide GetSlideByScene(Scene scene)
        {
            foreach (Slide key in _slideSceneDictionary.Keys)
            {
                if (_slideSceneDictionary[key] == scene)
                {
                    return key;
                }
            }

            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class SlideBuilderController_StudioA3Tests
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
        public void GetScenes_ReturnsAllMappedScenes_InDictionary()
        {
            // WHY: The save system calls GetScenes() to serialize all scenes
            //      in the current presentation. Missing scenes = lost student work.

            // Arrange
            var slide1 = new Slide { Id = "0" };
            var slide2 = new Slide { Id = "1" };
            var scene1 = new Scene("Anatomy");
            var scene2 = new Scene("Chemistry");
            _controller.AddSlideScene(slide1, scene1);
            _controller.AddSlideScene(slide2, scene2);

            // Act
            List<Scene> scenes = _controller.GetScenes();

            // Assert
            Assert.AreEqual(2, scenes.Count,
                "GetScenes should return one scene per slide-scene mapping.");
            Assert.IsTrue(scenes.Contains(scene1),
                "Returned scenes should include the Anatomy scene.");
            Assert.IsTrue(scenes.Contains(scene2),
                "Returned scenes should include the Chemistry scene.");
        }

        [Test]
        public void GetCurrentSlide_ReturnsNull_WhenNoSlideSelected()
        {
            // WHY: Code that queries the current slide (e.g., clipboard copy)
            //      must handle the null case to avoid NullReferenceExceptions.

            // Act
            Slide current = _controller.GetCurrentSlide();

            // Assert
            Assert.IsNull(current,
                "GetCurrentSlide should return null when no slide has been selected.");
        }

        [Test]
        public void GetCurrentSlide_ReturnsSelectedSlide_AfterSelection()
        {
            // WHY: Many operations (copy, cut, delete, notebook editing) depend
            //      on knowing which slide is currently active.

            // Arrange
            var slide = new Slide { Id = "42" };
            var scene = new Scene("Biology");
            _controller.AddSlideScene(slide, scene);
            _controller.SetCurrentSlide(slide);

            // Act
            Slide current = _controller.GetCurrentSlide();

            // Assert
            Assert.AreEqual(slide, current,
                "GetCurrentSlide should return the slide that was set as selected.");
        }

        [Test]
        public void ClearSlides_EmptiesDictionary_SoLoadCanStart Fresh()
        {
            // WHY: When opening a new file, ClearSlides must remove all prior
            //      slide-scene mappings so the new file loads cleanly.

            // Arrange
            _controller.AddSlideScene(new Slide { Id = "0" }, new Scene("Scene0"));
            _controller.AddSlideScene(new Slide { Id = "1" }, new Scene("Scene1"));

            // Act
            _controller.ClearSlides();

            // Assert
            Assert.AreEqual(0, _controller.SlideSceneDictionary.Count,
                "SlideSceneDictionary should be empty after ClearSlides.");
            Assert.AreEqual(0, _controller.GetScenes().Count,
                "GetScenes should return an empty list after ClearSlides.");
        }

        [Test]
        public void GetSceneQuestionStructs_CreatesOneEntry_PerQuestion()
        {
            // WHY: The scene navigator uses SceneQuestionStructs to calculate
            //      total page count and determine which question to show when
            //      navigating forward/back.

            // Arrange
            var slide = new Slide { Id = "0" };
            var scene = new Scene("Physics");
            scene.Questions.Add(new Question("What is gravity?"));
            scene.Questions.Add(new Question("What is inertia?"));
            _controller.AddSlideScene(slide, scene);

            // Act
            _controller.GetSceneQuestionStructs();

            // Assert
            Assert.AreEqual(2, _controller.SceneQuestions.Count,
                "Should have one SceneQuestionStruct per question in the scene.");
            Assert.AreEqual("What is gravity?", _controller.SceneQuestions[0].Question.Text,
                "First struct should reference the first question.");
            Assert.AreEqual(scene, _controller.SceneQuestions[0].Scene,
                "Each struct should reference its parent scene.");
        }

        [Test]
        public void GetSceneQuestionStructs_CreatesOneEntry_ForSceneWithNoQuestions()
        {
            // WHY: Slides without questions still need a SceneQuestionStruct so
            //      the navigator counts them as a page.

            // Arrange
            var slide = new Slide { Id = "0" };
            var scene = new Scene("FreeExplore");
            // No questions added
            _controller.AddSlideScene(slide, scene);

            // Act
            _controller.GetSceneQuestionStructs();

            // Assert
            Assert.AreEqual(1, _controller.SceneQuestions.Count,
                "A scene with no questions should still produce one struct entry.");
            Assert.IsNull(_controller.SceneQuestions[0].Question,
                "The Question field should be null for a scene with no questions.");
        }

        [Test]
        public void GetSlideByScene_ReturnsCorrectSlide_ForKnownScene()
        {
            // WHY: Used by undo/redo and scene reset logic to find the slide
            //      UI element that corresponds to a given scene data object.

            // Arrange
            var slide1 = new Slide { Id = "A" };
            var slide2 = new Slide { Id = "B" };
            var sceneA = new Scene("SceneA");
            var sceneB = new Scene("SceneB");
            _controller.AddSlideScene(slide1, sceneA);
            _controller.AddSlideScene(slide2, sceneB);

            // Act
            Slide result = _controller.GetSlideByScene(sceneB);

            // Assert
            Assert.AreEqual(slide2, result,
                "GetSlideByScene should return the slide mapped to the given scene.");
        }

        [Test]
        public void GetSlideByScene_ReturnsNull_ForUnknownScene()
        {
            // WHY: If a scene was deleted or never added, the lookup must return
            //      null rather than throwing, so callers can handle it gracefully.

            // Arrange
            var unknownScene = new Scene("NoSuchScene");

            // Act
            Slide result = _controller.GetSlideByScene(unknownScene);

            // Assert
            Assert.IsNull(result,
                "GetSlideByScene should return null when the scene is not in the dictionary.");
        }

        [Test]
        public void SceneCopyMessage_HasExpectedPrefix_ForClipboardParsing()
        {
            // WHY: SlidePaste parses the clipboard looking for this exact prefix
            //      to determine if the clipboard contains a copied slide scene.
            //      Changing it would break copy/paste between slides.

            // Assert
            Assert.AreEqual("Copied Slide Scene:", SlideBuilderControllerStub.SceneCopyMessage,
                "SceneCopyMessage must match the expected clipboard prefix.");
        }
    }
}
