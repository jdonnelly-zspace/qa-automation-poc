// =============================================================================
// AudioManagerTests.cs - Edit Mode Unit Tests for AudioManager
// =============================================================================
// TARGET CLASS: AudioManager
//   Real file: Assets/CommonA3/zSpace/Scripts/Utilities/AudioManager.cs
//
// WHAT IT TESTS:
//   Singleton audio manager that maintains a lookup dictionary of named audio
//   clips and exposes PlayClip(name) for on-demand playback. Tests validate
//   clip registration, lookup by name, case-sensitivity, duplicate handling,
//   and graceful behavior when clips are missing.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real AudioManager is a singleton MonoBehaviour that wraps Unity
//      AudioClip references. The stub here exercises only the dictionary
//      lookup and playback-tracking logic without Unity runtime.
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
    /// Lightweight stand-in for Unity's AudioClip, identified by name.
    /// </summary>
    public class AudioClipStub
    {
        public string Name { get; set; }

        public AudioClipStub(string name)
        {
            Name = name;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Mirrors the clip-management and playback-tracking portions of the real
    /// AudioManager singleton, without requiring MonoBehaviour or AudioSource.
    /// </summary>
    public class AudioManagerStub
    {
        private readonly Dictionary<string, AudioClipStub> _clipLookup =
            new Dictionary<string, AudioClipStub>();

        public AudioClipStub LastPlayedClip { get; private set; }
        public bool IsPlaying { get; private set; }

        public void AddClip(AudioClipStub clip)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            // Last-in wins for duplicate names, matching common Unity pattern
            _clipLookup[clip.Name] = clip;
        }

        public AudioClipStub PlayClip(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (!_clipLookup.TryGetValue(name, out AudioClipStub clip))
            {
                return null;
            }

            LastPlayedClip = clip;
            IsPlaying = true;
            return clip;
        }

        public void Stop()
        {
            IsPlaying = false;
        }

        public int ClipCount => _clipLookup.Count;
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class AudioManagerTests
    {
        private AudioManagerStub _audioManager;

        [SetUp]
        public void SetUp()
        {
            _audioManager = new AudioManagerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _audioManager = null;
        }

        [Test]
        public void PlayClip_ReturnsCorrectClip_WhenNameExists()
        {
            // WHY: Students hear contextual audio cues (e.g., organ sounds) during
            // anatomy lessons; playing the wrong clip breaks the learning experience.

            // Arrange
            var heartbeat = new AudioClipStub("heartbeat");
            var breathing = new AudioClipStub("breathing");
            _audioManager.AddClip(heartbeat);
            _audioManager.AddClip(breathing);

            // Act
            AudioClipStub result = _audioManager.PlayClip("heartbeat");

            // Assert
            Assert.IsNotNull(result,
                "PlayClip should return a clip when the name exists in the lookup.");
            Assert.AreEqual("heartbeat", result.Name,
                "PlayClip should return the clip whose name matches the request.");
        }

        [Test]
        public void PlayClip_ReturnsNull_WhenNameNotFound()
        {
            // WHY: If a lesson references a clip that was never registered (e.g., a
            // missing asset), the manager must fail gracefully rather than crash.

            // Arrange
            _audioManager.AddClip(new AudioClipStub("heartbeat"));

            // Act
            AudioClipStub result = _audioManager.PlayClip("nonexistent_clip");

            // Assert
            Assert.IsNull(result,
                "PlayClip should return null for a clip name not in the lookup.");
        }

        [Test]
        public void AddClip_BuildsLookupCorrectly_MultiplClips()
        {
            // WHY: A typical anatomy scene registers dozens of audio clips at load
            // time; the lookup must faithfully index every one.

            // Arrange & Act
            _audioManager.AddClip(new AudioClipStub("heartbeat"));
            _audioManager.AddClip(new AudioClipStub("breathing"));
            _audioManager.AddClip(new AudioClipStub("bloodflow"));

            // Assert
            Assert.AreEqual(3, _audioManager.ClipCount,
                "Lookup should contain exactly the number of distinct clips added.");
            Assert.IsNotNull(_audioManager.PlayClip("breathing"),
                "Each added clip should be retrievable by name.");
            Assert.IsNotNull(_audioManager.PlayClip("bloodflow"),
                "Each added clip should be retrievable by name.");
        }

        [Test]
        public void PlayClip_IsCaseSensitive_DifferentCasesAreDifferentClips()
        {
            // WHY: Asset names in Unity are case-sensitive on some platforms; the
            // lookup must respect exact casing to avoid cross-platform bugs.

            // Arrange
            _audioManager.AddClip(new AudioClipStub("Heartbeat"));

            // Act
            AudioClipStub upper = _audioManager.PlayClip("Heartbeat");
            AudioClipStub lower = _audioManager.PlayClip("heartbeat");

            // Assert
            Assert.IsNotNull(upper,
                "Exact-case match should succeed.");
            Assert.IsNull(lower,
                "Different-case lookup should fail because clip names are case-sensitive.");
        }

        [Test]
        public void AddClip_DuplicateName_LastClipWins()
        {
            // WHY: Hot-reloading or re-importing assets may register the same name
            // twice; the manager should keep the latest version rather than crash.

            // Arrange
            var original = new AudioClipStub("narration");
            var replacement = new AudioClipStub("narration");
            _audioManager.AddClip(original);

            // Act
            _audioManager.AddClip(replacement);
            AudioClipStub result = _audioManager.PlayClip("narration");

            // Assert
            Assert.AreSame(replacement, result,
                "When a duplicate name is added, the last clip should win.");
            Assert.AreEqual(1, _audioManager.ClipCount,
                "Duplicate names should not inflate the clip count.");
        }

        [Test]
        public void PlayClip_ReturnsNull_WhenManagerIsEmpty()
        {
            // WHY: A scene with no audio assets should not crash when code
            // unconditionally calls PlayClip during a lifecycle event.

            // Act
            AudioClipStub result = _audioManager.PlayClip("anything");

            // Assert
            Assert.IsNull(result,
                "PlayClip should return null when no clips have been added.");
            Assert.IsFalse(_audioManager.IsPlaying,
                "IsPlaying should remain false when no clip was played.");
        }

        [Test]
        public void PlayClip_SetsLastPlayedClipAndIsPlaying()
        {
            // WHY: UI elements (e.g., a speaker icon) rely on IsPlaying and
            // LastPlayedClip to show playback state to the student.

            // Arrange
            var clip = new AudioClipStub("label_narration");
            _audioManager.AddClip(clip);

            // Act
            _audioManager.PlayClip("label_narration");

            // Assert
            Assert.AreSame(clip, _audioManager.LastPlayedClip,
                "LastPlayedClip should reference the most recently played clip.");
            Assert.IsTrue(_audioManager.IsPlaying,
                "IsPlaying should be true after a successful PlayClip call.");
        }
    }
}
