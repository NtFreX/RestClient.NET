using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Flow
{
    public abstract class CachedFunctionBase : FunctionBaseDecorator
    {
        private readonly FunctionBaseDecorator _funcBase;

        private readonly object _lastResultsLock = new object();
        private readonly List<(DateTime DateTime, object[] Arguments, object Result)> _lastResults;

        public TimeSpan CachingTime { get; }
        public override event EventHandler<object> AfterExecution;
        public override event EventHandler<object[]> BeforeExecution;

        protected CachedFunctionBase(FunctionBaseDecorator funcBase, TimeSpan minAllowedInterval)
        {
            _funcBase = funcBase;
            _funcBase.AfterExecution += (sender, objects) => AfterExecution?.Invoke(sender, objects);
            _funcBase.BeforeExecution += (sender, objects) => BeforeExecution?.Invoke(sender, objects);

            _lastResults = new List<(DateTime DateTime, object[] Arguments, object Result)>();

            CachingTime = minAllowedInterval;
        }

        protected bool HasCachedInner(object[] arguments, out (DateTime DateTime, object[] Arguments, object Result)? lastResult)
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

        public override async Task<object> ExecuteInnerAsync(object[] arguments)
        {
            lock (_lastResultsLock)
            {
                var hasCached = HasCachedInner(arguments, out var cachedResult);

                if (hasCached && (CachingTime == TimeSpan.MaxValue ||
                                  cachedResult.Value.DateTime >= DateTime.Now - CachingTime))
                {
                    return cachedResult.Value.Result;
                }
                else if (hasCached)
                {
                    _lastResults.Remove(cachedResult.Value);
                }
            }
            var result = await _funcBase.ExecuteInnerAsync(arguments);
            lock (_lastResultsLock)
            {
                _lastResults.Add((DateTime.Now, arguments, result));
            }
            return result;
        }
    }

    public class CachedFunction<T> : CachedFunctionBase
    {
        public CachedFunction(Func<T> func, TimeSpan minAllowedInterval)
            : base(new Function<T>(func), minAllowedInterval) { }

        public CachedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval)
            : base(func, minAllowedInterval) { }

        public T Execute()
            => (T)ExecuteInnerAsync(null).GetAwaiter().GetResult();
        public bool HasCached()
            => HasCachedInner(null, out var _);
    }

    public class AsyncCachedFunction<T> : CachedFunctionBase
    {
        public AsyncCachedFunction(Func<Task<T>> func, TimeSpan minAllowedInterval)
            : base(new AsyncFunction<T>(func), minAllowedInterval) { }

        public AsyncCachedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval)
            : base(func, minAllowedInterval) { }

        public async Task<T> ExecuteAsync()
            => (T)await ExecuteInnerAsync(null);
        public bool HasCached()
            => HasCachedInner(null, out var _);
    }
    public class AsyncCachedFunction<TArg1, TResult> : CachedFunctionBase
    {
        public AsyncCachedFunction(Func<TArg1, Task<TResult>> func, TimeSpan minAllowedInterval)
            : base(new AsyncFunction<TArg1, TResult>(func), minAllowedInterval) { }

        public AsyncCachedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval)
            : base(func, minAllowedInterval) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1 });
        public bool HasCached(TArg1 arg1)
            => HasCachedInner(new object[] { arg1 }, out var _);
    }
    public class AsyncCachedFunction<TArg1, TArg2, TResult> : CachedFunctionBase
    {
        public AsyncCachedFunction(Func<TArg1, TArg2, Task<TResult>> func, TimeSpan minAllowedInterval)
            : base(new AsyncFunction<TArg1, TArg2, TResult>(func), minAllowedInterval) { }

        public AsyncCachedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval)
            : base(func, minAllowedInterval) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1, TArg2 arg2)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1, arg2 });
        public bool HasCached(TArg1 arg1, TArg2 arg2)
            => HasCachedInner(new object[] { arg1, arg2 }, out var _);
    }
}
