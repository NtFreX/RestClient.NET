using System;
using System.Threading;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET
{
    public abstract class RateLimitedFunctionBase<T>
    {
        private readonly Func<object[], Task<T>> _func;
        private readonly Func<object[], Task<bool>> _doNotRateLimitWhen;
        private readonly SemaphoreSlim _semaphoreSlim;

        private DateTime _lastExecution;

        public TimeSpan MaxAllowedInterval { get; }

        protected RateLimitedFunctionBase(Func<object[], Task<T>> func, TimeSpan minAllowedInterval, Func<object[], Task<bool>> doNotRateLimitWhen)
        {
            _func = func;
            MaxAllowedInterval = minAllowedInterval;
            _doNotRateLimitWhen = doNotRateLimitWhen;

            _lastExecution = DateTime.MinValue;
            _semaphoreSlim = new SemaphoreSlim(1);
        }

        protected async Task<TimeSpan> IsRateLimitedAsync(object[] arguments)
        {
            if (await _doNotRateLimitWhen(arguments))
                return TimeSpan.Zero;

            return _lastExecution + MaxAllowedInterval - DateTime.Now;
        }
        protected async Task<T> ExecuteInnerAsync(object[] arguments)
        {
            try
            {
                _semaphoreSlim.Wait();

                TimeSpan rateLimtDelay;
                while ((rateLimtDelay = await IsRateLimitedAsync(arguments)).Ticks > 0)
                {
                    await Task.Delay(rateLimtDelay);
                }

                var result = await _func(arguments);
                _lastExecution = DateTime.Now;
                return result;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }

    public class RateLimitedFunction<T> : RateLimitedFunctionBase<T>
    {
        public RateLimitedFunction(Func<T> func, TimeSpan minAllowedInterval)
            : this(func, minAllowedInterval, null) { }

        public RateLimitedFunction(Func<T> func, TimeSpan minAllowedInterval, Func<bool> doNotRateLimitWhen)
            : base(async args => await Task.FromResult(func()), minAllowedInterval, async args => await Task.FromResult(doNotRateLimitWhen())) { }

        public T Execute()
            => ExecuteInnerAsync(null).GetAwaiter().GetResult();
        public TimeSpan IsRateLimited()
            => IsRateLimitedAsync(null).GetAwaiter().GetResult();
    }

    public class AsyncRateLimitedFunction : RateLimitedFunctionBase<object>
    {
        public AsyncRateLimitedFunction(Func<Task> func, TimeSpan minAllowedInterval)
            : this(func, minAllowedInterval, null) { }

        public AsyncRateLimitedFunction(Func<Task> func, TimeSpan minAllowedInterval, Func<Task<bool>> doNotRateLimitWhen)
            : base(GetFunction(func), minAllowedInterval, async args => await doNotRateLimitWhen()) { }

        private static Func<object[], Task<object>> GetFunction(Func<Task> func)
        {
            return async args =>
            {
                await func();
                return null;
            };
        }

        public async Task ExecuteAsync()
            => await ExecuteInnerAsync(null);
        public async Task<TimeSpan> IsRateLimitedAsync()
            => await IsRateLimitedAsync(null);
    }

    public class AsyncRateLimitedFunction<T> : RateLimitedFunctionBase<T>
    {
        public AsyncRateLimitedFunction(Func<Task<T>> func, TimeSpan minAllowedInterval)
            : this(func, minAllowedInterval, null) { }

        public AsyncRateLimitedFunction(Func<Task<T>> func, TimeSpan minAllowedInterval, Func<Task<bool>> doNotRateLimitWhen)
            : base(async args => await func(), minAllowedInterval, async args => await doNotRateLimitWhen()) { }

        public async Task<T> ExecuteAsync()
            => await ExecuteInnerAsync(null);
        public async Task<TimeSpan> IsRateLimitedAsync()
            => await IsRateLimitedAsync(null);
    }

    public class AsyncRateLimitedFunction<TArg1, TResult> : RateLimitedFunctionBase<TResult>
    {
        public AsyncRateLimitedFunction(Func<TArg1, Task<TResult>> func, TimeSpan minAllowedInterval)
            : this(func, minAllowedInterval, null) { }

        public AsyncRateLimitedFunction(Func<TArg1, Task<TResult>> func, TimeSpan minAllowedInterval, Func<TArg1, Task<bool>> doNotRateLimitWhen)
            : base(async args => await func((TArg1)args[0]), minAllowedInterval, async args => await doNotRateLimitWhen((TArg1)args[0])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1)
            => await ExecuteInnerAsync(new object[] { arg1 });
        public async Task<TimeSpan> IsRateLimitedAsync(TArg1 arg1)
            => await IsRateLimitedAsync(new object[] { arg1 });
    }

    public class AsyncRateLimitedFunction<TArg1, TArg2, TResult> : RateLimitedFunctionBase<TResult>
    {
        public AsyncRateLimitedFunction(Func<TArg1, TArg2, Task<TResult>> func, TimeSpan minAllowedInterval)
            : this(func, minAllowedInterval, null) { }

        public AsyncRateLimitedFunction(Func<TArg1, TArg2, Task<TResult>> func, TimeSpan minAllowedInterval, Func<TArg1, TArg2, Task<bool>> doNotRateLimitWhen)
            : base(async args => await func((TArg1)args[0], (TArg2)args[1]), minAllowedInterval, async args => await doNotRateLimitWhen((TArg1)args[0], (TArg2)args[1])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1, TArg2 arg2)
            => await ExecuteInnerAsync(new object[] { arg1, arg2 });
        public async Task<TimeSpan> IsRateLimitedAsync(TArg1 arg1, TArg2 arg2)
            => await IsRateLimitedAsync(new object[] { arg1, arg2 });
    }

    public class AsyncRateLimitedFunction<TArg1, TArg2, TArg3, TResult> : RateLimitedFunctionBase<TResult>
    {
        public AsyncRateLimitedFunction(Func<TArg1, TArg2, TArg3, Task<TResult>> func, TimeSpan minAllowedInterval)
            : this(func, minAllowedInterval, null) { }

        public AsyncRateLimitedFunction(Func<TArg1, TArg2, TArg3, Task<TResult>> func, TimeSpan minAllowedInterval, Func<TArg1, TArg2, TArg3, Task<bool>> doNotRateLimitWhen)
            : base(async args => await func((TArg1)args[0], (TArg2)args[1], (TArg3)args[2]), minAllowedInterval, async args => await doNotRateLimitWhen((TArg1)args[0], (TArg2)args[1], (TArg3)args[2])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => await ExecuteInnerAsync(new object[] { arg1, arg2, arg3 });
        public async Task<TimeSpan> IsRateLimitedAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => await IsRateLimitedAsync(new object[] { arg1, arg2, arg3 });
    }
}
