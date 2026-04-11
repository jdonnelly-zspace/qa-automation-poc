// =============================================================================
// SessionStorageManagerTests.cs - Edit Mode Unit Tests for SessionStorageManager
// =============================================================================
// TARGET CLASS: SessionStorageManager
//   Real file: Assets/CommonA3/zSpace/Scripts/WebSessionStorage/SessionStorageManager.cs
//
// WHAT IT TESTS:
//   WebGL session persistence layer that saves and restores student answers as
//   JSON in browser sessionStorage. Tests validate save/load roundtrips, missing
//   session handling, overwrite behavior, multi-activity independence, and
//   answer data integrity through serialization.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace the using directives with the real namespaces.
//   3. The real SessionStorageManager calls into JavaScript interop for
//      browser sessionStorage. The stub here uses a Dictionary to simulate
//      the key/value store without a browser runtime.
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
    /// Represents a single student answer to a question in an activity.
    /// </summary>
    public class StudentAnswer
    {
        public string QuestionId { get; set; }
        public string TextAnswer { get; set; }
        public int SelectedChoice { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Top-level session data for a single activity, containing the student's
    /// name, all answers, and a timestamp.
    /// </summary>
    public class SessionData
    {
        public string ActivityId { get; set; }
        public string StudentName { get; set; }
        public List<StudentAnswer> Answers { get; set; } = new List<StudentAnswer>();
        public DateTime SavedAt { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Simulates browser sessionStorage using an in-memory Dictionary. Mirrors
    /// the Save/Load/HasSavedSession/ClearSession API of the real manager.
    /// </summary>
    public class SessionStorageManagerStub
    {
        public Dictionary<string, string> Storage { get; private set; } =
            new Dictionary<string, string>();

        public void Save(SessionData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Minimal JSON serialization for answers
            var answerJsonParts = data.Answers.Select(a =>
                $"{{\"QuestionId\":\"{a.QuestionId}\",\"TextAnswer\":\"{Escape(a.TextAnswer)}\",\"SelectedChoice\":{a.SelectedChoice}}}");

            string json = $"{{\"ActivityId\":\"{data.ActivityId}\"," +
                          $"\"StudentName\":\"{Escape(data.StudentName)}\"," +
                          $"\"Answers\":[{string.Join(",", answerJsonParts)}]," +
                          $"\"SavedAt\":\"{data.SavedAt:O}\"}}";

            Storage[data.ActivityId] = json;
        }

        public SessionData Load(string activityId)
        {
            if (string.IsNullOrEmpty(activityId) || !Storage.ContainsKey(activityId))
            {
                return null;
            }

            string json = Storage[activityId];

            // Minimal parser for the known JSON format
            var session = new SessionData
            {
                ActivityId = ExtractString(json, "ActivityId"),
                StudentName = ExtractString(json, "StudentName"),
                SavedAt = DateTime.Parse(ExtractString(json, "SavedAt"),
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind)
            };

            // Parse answers array
            int answersStart = json.IndexOf("[", StringComparison.Ordinal);
            int answersEnd = json.IndexOf("]", StringComparison.Ordinal);
            if (answersStart >= 0 && answersEnd > answersStart)
            {
                string answersBlock = json.Substring(answersStart + 1, answersEnd - answersStart - 1);
                if (!string.IsNullOrWhiteSpace(answersBlock))
                {
                    // Split on },{ to get individual answer objects
                    string[] parts = answersBlock.Split(new[] { "},{" }, StringSplitOptions.None);
                    foreach (string part in parts)
                    {
                        string cleaned = part.TrimStart('{').TrimEnd('}');
                        cleaned = "{" + cleaned + "}";
                        session.Answers.Add(new StudentAnswer
                        {
                            QuestionId = ExtractString(cleaned, "QuestionId"),
                            TextAnswer = ExtractString(cleaned, "TextAnswer"),
                            SelectedChoice = int.Parse(ExtractValue(cleaned, "SelectedChoice"))
                        });
                    }
                }
            }

            return session;
        }

        public bool HasSavedSession(string activityId)
        {
            return !string.IsNullOrEmpty(activityId) && Storage.ContainsKey(activityId);
        }

        public void ClearSession(string activityId)
        {
            if (!string.IsNullOrEmpty(activityId))
            {
                Storage.Remove(activityId);
            }
        }

        private string Escape(string value)
        {
            return value?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
        }

        private string ExtractString(string json, string key)
        {
            string search = $"\"{key}\":\"";
            int start = json.IndexOf(search, StringComparison.Ordinal);
            if (start < 0) return "";
            start += search.Length;
            int end = json.IndexOf("\"", start, StringComparison.Ordinal);
            if (end < 0) return "";
            return json.Substring(start, end - start);
        }

        private string ExtractValue(string json, string key)
        {
            string search = $"\"{key}\":";
            int start = json.IndexOf(search, StringComparison.Ordinal);
            if (start < 0) return "0";
            start += search.Length;
            int end = json.IndexOfAny(new[] { ',', '}' }, start);
            if (end < 0) return "0";
            return json.Substring(start, end - start).Trim();
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class SessionStorageManagerTests
    {
        private SessionStorageManagerStub _storageManager;

        [SetUp]
        public void SetUp()
        {
            _storageManager = new SessionStorageManagerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _storageManager = null;
        }

        [Test]
        public void Save_ThenLoad_RetrievesStoredSession()
        {
            // WHY: Students working through a heart dissection lesson must be able to
            // resume their progress after navigating away and back in the browser.

            // Arrange
            var session = new SessionData
            {
                ActivityId = "anatomy-heart-101",
                StudentName = "Maria Garcia",
                SavedAt = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc),
                Answers = new List<StudentAnswer>
                {
                    new StudentAnswer { QuestionId = "q1", TextAnswer = "Left ventricle", SelectedChoice = 2 }
                }
            };

            // Act
            _storageManager.Save(session);
            SessionData loaded = _storageManager.Load("anatomy-heart-101");

            // Assert
            Assert.IsNotNull(loaded,
                "Load should return the session data that was previously saved.");
            Assert.AreEqual("anatomy-heart-101", loaded.ActivityId,
                "Loaded ActivityId should match the saved value.");
            Assert.AreEqual("Maria Garcia", loaded.StudentName,
                "Loaded StudentName should match the saved value.");
        }

        [Test]
        public void Load_WithNoSavedData_ReturnsNull()
        {
            // WHY: A student opening a lesson for the first time has no saved session;
            // the manager must return null so the lesson starts fresh.

            // Act
            SessionData result = _storageManager.Load("anatomy-skeleton-201");

            // Assert
            Assert.IsNull(result,
                "Load should return null when no session has been saved for the given activity.");
        }

        [Test]
        public void HasSavedSession_ReturnsTrue_AfterSave()
        {
            // WHY: The lesson launcher checks HasSavedSession to show a "Resume"
            // button; it must return true when a session exists.

            // Arrange
            var session = new SessionData
            {
                ActivityId = "anatomy-lungs-102",
                StudentName = "James Chen",
                SavedAt = DateTime.UtcNow
            };
            _storageManager.Save(session);

            // Act & Assert
            Assert.IsTrue(_storageManager.HasSavedSession("anatomy-lungs-102"),
                "HasSavedSession should return true after a session has been saved for this activity.");
        }

        [Test]
        public void HasSavedSession_ReturnsFalse_BeforeSave()
        {
            // WHY: Before any data is saved, the launcher must not show a misleading
            // "Resume" button; HasSavedSession must report false.

            // Act & Assert
            Assert.IsFalse(_storageManager.HasSavedSession("anatomy-brain-301"),
                "HasSavedSession should return false when no session exists for the activity.");
        }

        [Test]
        public void ClearSession_RemovesSavedData()
        {
            // WHY: When a student clicks "Start Over", the previous session must be
            // fully removed so they begin with a clean slate.

            // Arrange
            var session = new SessionData
            {
                ActivityId = "anatomy-eye-103",
                StudentName = "Aisha Johnson",
                SavedAt = DateTime.UtcNow,
                Answers = new List<StudentAnswer>
                {
                    new StudentAnswer { QuestionId = "q1", TextAnswer = "Cornea", SelectedChoice = 1 }
                }
            };
            _storageManager.Save(session);
            Assert.IsTrue(_storageManager.HasSavedSession("anatomy-eye-103"),
                "Precondition: session should exist before clearing.");

            // Act
            _storageManager.ClearSession("anatomy-eye-103");

            // Assert
            Assert.IsFalse(_storageManager.HasSavedSession("anatomy-eye-103"),
                "HasSavedSession should return false after ClearSession.");
            Assert.IsNull(_storageManager.Load("anatomy-eye-103"),
                "Load should return null after ClearSession.");
        }

        [Test]
        public void Save_OverwritesPreviousSession()
        {
            // WHY: Each time a student submits new answers, the session must update
            // rather than accumulate stale copies in storage.

            // Arrange
            var original = new SessionData
            {
                ActivityId = "anatomy-muscle-104",
                StudentName = "Tom Wilson",
                SavedAt = new DateTime(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc),
                Answers = new List<StudentAnswer>
                {
                    new StudentAnswer { QuestionId = "q1", TextAnswer = "Biceps", SelectedChoice = 0 }
                }
            };
            _storageManager.Save(original);

            var updated = new SessionData
            {
                ActivityId = "anatomy-muscle-104",
                StudentName = "Tom Wilson",
                SavedAt = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
                Answers = new List<StudentAnswer>
                {
                    new StudentAnswer { QuestionId = "q1", TextAnswer = "Triceps", SelectedChoice = 1 }
                }
            };

            // Act
            _storageManager.Save(updated);
            SessionData loaded = _storageManager.Load("anatomy-muscle-104");

            // Assert
            Assert.AreEqual(1, _storageManager.Storage.Count,
                "Save with the same ActivityId should overwrite, not create a duplicate entry.");
            Assert.AreEqual("Triceps", loaded.Answers[0].TextAnswer,
                "Loaded answer should reflect the most recently saved data.");
        }

        [Test]
        public void MultipleActivities_StoredIndependently()
        {
            // WHY: A student may work on multiple lessons in the same browser session;
            // each activity's data must be isolated from the others.

            // Arrange
            var heartSession = new SessionData
            {
                ActivityId = "anatomy-heart-101",
                StudentName = "Elena Ruiz",
                SavedAt = DateTime.UtcNow,
                Answers = new List<StudentAnswer>
                {
                    new StudentAnswer { QuestionId = "q1", TextAnswer = "Aorta", SelectedChoice = 3 }
                }
            };
            var skeletonSession = new SessionData
            {
                ActivityId = "anatomy-skeleton-201",
                StudentName = "Elena Ruiz",
                SavedAt = DateTime.UtcNow,
                Answers = new List<StudentAnswer>
                {
                    new StudentAnswer { QuestionId = "q1", TextAnswer = "Femur", SelectedChoice = 1 }
                }
            };

            // Act
            _storageManager.Save(heartSession);
            _storageManager.Save(skeletonSession);

            // Assert
            Assert.AreEqual(2, _storageManager.Storage.Count,
                "Two different ActivityIds should produce two separate storage entries.");

            SessionData loadedHeart = _storageManager.Load("anatomy-heart-101");
            SessionData loadedSkeleton = _storageManager.Load("anatomy-skeleton-201");

            Assert.AreEqual("Aorta", loadedHeart.Answers[0].TextAnswer,
                "Heart session answers should be independent from skeleton session.");
            Assert.AreEqual("Femur", loadedSkeleton.Answers[0].TextAnswer,
                "Skeleton session answers should be independent from heart session.");
        }

        [Test]
        public void AnswerData_PreservedThroughSaveLoadRoundtrip()
        {
            // WHY: Every field of a student's answer (question ID, text, selected
            // choice) must survive serialization so grading logic sees accurate data.

            // Arrange
            var session = new SessionData
            {
                ActivityId = "anatomy-digestive-105",
                StudentName = "David Park",
                SavedAt = new DateTime(2026, 4, 1, 14, 0, 0, DateTimeKind.Utc),
                Answers = new List<StudentAnswer>
                {
                    new StudentAnswer { QuestionId = "q-stomach-fn", TextAnswer = "Digestion of proteins", SelectedChoice = 2 },
                    new StudentAnswer { QuestionId = "q-liver-role", TextAnswer = "Bile production", SelectedChoice = 0 }
                }
            };

            // Act
            _storageManager.Save(session);
            SessionData loaded = _storageManager.Load("anatomy-digestive-105");

            // Assert
            Assert.AreEqual(2, loaded.Answers.Count,
                "All answers should survive the save/load roundtrip.");
            Assert.AreEqual("q-stomach-fn", loaded.Answers[0].QuestionId,
                "First answer QuestionId should be preserved.");
            Assert.AreEqual("Digestion of proteins", loaded.Answers[0].TextAnswer,
                "First answer TextAnswer should be preserved.");
            Assert.AreEqual(2, loaded.Answers[0].SelectedChoice,
                "First answer SelectedChoice should be preserved.");
            Assert.AreEqual("q-liver-role", loaded.Answers[1].QuestionId,
                "Second answer QuestionId should be preserved.");
            Assert.AreEqual("Bile production", loaded.Answers[1].TextAnswer,
                "Second answer TextAnswer should be preserved.");
            Assert.AreEqual(0, loaded.Answers[1].SelectedChoice,
                "Second answer SelectedChoice should be preserved.");
        }
    }
}
