// =============================================================================
// NotebookBuilderControllerTests.cs - Edit Mode Unit Tests for NotebookBuilderController
// =============================================================================
// TARGET CLASS: NotebookBuilderController
//   Real file: Assets/StudioA3/Scripts/UI/NotebookBuilderController.cs
//
// WHAT IT TESTS:
//   Notebook builder question creation, answer type toggling, and question
//   validation. Validates that AnswerType flags are correctly built from
//   boolean toggles, questions are validated for non-empty text, reset clears
//   all state, and multiple-choice answer lists are properly sized.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment and
//      replace the using directives with the real namespaces.
//   3. The real NotebookBuilderController is a MonoBehaviour. These tests
//      exercise the logic through a lightweight POCO stub so they compile
//      standalone in the POC without a Unity runtime.
//   4. The AnswerType flags enum and Question class are shared with
//      StudentNotebookControllerTests. In the real project they come from a
//      single shared source; in this POC each test file is self-contained.
//   5. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // NOTE: AnswerType, Question, and StudentAnswerData are defined in
    // StudentNotebookControllerTests.cs within this same namespace. If that file
    // is not present, uncomment the stubs below.

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Represents a question being authored in the notebook builder.
    /// </summary>
    public class BuilderQuestion
    {
        public string QuestionText = "";
        public string Hint = "";
        public bool HasText = false;
        public bool HasImage = false;
        public bool HasScratchpad = false;
        public bool HasMultipleChoice = false;
        public List<string> MultipleChoiceAnswers = new List<string>();
        public List<bool> CorrectAnswers = new List<bool>();
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API of the real
    /// NotebookBuilderController, without requiring MonoBehaviour.
    /// </summary>
    public class NotebookBuilderControllerStub
    {
        public BuilderQuestion CurrentQuestion = new BuilderQuestion();

        /// <summary>
        /// Builds an AnswerType flags value from individual boolean toggles.
        /// </summary>
        public AnswerType BuildAnswerType(bool hasText, bool hasImage, bool hasScratchpad, bool hasMultipleChoice)
        {
            AnswerType flags = AnswerType.None;

            if (hasText)
                flags |= AnswerType.Text;
            if (hasImage)
                flags |= AnswerType.Image;
            if (hasScratchpad)
                flags |= AnswerType.Scratchpad;
            if (hasMultipleChoice)
                flags |= AnswerType.MultipleChoice;

            return flags;
        }

        /// <summary>
        /// Saves the current question by building its AnswerType from the
        /// toggle states.
        /// </summary>
        public Question SaveQuestion()
        {
            var question = new Question
            {
                QuestionText = CurrentQuestion.QuestionText,
                Hint = CurrentQuestion.Hint,
                AnswerFlags = BuildAnswerType(
                    CurrentQuestion.HasText,
                    CurrentQuestion.HasImage,
                    CurrentQuestion.HasScratchpad,
                    CurrentQuestion.HasMultipleChoice),
                MultipleChoiceAnswers = new List<string>(CurrentQuestion.MultipleChoiceAnswers),
                CorrectAnswers = new List<bool>(CurrentQuestion.CorrectAnswers),
                Answer = new StudentAnswerData()
            };
            return question;
        }

        /// <summary>
        /// Resets the current question to a blank state.
        /// </summary>
        public void ResetQuestion()
        {
            CurrentQuestion = new BuilderQuestion();
        }

        /// <summary>
        /// Validates that a question has the minimum required content.
        /// </summary>
        public bool ValidateQuestion(Question question)
        {
            if (question == null)
                return false;
            if (string.IsNullOrWhiteSpace(question.QuestionText))
                return false;
            return true;
        }

        /// <summary>
        /// Ensures the multiple-choice answer list and the student answer
        /// data indices list are the same size.
        /// </summary>
        public void SyncAnswerListSize(Question question)
        {
            if (question == null || question.MultipleChoiceAnswers == null)
                return;

            int choiceCount = question.MultipleChoiceAnswers.Count;

            if (question.Answer == null)
                question.Answer = new StudentAnswerData();

            // Ensure SelectedChoiceIndices list exists
            if (question.Answer.SelectedChoiceIndices == null)
                question.Answer.SelectedChoiceIndices = new List<int>();

            // Ensure CorrectAnswers matches choice count
            while (question.CorrectAnswers.Count < choiceCount)
                question.CorrectAnswers.Add(false);
            while (question.CorrectAnswers.Count > choiceCount)
                question.CorrectAnswers.RemoveAt(question.CorrectAnswers.Count - 1);
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class NotebookBuilderControllerTests
    {
        private NotebookBuilderControllerStub _builder;

        [SetUp]
        public void SetUp()
        {
            _builder = new NotebookBuilderControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _builder = null;
        }

        // ---------------------------------------------------------------------
        // Test: BuildAnswerType with text only
        // ---------------------------------------------------------------------
        // WHY: The most common question type is text-only. If the flags are
        //      built incorrectly, the student notebook won't show the text
        //      input field and students cannot answer the question.
        // ---------------------------------------------------------------------
        [Test]
        public void BuildAnswerType_TextOnly_ReturnsTextFlag()
        {
            // Act
            AnswerType result = _builder.BuildAnswerType(
                hasText: true, hasImage: false, hasScratchpad: false, hasMultipleChoice: false);

            // Assert
            Assert.AreEqual(AnswerType.Text, result,
                "BuildAnswerType with only text=true should return AnswerType.Text");
        }

        // ---------------------------------------------------------------------
        // Test: BuildAnswerType with multiple types
        // ---------------------------------------------------------------------
        // WHY: Teachers often create questions that accept multiple response
        //      types (e.g., "explain in words AND draw a diagram"). All selected
        //      flags must be combined via bitwise OR so every input control
        //      appears in the student view.
        // ---------------------------------------------------------------------
        [Test]
        public void BuildAnswerType_MultipleTypes_ReturnsCombinedFlags()
        {
            // Act
            AnswerType result = _builder.BuildAnswerType(
                hasText: true, hasImage: true, hasScratchpad: true, hasMultipleChoice: false);

            // Assert
            AnswerType expected = AnswerType.Text | AnswerType.Image | AnswerType.Scratchpad;
            Assert.AreEqual(expected, result,
                "BuildAnswerType with text, image, and scratchpad should return combined flags (7)");
            Assert.IsTrue((result & AnswerType.Text) == AnswerType.Text,
                "Combined result should include the Text flag");
            Assert.IsTrue((result & AnswerType.Image) == AnswerType.Image,
                "Combined result should include the Image flag");
            Assert.IsTrue((result & AnswerType.Scratchpad) == AnswerType.Scratchpad,
                "Combined result should include the Scratchpad flag");
        }

        // ---------------------------------------------------------------------
        // Test: BuildAnswerType with none returns None
        // ---------------------------------------------------------------------
        // WHY: If a teacher hasn't selected any answer type yet, the flags
        //      must be AnswerType.None (0). If it returns a non-zero value,
        //      the student UI may show phantom input controls.
        // ---------------------------------------------------------------------
        [Test]
        public void BuildAnswerType_AllFalse_ReturnsNone()
        {
            // Act
            AnswerType result = _builder.BuildAnswerType(
                hasText: false, hasImage: false, hasScratchpad: false, hasMultipleChoice: false);

            // Assert
            Assert.AreEqual(AnswerType.None, result,
                "BuildAnswerType with all false should return AnswerType.None (0)");
        }

        // ---------------------------------------------------------------------
        // Test: SaveQuestion sets correct flags from builder toggles
        // ---------------------------------------------------------------------
        // WHY: SaveQuestion is the bridge between the builder UI and the data
        //      model. If it doesn't correctly translate toggle states to flags,
        //      the saved question will render incorrectly in the student view.
        // ---------------------------------------------------------------------
        [Test]
        public void SaveQuestion_SetsCorrectAnswerFlags()
        {
            // Arrange -- configure builder for a text + multiple choice question
            _builder.CurrentQuestion.QuestionText = "Name the bones in the human hand.";
            _builder.CurrentQuestion.Hint = "There are 27 bones total.";
            _builder.CurrentQuestion.HasText = true;
            _builder.CurrentQuestion.HasMultipleChoice = true;
            _builder.CurrentQuestion.MultipleChoiceAnswers = new List<string>
            {
                "Carpals", "Metacarpals", "Phalanges"
            };
            _builder.CurrentQuestion.CorrectAnswers = new List<bool> { true, true, true };

            // Act
            Question saved = _builder.SaveQuestion();

            // Assert
            AnswerType expected = AnswerType.Text | AnswerType.MultipleChoice;
            Assert.AreEqual(expected, saved.AnswerFlags,
                "Saved question should have Text | MultipleChoice flags");
            Assert.AreEqual("Name the bones in the human hand.", saved.QuestionText,
                "Saved question should preserve the question text");
            Assert.AreEqual("There are 27 bones total.", saved.Hint,
                "Saved question should preserve the hint text");
            Assert.AreEqual(3, saved.MultipleChoiceAnswers.Count,
                "Saved question should preserve all 3 multiple-choice answers");
        }

        // ---------------------------------------------------------------------
        // Test: ValidateQuestion fails on empty text
        // ---------------------------------------------------------------------
        // WHY: A question without text is meaningless to students. The builder
        //      must reject empty questions so teachers are prompted to enter
        //      content before saving.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateQuestion_EmptyText_ReturnsFalse()
        {
            // Arrange
            var emptyQuestion = new Question { QuestionText = "" };
            var whitespaceQuestion = new Question { QuestionText = "   " };

            // Act & Assert
            Assert.IsFalse(_builder.ValidateQuestion(emptyQuestion),
                "ValidateQuestion should reject a question with empty text");
            Assert.IsFalse(_builder.ValidateQuestion(whitespaceQuestion),
                "ValidateQuestion should reject a question with whitespace-only text");
            Assert.IsFalse(_builder.ValidateQuestion(null),
                "ValidateQuestion should reject a null question");
        }

        // ---------------------------------------------------------------------
        // Test: ValidateQuestion passes with valid text
        // ---------------------------------------------------------------------
        // WHY: The validation check must not be overly strict. Any non-empty
        //      question text should pass, even a short one, so teachers are not
        //      blocked by arbitrary length requirements.
        // ---------------------------------------------------------------------
        [Test]
        public void ValidateQuestion_WithText_ReturnsTrue()
        {
            // Arrange
            var validQuestion = new Question
            {
                QuestionText = "What is the function of the mitochondria?"
            };

            // Act
            bool result = _builder.ValidateQuestion(validQuestion);

            // Assert
            Assert.IsTrue(result,
                "ValidateQuestion should pass for a question with non-empty text");
        }

        // ---------------------------------------------------------------------
        // Test: ResetQuestion clears all fields
        // ---------------------------------------------------------------------
        // WHY: When a teacher clicks "New Question", all builder fields must
        //      reset to defaults. Leftover state from a previous question leaking
        //      into a new one causes confusing data corruption.
        // ---------------------------------------------------------------------
        [Test]
        public void ResetQuestion_ClearsAllFields()
        {
            // Arrange -- set up a fully populated question in the builder
            _builder.CurrentQuestion.QuestionText = "Describe the water cycle.";
            _builder.CurrentQuestion.Hint = "Evaporation, condensation, precipitation.";
            _builder.CurrentQuestion.HasText = true;
            _builder.CurrentQuestion.HasImage = true;
            _builder.CurrentQuestion.HasScratchpad = true;
            _builder.CurrentQuestion.HasMultipleChoice = true;
            _builder.CurrentQuestion.MultipleChoiceAnswers.Add("Evaporation");
            _builder.CurrentQuestion.MultipleChoiceAnswers.Add("Condensation");

            // Act
            _builder.ResetQuestion();

            // Assert
            Assert.AreEqual("", _builder.CurrentQuestion.QuestionText,
                "QuestionText should be empty after reset");
            Assert.AreEqual("", _builder.CurrentQuestion.Hint,
                "Hint should be empty after reset");
            Assert.IsFalse(_builder.CurrentQuestion.HasText,
                "HasText should be false after reset");
            Assert.IsFalse(_builder.CurrentQuestion.HasImage,
                "HasImage should be false after reset");
            Assert.IsFalse(_builder.CurrentQuestion.HasScratchpad,
                "HasScratchpad should be false after reset");
            Assert.IsFalse(_builder.CurrentQuestion.HasMultipleChoice,
                "HasMultipleChoice should be false after reset");
            Assert.AreEqual(0, _builder.CurrentQuestion.MultipleChoiceAnswers.Count,
                "MultipleChoiceAnswers list should be empty after reset");
        }

        // ---------------------------------------------------------------------
        // Test: Multiple-choice answer list syncs with correct answers size
        // ---------------------------------------------------------------------
        // WHY: When a teacher adds or removes answer choices, the CorrectAnswers
        //      boolean list must stay in sync. A mismatch causes index-out-of-
        //      range errors when the student tries to answer the question.
        // ---------------------------------------------------------------------
        [Test]
        public void SyncAnswerListSize_MatchesChoiceCount()
        {
            // Arrange -- create a question with 4 choices but only 2 correct-answer entries
            var question = new Question
            {
                QuestionText = "Which organs are part of the digestive system?",
                AnswerFlags = AnswerType.MultipleChoice,
                MultipleChoiceAnswers = new List<string>
                {
                    "Stomach", "Liver", "Lungs", "Small Intestine"
                },
                CorrectAnswers = new List<bool> { true, true },
                Answer = new StudentAnswerData()
            };

            // Act
            _builder.SyncAnswerListSize(question);

            // Assert
            Assert.AreEqual(4, question.CorrectAnswers.Count,
                "CorrectAnswers list should be padded to match the 4 multiple-choice answers");
            Assert.IsTrue(question.CorrectAnswers[0],
                "First correct answer (Stomach) should remain true after sync");
            Assert.IsTrue(question.CorrectAnswers[1],
                "Second correct answer (Liver) should remain true after sync");
            Assert.IsFalse(question.CorrectAnswers[2],
                "Padded third entry should default to false");
            Assert.IsFalse(question.CorrectAnswers[3],
                "Padded fourth entry should default to false");
        }
    }
}
