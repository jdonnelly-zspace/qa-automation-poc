// =============================================================================
// ZTargetManagerTests.cs - Edit Mode Unit Tests for ZTargetManager
// =============================================================================
// TARGET CLASS: ZTargetManager
//   Real file: Assets/zSpace/Core/Scripts/Sdk/ZTargetManager.cs
//
// WHAT IT TESTS:
//   Hardware target manager in the zSpace SDK that provides access to trackable
//   targets (head, left/right/center eye, stylus). Validates target enumeration
//   by type and index, convenience properties for head/eye/stylus targets,
//   target count queries, and behavior when no targets of a given type exist.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class/interface marked with the "TODO: DELETE this stub"
//      comment and replace the using directives with the real namespaces.
//   3. The real ZTargetManager calls into ZPlugin native methods and extends
//      ZNativeResourceCache<ZTarget>. These tests exercise the public API
//      through a lightweight POCO stub so they compile standalone without
//      the native zSpace runtime.
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
    /// Stand-in for ZTargetType from the zSpace SDK.
    /// </summary>
    public enum ZTargetType
    {
        Head = 0,
        Primary = 1,
        LeftEye = 2,
        RightEye = 3,
        CenterEye = 4
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Minimal stand-in for a ZTarget resource.
    /// </summary>
    public class ZTargetStub
    {
        public ZTargetType TargetType { get; private set; }
        public int TypeIndex { get; private set; }

        public ZTargetStub(ZTargetType targetType, int typeIndex = 0)
        {
            TargetType = targetType;
            TypeIndex = typeIndex;
        }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight POCO that mirrors the public API and convenience properties
    /// of the real ZTargetManager, without requiring ZPlugin or ZContext.
    /// </summary>
    public class ZTargetManagerStub
    {
        private List<ZTargetStub> _targets = new List<ZTargetStub>();

        public void AddTarget(ZTargetStub target)
        {
            _targets.Add(target);
        }

        /// <summary>
        /// The default head target (zSpace glasses).
        /// </summary>
        public ZTargetStub HeadTarget
        {
            get { return GetTarget(ZTargetType.Head); }
        }

        /// <summary>
        /// The default left eye target.
        /// </summary>
        public ZTargetStub LeftEyeTarget
        {
            get { return GetTarget(ZTargetType.LeftEye); }
        }

        /// <summary>
        /// The default right eye target.
        /// </summary>
        public ZTargetStub RightEyeTarget
        {
            get { return GetTarget(ZTargetType.RightEye); }
        }

        /// <summary>
        /// The default center eye target.
        /// </summary>
        public ZTargetStub CenterEyeTarget
        {
            get { return GetTarget(ZTargetType.CenterEye); }
        }

        /// <summary>
        /// The default stylus target (Primary type).
        /// </summary>
        public ZTargetStub StylusTarget
        {
            get { return GetTarget(ZTargetType.Primary); }
        }

        /// <summary>
        /// Gets the number of targets of the specified type.
        /// </summary>
        public int GetNumTargets(ZTargetType targetType)
        {
            int count = 0;
            for (int i = 0; i < _targets.Count; i++)
            {
                if (_targets[i].TargetType == targetType)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Gets a target of the specified type at the given type-relative index.
        /// </summary>
        public ZTargetStub GetTarget(ZTargetType targetType, int index = 0)
        {
            int seen = 0;
            for (int i = 0; i < _targets.Count; i++)
            {
                if (_targets[i].TargetType == targetType)
                {
                    if (seen == index)
                    {
                        return _targets[i];
                    }
                    seen++;
                }
            }
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class ZTargetManagerTests
    {
        private ZTargetManagerStub _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new ZTargetManagerStub();

            // Simulate a standard zSpace target configuration:
            // head, left eye, right eye, center eye, and one stylus
            _manager.AddTarget(new ZTargetStub(ZTargetType.Head));
            _manager.AddTarget(new ZTargetStub(ZTargetType.LeftEye));
            _manager.AddTarget(new ZTargetStub(ZTargetType.RightEye));
            _manager.AddTarget(new ZTargetStub(ZTargetType.CenterEye));
            _manager.AddTarget(new ZTargetStub(ZTargetType.Primary));
        }

        [TearDown]
        public void TearDown()
        {
            _manager = null;
        }

        [Test]
        public void HeadTarget_ReturnsHeadType_WhenHeadTargetExists()
        {
            // WHY: The HeadTarget property is the primary entry point for head
            //      tracking. If it returns the wrong target type, stereoscopic
            //      rendering will use incorrect pose data.

            // Act
            ZTargetStub target = _manager.HeadTarget;

            // Assert
            Assert.IsNotNull(target,
                "HeadTarget should return a non-null target when a Head target exists.");
            Assert.AreEqual(ZTargetType.Head, target.TargetType,
                "HeadTarget should return a target of type Head.");
        }

        [Test]
        public void StylusTarget_ReturnsPrimaryType_WhenStylusExists()
        {
            // WHY: The StylusTarget convenience property maps to ZTargetType.Primary.
            //      Stylus input drives all 3D interaction (select, rotate, dissect),
            //      so returning the correct target is critical.

            // Act
            ZTargetStub target = _manager.StylusTarget;

            // Assert
            Assert.IsNotNull(target,
                "StylusTarget should return a non-null target when a Primary target exists.");
            Assert.AreEqual(ZTargetType.Primary, target.TargetType,
                "StylusTarget should return a target of type Primary.");
        }

        [Test]
        public void EyeTargets_ReturnCorrectTypes_ForStereoRendering()
        {
            // WHY: Left, right, and center eye targets drive stereo camera offsets.
            //      Swapping left and right would invert the 3D depth effect,
            //      causing visual discomfort.

            // Act
            ZTargetStub left = _manager.LeftEyeTarget;
            ZTargetStub right = _manager.RightEyeTarget;
            ZTargetStub center = _manager.CenterEyeTarget;

            // Assert
            Assert.IsNotNull(left,
                "LeftEyeTarget should return a non-null target.");
            Assert.AreEqual(ZTargetType.LeftEye, left.TargetType,
                "LeftEyeTarget should return a target of type LeftEye.");

            Assert.IsNotNull(right,
                "RightEyeTarget should return a non-null target.");
            Assert.AreEqual(ZTargetType.RightEye, right.TargetType,
                "RightEyeTarget should return a target of type RightEye.");

            Assert.IsNotNull(center,
                "CenterEyeTarget should return a non-null target.");
            Assert.AreEqual(ZTargetType.CenterEye, center.TargetType,
                "CenterEyeTarget should return a target of type CenterEye.");
        }

        [Test]
        public void GetNumTargets_ReturnsCorrectCount_ByType()
        {
            // WHY: Knowing the number of targets of each type lets the app
            //      validate hardware configuration at startup and warn users
            //      if expected devices are missing.

            // Assert
            Assert.AreEqual(1, _manager.GetNumTargets(ZTargetType.Head),
                "Should have exactly 1 Head target.");
            Assert.AreEqual(1, _manager.GetNumTargets(ZTargetType.Primary),
                "Should have exactly 1 Primary (stylus) target.");
            Assert.AreEqual(1, _manager.GetNumTargets(ZTargetType.LeftEye),
                "Should have exactly 1 LeftEye target.");
        }

        [Test]
        public void GetTarget_ReturnsNull_WhenNoTargetOfTypeExists()
        {
            // WHY: On non-zSpace hardware or when a device is disconnected,
            //      no target of a given type may exist. The manager must return
            //      null so callers can degrade gracefully instead of crashing.

            // Arrange — create a manager with no targets
            var emptyManager = new ZTargetManagerStub();

            // Act
            ZTargetStub target = emptyManager.GetTarget(ZTargetType.Head);

            // Assert
            Assert.IsNull(target,
                "GetTarget() should return null when no target of the requested type exists.");
        }

        [Test]
        public void GetTarget_ReturnsCorrectTarget_WhenMultipleOfSameType()
        {
            // WHY: Although uncommon, the SDK supports multiple targets of the
            //      same type (e.g., two styli). The index parameter must select
            //      the correct one.

            // Arrange — add a second Primary (stylus) target
            _manager.AddTarget(new ZTargetStub(ZTargetType.Primary, 1));

            // Act
            ZTargetStub first = _manager.GetTarget(ZTargetType.Primary, 0);
            ZTargetStub second = _manager.GetTarget(ZTargetType.Primary, 1);

            // Assert
            Assert.IsNotNull(first,
                "First Primary target should exist at index 0.");
            Assert.IsNotNull(second,
                "Second Primary target should exist at index 1.");
            Assert.AreEqual(0, first.TypeIndex,
                "First target should have TypeIndex 0.");
            Assert.AreEqual(1, second.TypeIndex,
                "Second target should have TypeIndex 1.");
        }

        [Test]
        public void GetNumTargets_ReturnsZero_ForAbsentType()
        {
            // WHY: Querying a type that has no registered targets must return 0
            //      so the app can skip initialization of that input subsystem.

            // Arrange — create a manager with only a head target
            var partialManager = new ZTargetManagerStub();
            partialManager.AddTarget(new ZTargetStub(ZTargetType.Head));

            // Act & Assert
            Assert.AreEqual(0, partialManager.GetNumTargets(ZTargetType.Primary),
                "GetNumTargets(Primary) should return 0 when no stylus target exists.");
            Assert.AreEqual(0, partialManager.GetNumTargets(ZTargetType.LeftEye),
                "GetNumTargets(LeftEye) should return 0 when no left eye target exists.");
        }

        [Test]
        public void ConvenienceProperties_ReturnNull_WhenManagerIsEmpty()
        {
            // WHY: When no hardware is connected, all convenience properties must
            //      return null. This is the first thing the app checks at startup
            //      to decide whether to enable zSpace-specific features.

            // Arrange
            var emptyManager = new ZTargetManagerStub();

            // Assert
            Assert.IsNull(emptyManager.HeadTarget,
                "HeadTarget should be null when no targets are registered.");
            Assert.IsNull(emptyManager.StylusTarget,
                "StylusTarget should be null when no targets are registered.");
            Assert.IsNull(emptyManager.LeftEyeTarget,
                "LeftEyeTarget should be null when no targets are registered.");
            Assert.IsNull(emptyManager.RightEyeTarget,
                "RightEyeTarget should be null when no targets are registered.");
            Assert.IsNull(emptyManager.CenterEyeTarget,
                "CenterEyeTarget should be null when no targets are registered.");
        }
    }
}
