// =============================================================================
// SlideOutMenuControllerTests.cs - Edit Mode Unit Tests for SlideOutMenuController
// =============================================================================
// TARGET CLASS: SlideOutMenuController
//   Real file: Assets/StudioA3/Scripts/UI/SlideOutMenuController.cs
//
// WHAT IT TESTS:
//   Slide-out menu that displays save dialogs, a nameplate showing the current
//   activity title with a dirty indicator, and license-dependent button
//   enablement. Tests validate nameplate text formatting, license-based button
//   restrictions (Lite vs Pro), and export filename sanitization.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real SlideOutMenuController is a MonoBehaviour that manages Unity
//      UI panels and buttons. The stub here exercises only the nameplate
//      formatting, license gating, and filename sanitization without Unity runtime.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real enum instead.
    /// <summary>
    /// Represents the license tier for the current user session.
    /// </summary>
    public enum LicenseType
    {
        Pro,
        Lite
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the nameplate, license-gating, and filename sanitization logic
    /// of the real SlideOutMenuController without requiring MonoBehaviour or
    /// Unity UI components.
    /// </summary>
    public class SlideOutMenuControllerStub
    {
        public string ActivityTitle { get; set; }
        public string FileName { get; set; }
        public bool IsDirty { get; set; }
        public LicenseType CurrentLicense { get; set; }

        private static readonly HashSet<string> LiteRestrictedButtons =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Save",
                "Settings",
                "Import",
                "ZView"
            };

        public SlideOutMenuControllerStub()
        {
            ActivityTitle = string.Empty;
            FileName = "Untitled";
            IsDirty = false;
            CurrentLicense = LicenseType.Pro;
        }

        /// <summary>
        /// Returns the nameplate text: the filename followed by " *" if unsaved
        /// changes exist.
        /// </summary>
        public string GetNameplateText()
        {
            if (IsDirty)
            {
                return FileName + " *";
            }

            return FileName;
        }

        /// <summary>
        /// Checks whether a named button should be enabled for the given license.
        /// Lite licenses restrict Save, Settings, Import, and ZView buttons.
        /// </summary>
        public bool IsButtonEnabled(string buttonName, LicenseType license)
        {
            if (license == LicenseType.Lite && LiteRestrictedButtons.Contains(buttonName))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes characters that are invalid for filenames, keeping only
        /// letters, digits, underscores, hyphens, and spaces.
        /// </summary>
        public string SanitizeExportFileName(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return Regex.Replace(input, @"[^a-zA-Z0-9_\- ]", "");
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class SlideOutMenuControllerTests
    {
        private SlideOutMenuControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new SlideOutMenuControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void GetNameplateText_ShowsFileName()
        {
            // WHY: The nameplate tells the student which activity file they are
            // working on; displaying the wrong name causes confusion and misfiles.

            // Arrange
            _controller.FileName = "Human Anatomy Lesson";
            _controller.IsDirty = false;

            // Act
            string result = _controller.GetNameplateText();

            // Assert
            Assert.AreEqual("Human Anatomy Lesson", result,
                "Nameplate should display the filename when the activity is clean.");
        }

        [Test]
        public void GetNameplateText_AppendsAsterisk_WhenDirty()
        {
            // WHY: The asterisk is the universal unsaved-changes indicator; students
            // and teachers rely on it to know they need to save before closing.

            // Arrange
            _controller.FileName = "Solar System Project";
            _controller.IsDirty = true;

            // Act
            string result = _controller.GetNameplateText();

            // Assert
            Assert.AreEqual("Solar System Project *", result,
                "Nameplate should append ' *' when the activity has unsaved changes.");
        }

        [Test]
        public void GetNameplateText_NoAsterisk_WhenClean()
        {
            // WHY: Showing an asterisk on a clean file is a false alarm that
            // wastes a teacher's time trying to save already-saved work.

            // Arrange
            _controller.FileName = "Volcano Simulation";
            _controller.IsDirty = false;

            // Act
            string result = _controller.GetNameplateText();

            // Assert
            Assert.IsFalse(result.Contains("*"),
                "Nameplate should not contain an asterisk when there are no unsaved changes.");
        }

        [Test]
        public void IsButtonEnabled_ProLicense_AllButtonsEnabled()
        {
            // WHY: Pro-licensed schools have paid for the full feature set;
            // every menu option must be accessible.

            // Act & Assert
            Assert.IsTrue(_controller.IsButtonEnabled("Save", LicenseType.Pro),
                "Save button should be enabled for Pro license.");
            Assert.IsTrue(_controller.IsButtonEnabled("Settings", LicenseType.Pro),
                "Settings button should be enabled for Pro license.");
            Assert.IsTrue(_controller.IsButtonEnabled("Import", LicenseType.Pro),
                "Import button should be enabled for Pro license.");
            Assert.IsTrue(_controller.IsButtonEnabled("ZView", LicenseType.Pro),
                "ZView button should be enabled for Pro license.");
            Assert.IsTrue(_controller.IsButtonEnabled("Open", LicenseType.Pro),
                "Open button should be enabled for Pro license.");
        }

        [Test]
        public void IsButtonEnabled_LiteLicense_DisablesSave()
        {
            // WHY: Lite license restricts Save to encourage upgrade; allowing
            // save in Lite would violate the licensing agreement.

            // Act
            bool result = _controller.IsButtonEnabled("Save", LicenseType.Lite);

            // Assert
            Assert.IsFalse(result,
                "Save button should be disabled for Lite license.");
        }

        [Test]
        public void IsButtonEnabled_LiteLicense_DisablesImport()
        {
            // WHY: Import of custom 3D models is a Pro feature; Lite users
            // work only with the bundled content library.

            // Act
            bool result = _controller.IsButtonEnabled("Import", LicenseType.Lite);

            // Assert
            Assert.IsFalse(result,
                "Import button should be disabled for Lite license.");
        }

        [Test]
        public void SanitizeExportFileName_RemovesSpecialCharacters()
        {
            // WHY: Export filenames with special characters cause cross-platform
            // file system errors, especially when transferring activities via USB.

            // Arrange
            string dirty = "My <Activity> @2024/v1.0!";

            // Act
            string result = _controller.SanitizeExportFileName(dirty);

            // Assert
            Assert.AreEqual("My Activity 2024v10", result,
                "Sanitizer should strip characters not in [a-zA-Z0-9_\\- ].");
        }

        [Test]
        public void SanitizeExportFileName_PreservesValidCharacters()
        {
            // WHY: Overly aggressive sanitization that strips hyphens or
            // underscores would break naming conventions teachers rely on.

            // Arrange
            string clean = "Lesson_03 - Heart Anatomy";

            // Act
            string result = _controller.SanitizeExportFileName(clean);

            // Assert
            Assert.AreEqual("Lesson_03 - Heart Anatomy", result,
                "Sanitizer should preserve letters, digits, underscores, hyphens, and spaces.");
        }

        [Test]
        public void SanitizeExportFileName_HandlesEmptyString()
        {
            // WHY: An empty filename field should not crash the export pipeline;
            // returning empty lets the caller display a validation message.

            // Act
            string result = _controller.SanitizeExportFileName("");

            // Assert
            Assert.AreEqual(string.Empty, result,
                "Sanitizer should return an empty string for empty input.");
        }
    }
}
