using System;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET
{
    public class AsyncRateLimitedCachedFunction<T>
    {
        private readonly AsyncCachedFunction<T> _cachedFunction;
        private readonly AsyncRateLimitedFunction<T> _rateLimitedFunction;

        public TimeSpan MaxInterval => _rateLimitedFunction.MaxAllowedInterval;
        public TimeSpan CachingTime => _cachedFunction.CachingTime;

        public AsyncRateLimitedCachedFunction(TimeSpan maxInterval, TimeSpan cachingTime, Func<Task<T>> func, Func<Task<bool>> ignoreRateLimitFunc)
        {
            _cachedFunction = new AsyncCachedFunction<T>(func, cachingTime);
            _rateLimitedFunction = new AsyncRateLimitedFunction<T>(_cachedFunction.ExecuteAsync, maxInterval, ignoreRateLimitFunc);
        }

        public async Task<T> ExecuteAsync()
            => await _rateLimitedFunction.ExecuteAsync();
        public async Task<TimeSpan> IsRateLimitedAsync()
            => await _rateLimitedFunction.IsRateLimitedAsync();
        public bool IsCached()
            => _cachedFunction.HasCached();
    }

    public class AsyncRateLimitedCachedFunction<TArg1, TResult>
    {
        private readonly AsyncCachedFunction<TArg1, TResult> _cachedFunction;
        private readonly AsyncRateLimitedFunction<TArg1, TResult> _rateLimitedFunction;

        public TimeSpan MaxInterval => _rateLimitedFunction.MaxAllowedInterval;
        public TimeSpan CachingTime => _cachedFunction.CachingTime;

        public AsyncRateLimitedCachedFunction(TimeSpan maxInterval, TimeSpan cachingTime, Func<TArg1, Task<TResult>> func, Func<TArg1, Task<bool>> ignoreRateLimitFunc)
        {
            _cachedFunction = new AsyncCachedFunction<TArg1, TResult>(func, cachingTime);
            _rateLimitedFunction = new AsyncRateLimitedFunction<TArg1, TResult>(_cachedFunction.ExecuteAsync, maxInterval, ignoreRateLimitFunc);
        }

        public async Task<TResult> ExecuteAsync(TArg1 arg1)
            => await _rateLimitedFunction.ExecuteAsync(arg1);
        public async Task<TimeSpan> IsRateLimitedAsync(TArg1 arg1)
            => await _rateLimitedFunction.IsRateLimitedAsync(arg1);
        public bool IsCached(TArg1 arg1)
            => _cachedFunction.HasCached(arg1);
    }
}
