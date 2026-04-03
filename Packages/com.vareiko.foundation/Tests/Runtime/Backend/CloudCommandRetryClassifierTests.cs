using NUnit.Framework;
using Vareiko.Foundation.Backend;

namespace Vareiko.Foundation.Tests.Backend
{
    public sealed class CloudCommandRetryClassifierTests
    {
        [Test]
        public void Classify_TimeoutMessage_IsRetryable()
        {
            CloudCommandRetryClassifier classifier = new CloudCommandRetryClassifier();

            CloudCommandFailureClassification result = classifier.Classify(string.Empty, "request timeout");

            Assert.That(result.Kind, Is.EqualTo(CloudCommandFailureKind.Retryable));
        }

        [Test]
        public void Classify_AuthError_IsNonRetryable()
        {
            CloudCommandRetryClassifier classifier = new CloudCommandRetryClassifier();

            CloudCommandFailureClassification result = classifier.Classify("Auth.Required", "authentication required");

            Assert.That(result.Kind, Is.EqualTo(CloudCommandFailureKind.NonRetryable));
        }

        [Test]
        public void Classify_DuplicateError_IsSuccessLike()
        {
            CloudCommandRetryClassifier classifier = new CloudCommandRetryClassifier();

            CloudCommandFailureClassification result = classifier.Classify("Duplicate.Request", "already processed");

            Assert.That(result.Kind, Is.EqualTo(CloudCommandFailureKind.SuccessLike));
        }
    }
}
