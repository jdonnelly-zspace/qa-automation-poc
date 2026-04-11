// =============================================================================
// AssetFileManagerTests.cs - Edit Mode Unit Tests for AssetFileManager
// =============================================================================
// TARGET CLASS: AssetFileManager
//   Real file: Assets/CommonA3/zSpace/Scripts/Utilities/AssetFileManager.cs
//
// WHAT IT TESTS:
//   Singleton file-I/O manager for scene save/load with zip packaging, path
//   normalization, and file version tracking. Tests focus on the pure-logic
//   portions: relative-path resolution, filename sanitization, and version
//   constants.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real AssetFileManager is a singleton MonoBehaviour that wraps
//      System.IO and zip operations. The stub here exercises only the
//      path-manipulation and sanitization logic without filesystem access.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the path-handling and filename-sanitization portions of the
    /// real AssetFileManager, without requiring actual filesystem or zip I/O.
    /// </summary>
    public class AssetFileManagerStub
    {
        public static readonly int SaveFileVersion = 4;

        private string _rootPath;

        public string RootPath
        {
            get { return _rootPath; }
            set { _rootPath = NormalizePath(value); }
        }

        public AssetFileManagerStub(string rootPath)
        {
            RootPath = rootPath;
        }

        /// <summary>
        /// Returns the portion of <paramref name="inPath"/> that is relative to
        /// <see cref="RootPath"/>, or null if the path is outside the root.
        /// </summary>
        public string LocalPath(string inPath)
        {
            if (string.IsNullOrEmpty(inPath))
            {
                return null;
            }

            string normalized = NormalizePath(inPath);

            if (!normalized.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string relative = normalized.Substring(_rootPath.Length);
            if (relative.StartsWith("/"))
            {
                relative = relative.Substring(1);
            }

            return relative;
        }

        /// <summary>
        /// Strips characters that are illegal in Windows/Mac/Linux filenames
        /// and trims whitespace. Returns "unnamed" for empty results.
        /// </summary>
        public string MakeSafeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "unnamed";
            }

            // Remove characters invalid on Windows (superset of Mac/Linux invalid chars)
            string safe = Regex.Replace(input, @"[<>:""/\\|?*]", "");
            safe = safe.Trim();

            if (string.IsNullOrEmpty(safe))
            {
                return "unnamed";
            }

            return safe;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }

            return path.Replace('\\', '/').TrimEnd('/');
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class AssetFileManagerTests
    {
        private AssetFileManagerStub _fileManager;

        [SetUp]
        public void SetUp()
        {
            _fileManager = new AssetFileManagerStub("C:/Users/Student/AppData/Local/zSpace/StudioA3");
        }

        [TearDown]
        public void TearDown()
        {
            _fileManager = null;
        }

        [Test]
        public void LocalPath_ReturnsRelativePath_WhenFileIsUnderRoot()
        {
            // WHY: Scene files saved inside the root must be referenced by relative
            // path so projects remain portable between student machines.

            // Act
            string result = _fileManager.LocalPath(
                "C:/Users/Student/AppData/Local/zSpace/StudioA3/Scenes/heart_dissection.zspace");

            // Assert
            Assert.AreEqual("Scenes/heart_dissection.zspace", result,
                "LocalPath should strip the root prefix and return only the relative portion.");
        }

        [Test]
        public void LocalPath_ReturnsNull_WhenFileIsOutsideRoot()
        {
            // WHY: Files outside the managed root (e.g., system files) must not
            // be treated as project assets to prevent accidental data corruption.

            // Act
            string result = _fileManager.LocalPath("D:/OtherFolder/rogue_file.txt");

            // Assert
            Assert.IsNull(result,
                "LocalPath should return null for paths outside the root directory.");
        }

        [Test]
        public void LocalPath_HandlesForwardAndBackSlashes()
        {
            // WHY: Windows paths use backslashes but Unity and zip entries use
            // forward slashes; normalization prevents path-mismatch bugs.

            // Act
            string withBackslashes = _fileManager.LocalPath(
                "C:\\Users\\Student\\AppData\\Local\\zSpace\\StudioA3\\Models\\skeleton.obj");
            string withForwardSlashes = _fileManager.LocalPath(
                "C:/Users/Student/AppData/Local/zSpace/StudioA3/Models/skeleton.obj");

            // Assert
            Assert.AreEqual("Models/skeleton.obj", withBackslashes,
                "Backslash paths should be normalized and resolve correctly.");
            Assert.AreEqual(withForwardSlashes, withBackslashes,
                "Forward-slash and backslash paths should produce identical results.");
        }

        [Test]
        public void MakeSafeFileName_RemovesInvalidCharacters()
        {
            // WHY: Student-entered scene names may include characters illegal on
            // Windows; sanitization prevents save failures and zip corruption.

            // Act
            string result = _fileManager.MakeSafeFileName("My Scene: \"Hearts & Lungs\" v2?");

            // Assert
            Assert.AreEqual("My Scene Hearts & Lungs v2", result,
                "Invalid filename characters (<>:\"/\\|?*) should be stripped.");
        }

        [Test]
        public void MakeSafeFileName_ReturnsUnnamed_WhenInputIsEmpty()
        {
            // WHY: An empty scene name would create an invisible file in the
            // file browser; a sensible default protects against this.

            // Act
            string fromEmpty = _fileManager.MakeSafeFileName("");
            string fromNull = _fileManager.MakeSafeFileName(null);
            string fromWhitespace = _fileManager.MakeSafeFileName("   ");

            // Assert
            Assert.AreEqual("unnamed", fromEmpty,
                "Empty string should produce the default 'unnamed' filename.");
            Assert.AreEqual("unnamed", fromNull,
                "Null input should produce the default 'unnamed' filename.");
            Assert.AreEqual("unnamed", fromWhitespace,
                "Whitespace-only input should produce the default 'unnamed' filename.");
        }

        [Test]
        public void MakeSafeFileName_PreservesValidNames()
        {
            // WHY: Filenames that are already safe should pass through untouched
            // to avoid surprising the student with an unexpected rename.

            // Act
            string result = _fileManager.MakeSafeFileName("Frog Dissection Lab 3");

            // Assert
            Assert.AreEqual("Frog Dissection Lab 3", result,
                "A filename with no invalid characters should be returned unchanged.");
        }

        [Test]
        public void SaveFileVersion_IsExpectedValue()
        {
            // WHY: The save file version gates deserialization logic; an unexpected
            // version would silently corrupt saved scenes during migration.

            // Assert
            Assert.AreEqual(4, AssetFileManagerStub.SaveFileVersion,
                "SaveFileVersion should be 4, matching the current zSpace Studio A3 format.");
        }

        [Test]
        public void LocalPath_ReturnsNull_WhenInputIsNullOrEmpty()
        {
            // WHY: Defensive null handling prevents NullReferenceExceptions when
            // scene-load code passes uninitialized path strings.

            // Act
            string fromNull = _fileManager.LocalPath(null);
            string fromEmpty = _fileManager.LocalPath("");

            // Assert
            Assert.IsNull(fromNull,
                "LocalPath should return null for null input.");
            Assert.IsNull(fromEmpty,
                "LocalPath should return null for empty string input.");
        }
    }
}
