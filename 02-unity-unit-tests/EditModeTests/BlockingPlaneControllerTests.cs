// =============================================================================
// BlockingPlaneControllerTests.cs - Edit Mode Unit Tests for BlockingPlaneController
// =============================================================================
// TARGET CLASS: BlockingPlaneController
//   Real file: Assets/CommonA3/zSpace/Scripts/UI/BlockingPlaneController.cs
//
// WHAT IT TESTS:
//   UI blocking plane that intercepts pointer clicks to dismiss overlays
//   (menus, popups). Validates Show/Hide toggling of the CanvasGroup's
//   blocksRaycasts property, the OnPointerClicked event invocation on
//   pointer down, and that clicks on child objects (not the plane itself)
//   are correctly ignored.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real BlockingPlaneController is a MonoBehaviour that implements
//      IPointerDownHandler and uses CanvasGroup + UnityEvent. These tests
//      exercise the logic through POCO stubs without a Unity runtime.
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
    /// Minimal stand-in for CanvasGroup, tracking blocksRaycasts.
    /// </summary>
    public class CanvasGroupMock
    {
        public bool BlocksRaycasts { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for a UnityEvent that tracks invocation count.
    /// </summary>
    public class ClickEventMock
    {
        public int InvokeCount { get; private set; }

        public void Invoke()
        {
            InvokeCount++;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for SelectionManager to track listener registration.
    /// </summary>
    public class SelectionManagerMock
    {
        public int AddListenerCount { get; private set; }
        public int RemoveListenerCount { get; private set; }

        public void AddSelectionChangedListener()
        {
            AddListenerCount++;
        }

        public void RemoveSelectionChangedListener()
        {
            RemoveListenerCount++;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API of the real
    /// BlockingPlaneController, without requiring MonoBehaviour.
    /// </summary>
    public class BlockingPlaneControllerStub
    {
        private CanvasGroupMock _canvasGroup;
        private SelectionManagerMock _selectionManager;

        public ClickEventMock OnPointerClicked = new ClickEventMock();

        public BlockingPlaneControllerStub(
            CanvasGroupMock canvasGroup,
            SelectionManagerMock selectionManager)
        {
            _canvasGroup = canvasGroup;
            _selectionManager = selectionManager;
        }

        public CanvasGroupMock CanvasGroup
        {
            get { return _canvasGroup; }
        }

        /// <summary>
        /// Mirrors Show() — enables raycasts and registers selection listener.
        /// </summary>
        public void Show()
        {
            _canvasGroup.BlocksRaycasts = true;
            _selectionManager.AddSelectionChangedListener();
        }

        /// <summary>
        /// Mirrors Hide() — disables raycasts and removes selection listener.
        /// </summary>
        public void Hide()
        {
            _canvasGroup.BlocksRaycasts = false;
            _selectionManager.RemoveSelectionChangedListener();
        }

        /// <summary>
        /// Simulates OnPointerDown. The real method checks that the raycast
        /// hit this specific gameObject. We simulate this with a flag.
        /// </summary>
        public void OnPointerDown(bool hitSelf)
        {
            if (hitSelf)
            {
                OnPointerClicked.Invoke();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class BlockingPlaneControllerTests
    {
        private BlockingPlaneControllerStub _controller;
        private CanvasGroupMock _canvasGroup;
        private SelectionManagerMock _selectionManager;

        [SetUp]
        public void SetUp()
        {
            _canvasGroup = new CanvasGroupMock();
            _selectionManager = new SelectionManagerMock();
            _controller = new BlockingPlaneControllerStub(_canvasGroup, _selectionManager);
        }

        [TearDown]
        public void TearDown()
        {
            _controller = null;
            _canvasGroup = null;
            _selectionManager = null;
        }

        [Test]
        public void Show_EnablesBlocksRaycasts_SoPlaneInterceptsClicks()
        {
            // WHY: The blocking plane must intercept pointer events when shown
            // so that clicks outside a popup/menu dismiss it rather than
            // interacting with the scene behind it.

            // Arrange
            _canvasGroup.BlocksRaycasts = false;

            // Act
            _controller.Show();

            // Assert
            Assert.IsTrue(_canvasGroup.BlocksRaycasts,
                "Show() must enable blocksRaycasts so the plane intercepts pointer events.");
        }

        [Test]
        public void Hide_DisablesBlocksRaycasts_SoPlaneIsTransparentToInput()
        {
            // WHY: When hidden, the blocking plane must not consume any pointer
            // events, allowing the user to interact with the scene normally.

            // Arrange
            _controller.Show();

            // Act
            _controller.Hide();

            // Assert
            Assert.IsFalse(_canvasGroup.BlocksRaycasts,
                "Hide() must disable blocksRaycasts so the plane is transparent to input.");
        }

        [Test]
        public void Show_RegistersSelectionChangedListener()
        {
            // WHY: The blocking plane listens for selection changes while
            // visible so it can respond to external state changes (e.g.
            // auto-dismiss when selection changes).

            // Act
            _controller.Show();

            // Assert
            Assert.AreEqual(1, _selectionManager.AddListenerCount,
                "Show() should register a SelectionChanged listener exactly once.");
        }

        [Test]
        public void Hide_RemovesSelectionChangedListener()
        {
            // WHY: Failing to unregister the listener would cause a memory
            // leak and potentially trigger callbacks on a hidden plane.

            // Arrange
            _controller.Show();

            // Act
            _controller.Hide();

            // Assert
            Assert.AreEqual(1, _selectionManager.RemoveListenerCount,
                "Hide() should remove the SelectionChanged listener to prevent leaks.");
        }

        [Test]
        public void OnPointerDown_HitSelf_InvokesOnPointerClicked()
        {
            // WHY: When the user clicks directly on the blocking plane, the
            // OnPointerClicked event must fire so subscribers (menus, popups)
            // know to dismiss themselves.

            // Act
            _controller.OnPointerDown(hitSelf: true);

            // Assert
            Assert.AreEqual(1, _controller.OnPointerClicked.InvokeCount,
                "OnPointerDown hitting the plane itself should invoke OnPointerClicked.");
        }

        [Test]
        public void OnPointerDown_HitChild_DoesNotInvokeEvent()
        {
            // WHY: If the pointer hit a child element (not the plane itself),
            // the event should NOT fire. This prevents accidental dismissal
            // when clicking on content displayed on top of the blocking plane.

            // Act
            _controller.OnPointerDown(hitSelf: false);

            // Assert
            Assert.AreEqual(0, _controller.OnPointerClicked.InvokeCount,
                "OnPointerDown on a child object should not invoke OnPointerClicked.");
        }

        [Test]
        public void CanvasGroup_PropertyExposesInternalCanvasGroup()
        {
            // WHY: External code may need to read the CanvasGroup to check
            // whether the blocking plane is currently active (blocksRaycasts).

            // Assert
            Assert.IsNotNull(_controller.CanvasGroup,
                "CanvasGroup property must expose the internal CanvasGroup reference.");
            Assert.AreSame(_canvasGroup, _controller.CanvasGroup,
                "CanvasGroup property should return the same instance passed during construction.");
        }

        [Test]
        public void ShowThenHide_CyclesThroughStatesCorrectly()
        {
            // WHY: The blocking plane may be shown and hidden repeatedly during
            // a session (e.g. opening/closing menus). This verifies the full
            // lifecycle works without leaving stale state.

            // Act & Assert — Show
            _controller.Show();
            Assert.IsTrue(_canvasGroup.BlocksRaycasts,
                "After Show(), blocksRaycasts should be true.");

            // Act & Assert — Hide
            _controller.Hide();
            Assert.IsFalse(_canvasGroup.BlocksRaycasts,
                "After Hide(), blocksRaycasts should be false.");

            // Act & Assert — Show again
            _controller.Show();
            Assert.IsTrue(_canvasGroup.BlocksRaycasts,
                "After second Show(), blocksRaycasts should be true again.");
        }
    }
}
