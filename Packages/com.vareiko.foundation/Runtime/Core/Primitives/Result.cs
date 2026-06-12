using System;

namespace Vareiko.Foundation
{
    /// <summary>
    /// Canonical success/failure primitive for foundation APIs. Domain results that carry more
    /// than a value + error message (codes, retry flags) keep their own shapes; everything else
    /// should use <see cref="Result"/> / <see cref="Result{T}"/> instead of inventing a new one.
    /// </summary>
    public readonly struct Result : IEquatable<Result>
    {
        private readonly bool _isSuccess;
        private readonly string _error;

        private Result(bool isSuccess, string error)
        {
            _isSuccess = isSuccess;
            _error = error ?? string.Empty;
        }

        public bool IsSuccess => _isSuccess;
        public bool IsFailure => !_isSuccess;
        // Null-coalesced so default(Result) — a failure — still exposes a non-null error.
        public string Error => _error ?? string.Empty;

        public static Result Success()
        {
            return new Result(true, string.Empty);
        }

        public static Result Fail(string error)
        {
            return new Result(false, string.IsNullOrEmpty(error) ? "Unknown error." : error);
        }

        public bool Equals(Result other)
        {
            return _isSuccess == other._isSuccess && string.Equals(Error, other.Error, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is Result other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _isSuccess ? 1 : Error.GetHashCode();
        }

        public override string ToString()
        {
            return _isSuccess ? "Success" : $"Failure({Error})";
        }
    }

    public readonly struct Result<T>
    {
        private readonly bool _isSuccess;
        private readonly T _value;
        private readonly string _error;

        private Result(bool isSuccess, T value, string error)
        {
            _isSuccess = isSuccess;
            _value = value;
            _error = error ?? string.Empty;
        }

        public bool IsSuccess => _isSuccess;
        public bool IsFailure => !_isSuccess;
        // Null-coalesced so default(Result<T>) — a failure — still exposes a non-null error.
        public string Error => _error ?? string.Empty;

        /// <summary>The value on success; <c>default</c> on failure.</summary>
        public T Value => _value;

        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, string.Empty);
        }

        public static Result<T> Fail(string error)
        {
            return new Result<T>(false, default, string.IsNullOrEmpty(error) ? "Unknown error." : error);
        }

        public bool TryGetValue(out T value)
        {
            value = _value;
            return _isSuccess;
        }

        public T GetValueOrDefault(T fallback)
        {
            return _isSuccess ? _value : fallback;
        }

        /// <summary>Drops the value, keeping the success/error state.</summary>
        public Result AsResult()
        {
            return _isSuccess ? Result.Success() : Result.Fail(Error);
        }

        public override string ToString()
        {
            return _isSuccess ? $"Success({_value})" : $"Failure({Error})";
        }
    }
}
