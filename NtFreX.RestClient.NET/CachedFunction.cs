using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET
{
    public abstract class CachedFunctionBase<T>
    {
        private readonly Func<object[], Task<T>> _func;
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly List<(DateTime DateTime, object[] Arguments, T Result)> _lastResults;

        public TimeSpan CachingTime { get; }

        protected CachedFunctionBase(Func<object[], Task<T>> func, TimeSpan minAllowedInterval)
        {
            _func = func;
            CachingTime = minAllowedInterval;

            _lastResults = new List<(DateTime DateTime, object[] Arguments, T Result)>();
            _semaphoreSlim = new SemaphoreSlim(1);
        }

        protected bool HasCachedInner(object[] arguments, out (DateTime DateTime, object[] Arguments, T Result)? lastResult)
        {
            lastResult = null;
            if (arguments == null)
            {
                if (_lastResults.Any())
                {
                    lastResult = _lastResults.First();
                }
            }
            else
            {
                lastResult = _lastResults.FirstOrDefault(x =>
                {
                    for (int i = 0; i < x.Arguments.Length; i++)
                    {
                        if (!x.Arguments[i].Equals(arguments[i]))
                            return false;
                    }
                    return true;
                });
            }

            return lastResult != null && lastResult.Value.DateTime != DateTime.MinValue;
        }

        protected async Task<T> ExecuteInnerAsync(object[] arguments)
        {
            try
            {
                _semaphoreSlim.Wait();

                var hasCached = HasCachedInner(arguments, out var cachedResult);

                if (hasCached && (CachingTime == TimeSpan.MaxValue || cachedResult.Value.DateTime >= DateTime.Now - CachingTime))
                {
                    return cachedResult.Value.Result;
                }
                else if (hasCached)
                {
                    _lastResults.Remove(cachedResult.Value);
                }

                var result = await _func(arguments);
                _lastResults.Add((DateTime.Now, arguments, result));

                return result;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }

    public class CachedFunction<T> : CachedFunctionBase<T>
    {
        public CachedFunction(Func<T> func, TimeSpan minAllowedInterval)
            : base(async args => await Task.FromResult(func()), minAllowedInterval)
        { }

        public T Execute()
            => ExecuteInnerAsync(null).GetAwaiter().GetResult();

        public bool HasCached()
            => HasCachedInner(null, out var _);
    }

    public class AsyncCachedFunction<T> : CachedFunctionBase<T>
    {
        public AsyncCachedFunction(Func<Task<T>> func, TimeSpan minAllowedInterval)
            : base(async args => await func(), minAllowedInterval) { }

        public async Task<T> ExecuteAsync()
            => await ExecuteInnerAsync(null);

        public bool HasCached()
            => HasCachedInner(null, out var _);
    }

    public class AsyncCachedFunction<TArg1, TResult> : CachedFunctionBase<TResult>
    {
        public AsyncCachedFunction(Func<TArg1, Task<TResult>> func, TimeSpan minAllowedInterval)
            : base(async args => await func((TArg1)args[0]), minAllowedInterval) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1)
            => await ExecuteInnerAsync(new object[] { arg1 });

        public bool HasCached(TArg1 arg1)
            => HasCachedInner(new object[] { arg1 }, out var _);
    }

    public class AsyncCachedFunction<TArg1, TArg2, TResult> : CachedFunctionBase<TResult>
    {
        public AsyncCachedFunction(Func<TArg1, TArg2, Task<TResult>> func, TimeSpan minAllowedInterval)
            : base(async args => await func((TArg1)args[0], (TArg2)args[1]), minAllowedInterval) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1, TArg2 arg2)
            => await ExecuteInnerAsync(new object[] { arg1, arg2 });

        public bool HasCached(TArg1 arg1, TArg2 arg2)
            => HasCachedInner(new object[] { arg1, arg2 }, out var _);
    }
}
