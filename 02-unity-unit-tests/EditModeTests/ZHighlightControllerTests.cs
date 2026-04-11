// =============================================================================
// ZHighlightControllerTests.cs - Edit Mode Unit Tests for ZHighlightController
// =============================================================================
// TARGET CLASS: ZHighlightController
//   Real file: Assets/CommonA3/zSpace/Scripts/Highlighting/ZHighlightController.cs
//
// WHAT IT TESTS:
//   Wrapper controller that ensures a HighlightingEffect component is attached
//   to the same GameObject. Validates Awake-time initialization, auto-creation
//   of missing components, and idempotent behavior when the component already
//   exists.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real ZHighlightController is a MonoBehaviour that calls
//      GetComponent/AddComponent in Awake(). These tests exercise the logic
//      through a lightweight POCO stub so they compile standalone in the POC.
//   4. Run via Window > General > Test Runner > EditMode tab.
// =============================================================================

using System;
using NUnit.Framework;

namespace zSpace.StudioA3.Tests
{
    // -------------------------------------------------------------------------
    // Stubs - remove these when wiring up to the real codebase
    // -------------------------------------------------------------------------

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for the HighlightingEffect component from the
    /// HighlightingSystem plugin.
    /// </summary>
    public class HighlightingEffect
    {
        public bool IsActive { get; set; } = true;
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for a GameObject that can hold components, used to
    /// simulate GetComponent/AddComponent behavior without Unity runtime.
    /// </summary>
    public class MockGameObject
    {
        private HighlightingEffect _highlightingEffect;

        public void SetHighlightingEffect(HighlightingEffect effect)
        {
            _highlightingEffect = effect;
        }

        public HighlightingEffect GetComponent()
        {
            return _highlightingEffect;
        }

        public HighlightingEffect AddComponent()
        {
            _highlightingEffect = new HighlightingEffect();
            return _highlightingEffect;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the Awake-time initialization logic of the
    /// real ZHighlightController without requiring MonoBehaviour.
    /// </summary>
    public class ZHighlightControllerStub
    {
        private HighlightingEffect _highlightingEffect;
        private MockGameObject _gameObject;

        public ZHighlightControllerStub(MockGameObject gameObject)
        {
            _gameObject = gameObject ?? throw new ArgumentNullException(nameof(gameObject));
        }

        public HighlightingEffect HighlightingEffect
        {
            get { return _highlightingEffect; }
        }

        /// <summary>
        /// Simulates the Awake() callback of the real MonoBehaviour.
        /// Ensures a HighlightingEffect is present, adding one if needed.
        /// </summary>
        public void Awake()
        {
            if (_highlightingEffect == null)
            {
                HighlightingEffect existing = _gameObject.GetComponent();
                if (existing == null)
                {
                    existing = _gameObject.AddComponent();
                }

                _highlightingEffect = existing;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ZHighlightControllerTests
    {
        private MockGameObject _gameObject;
        private ZHighlightControllerStub _controller;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new MockGameObject();
            _controller = new ZHighlightControllerStub(_gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
            _gameObject = null;
        }

        [Test]
        public void Awake_NoExistingEffect_CreatesHighlightingEffect()
        {
            // WHY: If an artist forgets to add HighlightingEffect manually, the
            //       controller must auto-create it so highlight visuals work at runtime.

            // Act
            _controller.Awake();

            // Assert
            Assert.IsNotNull(_controller.HighlightingEffect,
                "Awake should create a HighlightingEffect when none exists on the GameObject.");
        }

        [Test]
        public void Awake_ExistingEffect_ReusesExistingComponent()
        {
            // WHY: If the component is already present (e.g., artist-configured settings),
            //       Awake must not overwrite it with a new default instance.

            // Arrange
            var preExisting = new HighlightingEffect();
            _gameObject.SetHighlightingEffect(preExisting);

            // Act
            _controller.Awake();

            // Assert
            Assert.AreSame(preExisting, _controller.HighlightingEffect,
                "Awake should reuse the existing HighlightingEffect, not create a new one.");
        }

        [Test]
        public void Awake_CalledTwice_DoesNotReplaceEffect()
        {
            // WHY: Idempotency prevents accidental reset of highlight settings
            //       if Unity calls Awake more than once during scene transitions.

            // Act
            _controller.Awake();
            HighlightingEffect firstEffect = _controller.HighlightingEffect;
            _controller.Awake();

            // Assert
            Assert.AreSame(firstEffect, _controller.HighlightingEffect,
                "Calling Awake a second time should not replace the HighlightingEffect.");
        }

        [Test]
        public void Awake_CreatedEffectIsActive_HighlightingReadyByDefault()
        {
            // WHY: Auto-created effects should be active so that highlight calls
            //       succeed immediately without additional setup.

            // Act
            _controller.Awake();

            // Assert
            Assert.IsTrue(_controller.HighlightingEffect.IsActive,
                "Auto-created HighlightingEffect should be active by default.");
        }

        [Test]
        public void Constructor_NullGameObject_ThrowsArgumentNullException()
        {
            // WHY: The controller cannot function without a host GameObject.
            //       Failing fast prevents confusing NullReferenceExceptions later.

            Assert.Throws<ArgumentNullException>(() => new ZHighlightControllerStub(null),
                "Constructor should throw ArgumentNullException when gameObject is null.");
        }

        [Test]
        public void HighlightingEffect_BeforeAwake_ReturnsNull()
        {
            // WHY: Before initialization, the property should be null rather than
            //       returning an uninitialized object that could hide bugs.

            // Assert
            Assert.IsNull(_controller.HighlightingEffect,
                "HighlightingEffect should be null before Awake is called.");
        }
    }
}
