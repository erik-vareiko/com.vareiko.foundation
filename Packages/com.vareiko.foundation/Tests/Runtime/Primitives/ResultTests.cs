using NUnit.Framework;

namespace Vareiko.Foundation.Tests.Primitives
{
    public sealed class ResultTests
    {
        [Test]
        public void Success_HasNoError()
        {
            Result result = Result.Success();
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
            Assert.That(result.Error, Is.Empty);
        }

        [Test]
        public void Fail_CarriesError_AndEmptyErrorGetsPlaceholder()
        {
            Assert.That(Result.Fail("boom").Error, Is.EqualTo("boom"));
            Assert.That(Result.Fail("boom").IsFailure, Is.True);
            Assert.That(Result.Fail(null).Error, Is.Not.Empty);
        }

        [Test]
        public void Default_IsFailure()
        {
            Result result = default;
            Assert.That(result.IsSuccess, Is.False, "default(Result) must not read as success.");
        }

        [Test]
        public void GenericSuccess_ExposesValue()
        {
            Result<int> result = Result<int>.Success(42);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(result.TryGetValue(out int value), Is.True);
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void GenericFail_HasDefaultValue_AndFallback()
        {
            Result<string> result = Result<string>.Fail("nope");
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.TryGetValue(out string value), Is.False);
            Assert.That(value, Is.Null);
            Assert.That(result.GetValueOrDefault("fallback"), Is.EqualTo("fallback"));
        }

        [Test]
        public void AsResult_KeepsState()
        {
            Assert.That(Result<int>.Success(1).AsResult().IsSuccess, Is.True);
            Result downgraded = Result<int>.Fail("err").AsResult();
            Assert.That(downgraded.IsFailure, Is.True);
            Assert.That(downgraded.Error, Is.EqualTo("err"));
        }
    }
}
