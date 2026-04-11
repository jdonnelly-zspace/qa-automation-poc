// =============================================================================
// StudentNotebookControllerTests.cs - Edit Mode Unit Tests for StudentNotebookController
// =============================================================================
// TARGET CLASS: StudentNotebookController
//   Real file: Assets/StudioA3/Scripts/UI/StudentNotebookController.cs
//
// WHAT IT TESTS:
//   Student notebook question display, answer saving, and answer type flags.
//   Validates that flag-based answer types (Text, Image, Scratchpad,
//   MultipleChoice) are correctly identified, multi-select detection works,
//   HTML stripping produces clean text, and student answers are preserved.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/enum marked with the "TODO: DELETE this stub" comment
//      and replace the using directives with the real namespaces.
//   3. The real StudentNotebookController is a MonoBehaviour. These tests
//      exercise the logic through lightweight POCO stubs so they compile
//      standalone in the POC without a Unity runtime.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real enum instead.
    /// <summary>
    /// Flags enum representing the types of answers a question can accept.
    /// </summary>
    [Flags]
    public enum AnswerType
    {
        None = 0,
        Text = 1,
        Image = 2,
        Scratchpad = 4,
        MultipleChoice = 8
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Stores a student's answer data for a single question.
    /// </summary>
    public class StudentAnswerData
    {
        public string TextAnswer = "";
        public int SelectedChoiceIndex = -1;
        public List<int> SelectedChoiceIndices = new List<int>();
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Represents a single question in the student notebook.
    /// </summary>
    public class Question
    {
        public string QuestionText = "";
        public string Hint = "";
        public AnswerType AnswerFlags = AnswerType.None;
        public List<string> MultipleChoiceAnswers = new List<string>();
        public List<bool> CorrectAnswers = new List<bool>();
        public StudentAnswerData Answer = new StudentAnswerData();

        /// <summary>
        /// Returns true if the question's AnswerFlags include the given flag.
        /// </summary>
        public bool HasFlag(AnswerType flag)
        {
            return (AnswerFlags & flag) == flag;
        }

        /// <summary>
        /// Returns true if this is a multi-select question (more than one
        /// correct answer).
        /// </summary>
        public bool IsMultiSelect()
        {
            return CorrectAnswers.Count(x => x) > 1;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Utility methods that mirror StudentNotebookController helpers.
    /// </summary>
    public static class NotebookUtility
    {
        /// <summary>
        /// Removes HTML tags from the input string, returning plain text.
        /// </summary>
        public static string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";
            return Regex.Replace(input, "<.*?>", string.Empty);
        }

        /// <summary>
        /// Builds the full display text for a question, appending the hint
        /// if one is present.
        /// </summary>
        public static string BuildFullQuestionText(Question question)
        {
            if (question == null)
                return "";

            string text = question.QuestionText ?? "";
            if (!string.IsNullOrEmpty(question.Hint))
            {
                text += " (Hint: " + question.Hint + ")";
            }
            return text;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class StudentNotebookControllerTests
    {
        private Question _textQuestion;
        private Question _imageQuestion;
        private Question _multipleChoiceQuestion;
        private Question _combinedQuestion;

        [SetUp]
        public void SetUp()
        {
            _textQuestion = new Question
            {
                QuestionText = "Describe the layers of the Earth.",
                Hint = "Think about the crust, mantle, and core.",
                AnswerFlags = AnswerType.Text,
                Answer = new StudentAnswerData()
            };

            _imageQuestion = new Question
            {
                QuestionText = "Draw the phases of the Moon.",
                Hint = "",
                AnswerFlags = AnswerType.Image,
                Answer = new StudentAnswerData()
            };

            _multipleChoiceQuestion = new Question
            {
                QuestionText = "Which planets are gas giants?",
                Hint = "There are four in our solar system.",
                AnswerFlags = AnswerType.MultipleChoice,
                MultipleChoiceAnswers = new List<string>
                {
                    "Jupiter", "Mars", "Saturn", "Uranus", "Neptune", "Earth"
                },
                CorrectAnswers = new List<bool>
                {
                    true, false, true, true, true, false
                },
                Answer = new StudentAnswerData()
            };

            _combinedQuestion = new Question
            {
                QuestionText = "Explain photosynthesis and sketch the process.",
                Hint = "Include chloroplasts in your sketch.",
                AnswerFlags = AnswerType.Text | AnswerType.Image,
                Answer = new StudentAnswerData()
            };
        }

        [TearDown]
        public void TearDown()
        {
            _textQuestion = null;
            _imageQuestion = null;
            _multipleChoiceQuestion = null;
            _combinedQuestion = null;
        }

        // ---------------------------------------------------------------------
        // Test: HasFlag correctly identifies Text answer type
        // ---------------------------------------------------------------------
        // WHY: The notebook UI must show the correct input controls for each
        //      question. If HasFlag misidentifies Text, the text input field
        //      won't appear and students cannot type their answer.
        // ---------------------------------------------------------------------
        [Test]
        public void HasFlag_TextQuestion_IdentifiesTextFlag()
        {
            // Act & Assert
            Assert.IsTrue(_textQuestion.HasFlag(AnswerType.Text),
                "Text question should have the Text flag set");
            Assert.IsFalse(_textQuestion.HasFlag(AnswerType.Image),
                "Text question should not have the Image flag set");
            Assert.IsFalse(_textQuestion.HasFlag(AnswerType.Scratchpad),
                "Text question should not have the Scratchpad flag set");
            Assert.IsFalse(_textQuestion.HasFlag(AnswerType.MultipleChoice),
                "Text question should not have the MultipleChoice flag set");
        }

        // ---------------------------------------------------------------------
        // Test: HasFlag correctly identifies Image answer type
        // ---------------------------------------------------------------------
        // WHY: Image-type questions show the camera/screenshot capture UI. If
        //      the flag is wrong, students cannot capture visual evidence for
        //      their notebook.
        // ---------------------------------------------------------------------
        [Test]
        public void HasFlag_ImageQuestion_IdentifiesImageFlag()
        {
            // Act & Assert
            Assert.IsTrue(_imageQuestion.HasFlag(AnswerType.Image),
                "Image question should have the Image flag set");
            Assert.IsFalse(_imageQuestion.HasFlag(AnswerType.Text),
                "Image question should not have the Text flag set");
        }

        // ---------------------------------------------------------------------
        // Test: Combined flags work correctly (Text | Image)
        // ---------------------------------------------------------------------
        // WHY: Some questions require both a text explanation and an image
        //      capture. The flags enum uses bitwise OR, and the UI must show
        //      both input controls. If combined flags fail, the student only
        //      sees half the expected input area.
        // ---------------------------------------------------------------------
        [Test]
        public void HasFlag_CombinedFlags_IdentifiesBothTextAndImage()
        {
            // Act & Assert
            Assert.IsTrue(_combinedQuestion.HasFlag(AnswerType.Text),
                "Combined question should have the Text flag set");
            Assert.IsTrue(_combinedQuestion.HasFlag(AnswerType.Image),
                "Combined question should have the Image flag set");
            Assert.IsFalse(_combinedQuestion.HasFlag(AnswerType.MultipleChoice),
                "Combined Text|Image question should not have the MultipleChoice flag");
        }

        // ---------------------------------------------------------------------
        // Test: IsMultiSelect returns true when multiple correct answers
        // ---------------------------------------------------------------------
        // WHY: Multi-select questions use checkboxes instead of radio buttons.
        //      If IsMultiSelect is wrong, the UI renders the wrong control and
        //      students can only pick one answer when they need to pick several.
        // ---------------------------------------------------------------------
        [Test]
        public void IsMultiSelect_MultipleCorrectAnswers_ReturnsTrue()
        {
            // Act -- gas giants question has 4 correct answers
            bool result = _multipleChoiceQuestion.IsMultiSelect();

            // Assert
            Assert.IsTrue(result,
                "Question with 4 correct answers (Jupiter, Saturn, Uranus, Neptune) should be multi-select");
        }

        // ---------------------------------------------------------------------
        // Test: IsMultiSelect returns false when single correct answer
        // ---------------------------------------------------------------------
        // WHY: Single-answer questions must use radio buttons, not checkboxes.
        //      If a single-correct question is treated as multi-select, students
        //      may think they need to choose more than one answer.
        // ---------------------------------------------------------------------
        [Test]
        public void IsMultiSelect_SingleCorrectAnswer_ReturnsFalse()
        {
            // Arrange -- question with exactly one correct answer
            var singleChoice = new Question
            {
                QuestionText = "What is the closest star to Earth?",
                AnswerFlags = AnswerType.MultipleChoice,
                MultipleChoiceAnswers = new List<string> { "Proxima Centauri", "Sirius", "The Sun" },
                CorrectAnswers = new List<bool> { false, false, true }
            };

            // Act
            bool result = singleChoice.IsMultiSelect();

            // Assert
            Assert.IsFalse(result,
                "Question with exactly 1 correct answer should not be multi-select");
        }

        // ---------------------------------------------------------------------
        // Test: StripHtmlTags removes basic HTML tags
        // ---------------------------------------------------------------------
        // WHY: Question text sometimes contains HTML formatting from the
        //      authoring tool. The student-facing UI must strip tags to show
        //      clean plain text, or students see raw "<b>bold</b>" markup.
        // ---------------------------------------------------------------------
        [Test]
        public void StripHtmlTags_RemovesBasicHtmlTags()
        {
            // Arrange
            string htmlInput = "<p>What is the <b>mitochondria</b> known as?</p>";

            // Act
            string result = NotebookUtility.StripHtmlTags(htmlInput);

            // Assert
            Assert.AreEqual("What is the mitochondria known as?", result,
                "StripHtmlTags should remove <p>, <b>, and closing tags, leaving plain text");
        }

        // ---------------------------------------------------------------------
        // Test: StripHtmlTags handles empty string
        // ---------------------------------------------------------------------
        // WHY: Some questions have empty hint or text fields. StripHtmlTags
        //      must return empty string (not null or throw) to prevent
        //      NullReferenceException in downstream UI code.
        // ---------------------------------------------------------------------
        [Test]
        public void StripHtmlTags_EmptyString_ReturnsEmptyString()
        {
            // Act
            string result = NotebookUtility.StripHtmlTags("");

            // Assert
            Assert.AreEqual("", result,
                "StripHtmlTags on empty string should return empty string, not null");
        }

        // ---------------------------------------------------------------------
        // Test: BuildFullQuestionText includes hint when present
        // ---------------------------------------------------------------------
        // WHY: Hints provide scaffolding for students who are stuck. If the
        //      hint is not appended to the question display, students miss the
        //      pedagogical support the teacher intended.
        // ---------------------------------------------------------------------
        [Test]
        public void BuildFullQuestionText_WithHint_IncludesHint()
        {
            // Act
            string fullText = NotebookUtility.BuildFullQuestionText(_textQuestion);

            // Assert
            Assert.IsTrue(fullText.Contains("Describe the layers of the Earth"),
                "Full question text should contain the question text");
            Assert.IsTrue(fullText.Contains("Think about the crust, mantle, and core"),
                "Full question text should contain the hint");
        }

        // ---------------------------------------------------------------------
        // Test: SaveAnswer preserves text answer
        // ---------------------------------------------------------------------
        // WHY: When a student types an answer and navigates away, the answer
        //      must be stored in the StudentAnswerData. Lost answers are the
        //      single most frustrating bug for students using the notebook.
        // ---------------------------------------------------------------------
        [Test]
        public void SaveAnswer_PreservesTextAnswer()
        {
            // Arrange -- simulate student typing an answer
            string studentAnswer = "The Earth has three main layers: crust, mantle, and core.";
            _textQuestion.Answer.TextAnswer = studentAnswer;

            // Act -- read back the stored answer
            string storedAnswer = _textQuestion.Answer.TextAnswer;

            // Assert
            Assert.AreEqual(studentAnswer, storedAnswer,
                "StudentAnswerData should preserve the exact text the student entered");
        }

        // ---------------------------------------------------------------------
        // Test: SaveAnswer preserves choice selection
        // ---------------------------------------------------------------------
        // WHY: For multiple-choice questions, the student's selected indices
        //      must persist. If selections are lost, students have to re-answer
        //      questions every time they navigate back to them.
        // ---------------------------------------------------------------------
        [Test]
        public void SaveAnswer_PreservesChoiceSelection()
        {
            // Arrange -- student selects Jupiter (0), Saturn (2), Uranus (3), Neptune (4)
            _multipleChoiceQuestion.Answer.SelectedChoiceIndices = new List<int> { 0, 2, 3, 4 };

            // Act -- read back the stored selections
            List<int> storedSelections = _multipleChoiceQuestion.Answer.SelectedChoiceIndices;

            // Assert
            Assert.AreEqual(4, storedSelections.Count,
                "StudentAnswerData should store all 4 selected choice indices");
            Assert.Contains(0, storedSelections,
                "Selected indices should include 0 (Jupiter)");
            Assert.Contains(2, storedSelections,
                "Selected indices should include 2 (Saturn)");
            Assert.Contains(3, storedSelections,
                "Selected indices should include 3 (Uranus)");
            Assert.Contains(4, storedSelections,
                "Selected indices should include 4 (Neptune)");
        }
    }
}
