// =============================================================================
// ImportUIControllerTests.cs - Edit Mode Unit Tests for ImportUIController
// =============================================================================
// TARGET CLASS: ImportUIController
//   Real file: Assets/StudioA3/Scripts/UI/ImportUIController.cs
//
// WHAT IT TESTS:
//   Import panel that routes file type selection based on the import button
//   pressed (Audio, Image, Model, Video) and adjusts available extensions by
//   platform (Windows vs WebGL). Tests validate file filter generation,
//   platform-specific extension lists, and panel visibility toggling.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real ImportUIController is a MonoBehaviour that opens native file
//      dialogs and manages Unity UI panels. The stub here exercises only the
//      file-filter logic and visibility state without Unity runtime.
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

    // TODO: DELETE this stub when integrating into the Unity project — use the real enum instead.
    /// <summary>
    /// Represents the runtime platform for import compatibility checks.
    /// </summary>
    public enum Platform
    {
        Windows,
        WebGL
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the file-filter routing and visibility logic of the real
    /// ImportUIController without requiring MonoBehaviour or native file dialogs.
    /// </summary>
    public class ImportUIControllerStub
    {
        public bool IsVisible { get; private set; }
        public Platform CurrentPlatform { get; set; }

        private static readonly Dictionary<string, Dictionary<Platform, List<string>>> ExtensionMap =
            new Dictionary<string, Dictionary<Platform, List<string>>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "AudioButton", new Dictionary<Platform, List<string>>
                    {
                        { Platform.Windows, new List<string> { ".wav", ".mp3" } },
                        { Platform.WebGL, new List<string> { ".wav", ".mp3" } }
                    }
                },
                {
                    "ImageButton", new Dictionary<Platform, List<string>>
                    {
                        { Platform.Windows, new List<string> { ".png", ".jpg", ".jpeg" } },
                        { Platform.WebGL, new List<string> { ".png", ".jpg", ".jpeg" } }
                    }
                },
                {
                    "ModelButton", new Dictionary<Platform, List<string>>
                    {
                        { Platform.Windows, new List<string> { ".dae", ".obj", ".fbx" } },
                        { Platform.WebGL, new List<string> { ".zip" } }
                    }
                },
                {
                    "VideoButton", new Dictionary<Platform, List<string>>
                    {
                        { Platform.Windows, new List<string> { ".mp4" } },
                        { Platform.WebGL, new List<string> { ".mp4" } }
                    }
                }
            };

        public ImportUIControllerStub()
        {
            IsVisible = false;
            CurrentPlatform = Platform.Windows;
        }

        public void Show()
        {
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
        }

        /// <summary>
        /// Returns a file extension filter string suitable for a file dialog,
        /// e.g., "*.wav;*.mp3".
        /// </summary>
        public string GetFileFilter(string buttonName, Platform platform)
        {
            List<string> extensions = GetSupportedExtensions(buttonName, platform);
            if (extensions.Count == 0)
            {
                return string.Empty;
            }

            var filters = new List<string>();
            foreach (string ext in extensions)
            {
                filters.Add("*" + ext);
            }
            return string.Join(";", filters);
        }

        /// <summary>
        /// Returns the list of supported file extensions for the given import
        /// button and platform combination.
        /// </summary>
        public List<string> GetSupportedExtensions(string buttonName, Platform platform)
        {
            if (string.IsNullOrEmpty(buttonName))
            {
                return new List<string>();
            }

            if (ExtensionMap.TryGetValue(buttonName, out var platformMap))
            {
                if (platformMap.TryGetValue(platform, out List<string> extensions))
                {
                    return new List<string>(extensions);
                }
            }

            return new List<string>();
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ImportUIControllerTests
    {
        private ImportUIControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new ImportUIControllerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
        }

        [Test]
        public void GetSupportedExtensions_AudioButton_ReturnsWavAndMp3()
        {
            // WHY: Students import audio narration for their activities; the
            // file dialog must show only compatible audio formats.

            // Act
            List<string> extensions = _controller.GetSupportedExtensions("AudioButton", Platform.Windows);

            // Assert
            Assert.Contains(".wav", extensions,
                "Audio import should support .wav files.");
            Assert.Contains(".mp3", extensions,
                "Audio import should support .mp3 files.");
            Assert.AreEqual(2, extensions.Count,
                "Audio import should support exactly wav and mp3.");
        }

        [Test]
        public void GetSupportedExtensions_ImageButton_ReturnsPngAndJpg()
        {
            // WHY: Image imports are used for background slides and reference
            // photos; supporting common formats avoids student frustration.

            // Act
            List<string> extensions = _controller.GetSupportedExtensions("ImageButton", Platform.Windows);

            // Assert
            Assert.Contains(".png", extensions,
                "Image import should support .png files.");
            Assert.Contains(".jpg", extensions,
                "Image import should support .jpg files.");
            Assert.Contains(".jpeg", extensions,
                "Image import should support .jpeg files.");
        }

        [Test]
        public void GetSupportedExtensions_ModelButton_Windows_ReturnsDaeObjFbx()
        {
            // WHY: On Windows, students can import native 3D model formats
            // directly; supporting multiple formats increases content flexibility.

            // Act
            List<string> extensions = _controller.GetSupportedExtensions("ModelButton", Platform.Windows);

            // Assert
            Assert.Contains(".dae", extensions,
                "Windows model import should support COLLADA (.dae) files.");
            Assert.Contains(".obj", extensions,
                "Windows model import should support Wavefront (.obj) files.");
            Assert.Contains(".fbx", extensions,
                "Windows model import should support FBX (.fbx) files.");
            Assert.AreEqual(3, extensions.Count,
                "Windows model import should support exactly three formats.");
        }

        [Test]
        public void GetSupportedExtensions_ModelButton_WebGL_ReturnsZipOnly()
        {
            // WHY: WebGL cannot process raw 3D files at runtime; models must be
            // pre-packaged as zip bundles, so the dialog should only show .zip.

            // Act
            List<string> extensions = _controller.GetSupportedExtensions("ModelButton", Platform.WebGL);

            // Assert
            Assert.AreEqual(1, extensions.Count,
                "WebGL model import should support exactly one format.");
            Assert.Contains(".zip", extensions,
                "WebGL model import should only support .zip bundles.");
        }

        [Test]
        public void GetSupportedExtensions_VideoButton_ReturnsMp4()
        {
            // WHY: Video playback in Unity uses the VideoPlayer component which
            // reliably handles mp4; other formats cause cross-platform issues.

            // Act
            List<string> extensions = _controller.GetSupportedExtensions("VideoButton", Platform.Windows);

            // Assert
            Assert.AreEqual(1, extensions.Count,
                "Video import should support exactly one format.");
            Assert.Contains(".mp4", extensions,
                "Video import should support .mp4 files.");
        }

        [Test]
        public void Show_SetsVisibleTrue()
        {
            // WHY: When a student clicks Import in the slide-out menu, the import
            // panel must appear so they can choose a file type to import.

            // Arrange
            Assert.IsFalse(_controller.IsVisible);

            // Act
            _controller.Show();

            // Assert
            Assert.IsTrue(_controller.IsVisible,
                "Show should set IsVisible to true.");
        }

        [Test]
        public void Hide_SetsVisibleFalse()
        {
            // WHY: After importing a file or pressing Cancel, the panel should
            // disappear so it does not obscure the 3D workspace.

            // Arrange
            _controller.Show();
            Assert.IsTrue(_controller.IsVisible);

            // Act
            _controller.Hide();

            // Assert
            Assert.IsFalse(_controller.IsVisible,
                "Hide should set IsVisible to false.");
        }

        [Test]
        public void Hide_WhenAlreadyHidden_IsSafe()
        {
            // WHY: Multiple code paths may call Hide (e.g., ESC key and Cancel
            // button); double-hiding should not throw or cause side effects.

            // Arrange — panel starts hidden
            Assert.IsFalse(_controller.IsVisible);

            // Act
            _controller.Hide();

            // Assert
            Assert.IsFalse(_controller.IsVisible,
                "Calling Hide when already hidden should not throw or change state.");
        }

        [Test]
        public void GetFileFilter_UnknownButton_ReturnsEmptyString()
        {
            // WHY: If a new import type is added but not yet mapped, the filter
            // should return empty rather than crash the file dialog.

            // Act
            string filter = _controller.GetFileFilter("UnknownButton", Platform.Windows);

            // Assert
            Assert.AreEqual(string.Empty, filter,
                "Unrecognized button name should return an empty file filter.");
        }
    }
}
