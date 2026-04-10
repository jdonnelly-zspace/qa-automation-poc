// =============================================================================
// SceneManagerTests.cs - Edit Mode Unit Tests for SceneManager
// =============================================================================
// TARGET CLASS: SceneManager
//   Real file: Assets/zSpace/StudioA3/Scripts/Scene/SceneManager.cs
//
// WHAT IT TESTS:
//   The scene management layer that handles definitions (locale-aware word
//   glossary entries), scene creation with unique naming, and clipboard
//   operations. These tests focus on definition CRUD and the unique-name
//   generation used when cloning scenes.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real SceneManager is a MonoBehaviour that manages Unity Scene
//      objects. The stub here exercises only the pure-data operations
//      (definitions and name generation) without Unity runtime.
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
    /// Mirrors the real Definition data class used by the glossary system.
    /// </summary>
    public class Definition
    {
        public string Word { get; set; }
        public string ID { get; set; }
        public string Text { get; set; }
        public string Locale { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight stand-in for the definition-management and scene-naming
    /// portions of the real SceneManager.
    /// </summary>
    public class SceneManagerStub
    {
        // locale -> word -> id -> definition
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, Definition>>>
            _definitions = new Dictionary<string, Dictionary<string, Dictionary<string, Definition>>>(
                StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _existingSceneNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        public void SetDefinition(string locale, string word, string id, string definitionText)
        {
            if (!_definitions.ContainsKey(locale))
            {
                _definitions[locale] = new Dictionary<string, Dictionary<string, Definition>>(
                    StringComparer.OrdinalIgnoreCase);
            }

            if (!_definitions[locale].ContainsKey(word))
            {
                _definitions[locale][word] = new Dictionary<string, Definition>(
                    StringComparer.OrdinalIgnoreCase);
            }

            _definitions[locale][word][id] = new Definition
            {
                Word = word,
                ID = id,
                Text = definitionText,
                Locale = locale
            };
        }

        public Definition GetDefinition(string locale, string word, string id)
        {
            if (!_definitions.ContainsKey(locale))
            {
                return null;
            }

            if (!_definitions[locale].ContainsKey(word))
            {
                return null;
            }

            if (!_definitions[locale][word].ContainsKey(id))
            {
                return null;
            }

            return _definitions[locale][word][id];
        }

        /// <summary>
        /// Registers a scene name so the unique-name generator can avoid it.
        /// </summary>
        public void RegisterSceneName(string name)
        {
            _existingSceneNames.Add(name);
        }

        /// <summary>
        /// Generates a unique scene name by appending an incrementing suffix
        /// when the base name is already taken. Mirrors the real
        /// SceneManager's clone-naming logic.
        /// </summary>
        public string GenerateUniqueSceneName(string baseName)
        {
            if (!_existingSceneNames.Contains(baseName))
            {
                _existingSceneNames.Add(baseName);
                return baseName;
            }

            int suffix = 1;
            string candidate;
            do
            {
                candidate = $"{baseName} ({suffix})";
                suffix++;
            }
            while (_existingSceneNames.Contains(candidate));

            _existingSceneNames.Add(candidate);
            return candidate;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class SceneManagerTests
    {
        private SceneManagerStub _sceneManager;

        [SetUp]
        public void SetUp()
        {
            _sceneManager = new SceneManagerStub();
        }

        [TearDown]
        public void TearDown()
        {
            _sceneManager = null;
        }

        [Test]
        public void SetDefinition_CreatesLocaleIfMissing()
        {
            // Act - set a definition for a locale that does not exist yet
            _sceneManager.SetDefinition("fr-FR", "coeur", "def1", "Organe musculaire creux");

            // Assert
            Definition result = _sceneManager.GetDefinition("fr-FR", "coeur", "def1");
            Assert.IsNotNull(result,
                "Definition should exist after SetDefinition creates the locale.");
            Assert.AreEqual("Organe musculaire creux", result.Text);
            Assert.AreEqual("fr-FR", result.Locale);
        }

        [Test]
        public void GetDefinition_ReturnsNullForUnknownLocale()
        {
            // Arrange - add a definition in English only
            _sceneManager.SetDefinition("en-US", "heart", "def1", "A muscular organ");

            // Act
            Definition result = _sceneManager.GetDefinition("ja-JP", "heart", "def1");

            // Assert
            Assert.IsNull(result,
                "Should return null when the locale has no definitions.");
        }

        [Test]
        public void GetDefinition_ReturnsCorrectDefinition()
        {
            // Arrange
            _sceneManager.SetDefinition("en-US", "heart", "def1", "A muscular organ");
            _sceneManager.SetDefinition("en-US", "heart", "def2", "The center of emotions");
            _sceneManager.SetDefinition("en-US", "lung", "def1", "A respiratory organ");

            // Act
            Definition heartDef2 = _sceneManager.GetDefinition("en-US", "heart", "def2");
            Definition lungDef1 = _sceneManager.GetDefinition("en-US", "lung", "def1");

            // Assert
            Assert.IsNotNull(heartDef2);
            Assert.AreEqual("The center of emotions", heartDef2.Text);

            Assert.IsNotNull(lungDef1);
            Assert.AreEqual("A respiratory organ", lungDef1.Text);
        }

        [Test]
        public void SetDefinition_OverwritesExisting()
        {
            // Arrange
            _sceneManager.SetDefinition("en-US", "heart", "def1", "Old definition");

            // Act
            _sceneManager.SetDefinition("en-US", "heart", "def1", "Updated definition");

            // Assert
            Definition result = _sceneManager.GetDefinition("en-US", "heart", "def1");
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated definition", result.Text,
                "SetDefinition should overwrite an existing entry with the same key.");
        }

        [Test]
        public void CloneScene_GeneratesUniqueNames()
        {
            // Arrange - register an existing scene name
            _sceneManager.RegisterSceneName("My Scene");

            // Act - clone generates a unique name to avoid collision
            string clone1 = _sceneManager.GenerateUniqueSceneName("My Scene");
            string clone2 = _sceneManager.GenerateUniqueSceneName("My Scene");
            string noConflict = _sceneManager.GenerateUniqueSceneName("Brand New Scene");

            // Assert
            Assert.AreEqual("My Scene (1)", clone1,
                "First clone should get suffix (1).");
            Assert.AreEqual("My Scene (2)", clone2,
                "Second clone should get suffix (2).");
            Assert.AreEqual("Brand New Scene", noConflict,
                "A name with no conflict should be returned as-is.");
        }
    }
}
