using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.Validation;

namespace Vareiko.Foundation.Tests.Validation
{
    public sealed class FoundationStartupValidationRulesTests
    {
        [Test]
        public void SaveSecurityRule_WithMissingConfig_ReturnsWarning()
        {
            SaveSecurityStartupValidationRule rule = new SaveSecurityStartupValidationRule();

            StartupValidationResult result = rule.Validate();

            Assert.That(result.Severity, Is.EqualTo(StartupValidationSeverity.Warning));
            Assert.That(result.Message, Does.Contain("SaveSecurityConfig"));
        }

        [Test]
        public void SaveSecurityRule_WithDefaultSecretAndEncryptionEnabled_ReturnsError()
        {
            SaveSecurityConfig config = ScriptableObject.CreateInstance<SaveSecurityConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_enableEncryption", true);
                ReflectionTestUtil.SetPrivateField(config, "_secretKey", "replace-with-project-secret");

                SaveSecurityStartupValidationRule rule = new SaveSecurityStartupValidationRule(config);
                StartupValidationResult result = rule.Validate();

                Assert.That(result.Severity, Is.EqualTo(StartupValidationSeverity.Error));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void BackendRule_PlayFabWithoutTitleId_ReturnsError()
        {
            BackendConfig config = ScriptableObject.CreateInstance<BackendConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_provider", BackendProviderType.PlayFab);
                ReflectionTestUtil.SetPrivateField(config, "_titleId", string.Empty);

                BackendStartupValidationRule rule = new BackendStartupValidationRule(config);
                StartupValidationResult result = rule.Validate();

                Assert.That(result.Severity, Is.EqualTo(StartupValidationSeverity.Error));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void BackendRule_WithNoneProvider_ReturnsWarning()
        {
            BackendStartupValidationRule rule = new BackendStartupValidationRule();
            StartupValidationResult result = rule.Validate();

            Assert.That(result.Severity, Is.EqualTo(StartupValidationSeverity.Warning));
        }

        [Test]
        public void ObservabilityRule_WithExceptionCaptureDisabled_ReturnsWarning()
        {
            ObservabilityConfig config = ScriptableObject.CreateInstance<ObservabilityConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_captureUnhandledExceptions", false);

                ObservabilityStartupValidationRule rule = new ObservabilityStartupValidationRule(config);
                StartupValidationResult result = rule.Validate();

                Assert.That(result.Severity, Is.EqualTo(StartupValidationSeverity.Warning));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
