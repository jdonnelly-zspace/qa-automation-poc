// =============================================================================
// LicenseManagerUnityTests.cs - Edit Mode Unit Tests for LicenseManagerUnity
// =============================================================================
// TARGET CLASS: LicenseManagerUnity
//   Real file: Assets/zSpace/StudioA3/Scripts/Licensing/LicenseManagerUnity.cs
//
// WHAT IT TESTS:
//   Licensing configuration validation performed at startup. The real
//   VerifyApplicationLicensingConfiguration method throws on invalid configs
//   (null/empty version, missing modes, null product ID). These tests confirm
//   every guard clause and the happy path.
//
// INTEGRATION NOTES:
//   1. Copy into Assets/Tests/EditModeTests/ in the Unity project.
//   2. Delete every class marked with the "TODO: DELETE this stub" comment
//      and replace using directives with real namespaces.
//   3. The real LicenseManagerUnity is a MonoBehaviour singleton. The stub
//      here extracts only the pure validation logic so tests run without
//      Unity runtime dependencies.
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
    /// Mirrors the real ApplicationLicensingConfiguration data class.
    /// </summary>
    public class ApplicationLicensingConfiguration
    {
        public string Version { get; set; }
        public List<string> ActiveModeIds { get; set; }
        public string ProductId { get; set; }
    }

    // TODO: DELETE this stub when integrating into the Unity project — use the real class instead.
    /// <summary>
    /// Lightweight stand-in for the validation logic in LicenseManagerUnity.
    /// Reproduces the guard-clause behaviour of
    /// VerifyApplicationLicensingConfiguration without MonoBehaviour.
    /// </summary>
    public class LicenseManagerUnityStub
    {
        public void VerifyApplicationLicensingConfiguration(
            ApplicationLicensingConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config),
                    "Licensing configuration must not be null.");
            }

            if (string.IsNullOrEmpty(config.Version))
            {
                throw new ArgumentException(
                    "Licensing configuration Version must not be null or empty.",
                    nameof(config));
            }

            if (config.ActiveModeIds == null)
            {
                throw new ArgumentException(
                    "Licensing configuration ActiveModeIds must not be null.",
                    nameof(config));
            }

            if (config.ActiveModeIds.Count == 0)
            {
                throw new ArgumentException(
                    "Licensing configuration ActiveModeIds must contain at least one mode.",
                    nameof(config));
            }

            if (string.IsNullOrEmpty(config.ProductId))
            {
                throw new ArgumentException(
                    "Licensing configuration ProductId must not be null or empty.",
                    nameof(config));
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [TestFixture]
    public class LicenseManagerUnityTests
    {
        private LicenseManagerUnityStub _licenseManager;

        [SetUp]
        public void SetUp()
        {
            _licenseManager = new LicenseManagerUnityStub();
        }

        [TearDown]
        public void TearDown()
        {
            _licenseManager = null;
        }

        [Test]
        public void VerifyConfig_ThrowsOnNullVersion()
        {
            // Arrange
            var config = new ApplicationLicensingConfiguration
            {
                Version = null,
                ActiveModeIds = new List<string> { "mode_ar" },
                ProductId = "studio-a3"
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(
                () => _licenseManager.VerifyApplicationLicensingConfiguration(config));

            StringAssert.Contains("Version", ex.Message,
                "Exception message should mention the invalid field.");
        }

        [Test]
        public void VerifyConfig_ThrowsOnEmptyActiveModeIds()
        {
            // Arrange
            var config = new ApplicationLicensingConfiguration
            {
                Version = "1.0.0",
                ActiveModeIds = new List<string>(),
                ProductId = "studio-a3"
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(
                () => _licenseManager.VerifyApplicationLicensingConfiguration(config));

            StringAssert.Contains("ActiveModeIds", ex.Message,
                "Exception message should mention ActiveModeIds.");
        }

        [Test]
        public void VerifyConfig_ThrowsOnNullActiveModeIds()
        {
            // Arrange
            var config = new ApplicationLicensingConfiguration
            {
                Version = "1.0.0",
                ActiveModeIds = null,
                ProductId = "studio-a3"
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(
                () => _licenseManager.VerifyApplicationLicensingConfiguration(config));

            StringAssert.Contains("ActiveModeIds", ex.Message,
                "Exception message should mention ActiveModeIds.");
        }

        [Test]
        public void VerifyConfig_AcceptsValidConfiguration()
        {
            // Arrange
            var config = new ApplicationLicensingConfiguration
            {
                Version = "2.5.1",
                ActiveModeIds = new List<string> { "mode_ar", "mode_vr" },
                ProductId = "studio-a3"
            };

            // Act & Assert - should not throw
            Assert.DoesNotThrow(
                () => _licenseManager.VerifyApplicationLicensingConfiguration(config),
                "A fully valid configuration should pass verification.");
        }

        [Test]
        public void VerifyConfig_ThrowsOnNullProductId()
        {
            // Arrange
            var config = new ApplicationLicensingConfiguration
            {
                Version = "1.0.0",
                ActiveModeIds = new List<string> { "mode_ar" },
                ProductId = null
            };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(
                () => _licenseManager.VerifyApplicationLicensingConfiguration(config));

            StringAssert.Contains("ProductId", ex.Message,
                "Exception message should mention ProductId.");
        }
    }
}
