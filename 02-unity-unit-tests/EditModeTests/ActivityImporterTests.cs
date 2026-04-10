// =============================================================================
// ActivityImporterTests.cs - Edit Mode Unit Tests for ActivityImporter
// =============================================================================
// TARGET CLASS: ActivityImporter
//   Real file: Assets/zSpace/StudioA3/Scripts/ActivityPack/ActivityImporter.cs
//
// WHAT IT TESTS:
//   The filename-sanitisation helper used when importing activity packs from
//   external sources. The private makeSafeFileName method strips characters
//   that are unsafe for file systems and converts spaces to underscores.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete the stub class marked with the "TODO: DELETE this stub" comment.
//   3. The real makeSafeFileName is private. You have two options:
//      a) Use [assembly: InternalsVisibleTo("Tests")] and change access to
//         internal, OR
//      b) Keep the local copy of the logic in the test file and maintain
//         it in sync when the production code changes.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System.Text.RegularExpressions;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Contains a copy of the private makeSafeFileName logic from the real
    /// ActivityImporter. The method replaces any character that is not
    /// alphanumeric or a space with an empty string, then replaces spaces
    /// with underscores.
    /// </summary>
    public static class ActivityImporterStub
    {
        /// <summary>
        /// Produces a filesystem-safe filename from an arbitrary input string.
        /// Matches the real ActivityImporter.makeSafeFileName behaviour:
        ///   1. Strip everything that is not [a-zA-Z0-9 ].
        ///   2. Replace each space with an underscore.
        /// </summary>
        public static string MakeSafeFileName(string input)
        {
            if (input == null)
            {
                return string.Empty;
            }

            // Step 1: remove non-alphanumeric characters (preserve spaces)
            string cleaned = Regex.Replace(input, @"[^a-zA-Z0-9 ]", string.Empty);

            // Step 2: replace spaces with underscores
            string safe = cleaned.Replace(' ', '_');

            return safe;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ActivityImporterTests
    {
        [Test]
        public void MakeSafeFileName_RemovesSpecialCharacters()
        {
            // Arrange
            string input = "Lab: Heart & Lungs (v2.1)";

            // Act
            string result = ActivityImporterStub.MakeSafeFileName(input);

            // Assert
            Assert.AreEqual("Lab_Heart__Lungs_v21", result,
                "Special characters like :, &, (, ), and . should be stripped.");
        }

        [Test]
        public void MakeSafeFileName_PreservesAlphanumeric()
        {
            // Arrange
            string input = "FrogDissection3";

            // Act
            string result = ActivityImporterStub.MakeSafeFileName(input);

            // Assert
            Assert.AreEqual("FrogDissection3", result,
                "A purely alphanumeric string should pass through unchanged.");
        }

        [Test]
        public void MakeSafeFileName_ConvertsSpacesToUnderscores()
        {
            // Arrange
            string input = "Solar System Explorer";

            // Act
            string result = ActivityImporterStub.MakeSafeFileName(input);

            // Assert
            Assert.AreEqual("Solar_System_Explorer", result,
                "Spaces should be replaced with underscores.");
        }

        [Test]
        public void MakeSafeFileName_HandlesEmptyString()
        {
            // Act
            string result = ActivityImporterStub.MakeSafeFileName(string.Empty);

            // Assert
            Assert.AreEqual(string.Empty, result,
                "An empty input should produce an empty output.");
        }

        [Test]
        public void MakeSafeFileName_HandlesStringWithOnlySpecialChars()
        {
            // Arrange
            string input = "!@#$%^&*()";

            // Act
            string result = ActivityImporterStub.MakeSafeFileName(input);

            // Assert
            Assert.AreEqual(string.Empty, result,
                "A string containing only special characters should produce an empty result.");
        }
    }
}
