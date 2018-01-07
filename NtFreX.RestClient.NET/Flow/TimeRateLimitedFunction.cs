using System;
using System.Threading;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Flow
{
    public abstract class TimeRateLimitedFunctionBase : RateLimitedFunctionBase
    {
        public TimeSpan MinInterval { get; }

        private DateTime _lastExecution;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        protected TimeRateLimitedFunctionBase(FunctionBaseDecorator func, TimeSpan minInterval)
            : this(func, minInterval, args => Task.FromResult(false)) { }

        protected TimeRateLimitedFunctionBase(FunctionBaseDecorator func, TimeSpan minInterval, Func<object[], Task<bool>> ignoreRateLimitFunc)
            : base(func, ignoreRateLimitFunc)
        {
            MinInterval = minInterval;
            _lastExecution = DateTime.MinValue;

            func.BeforeExecution += BeforeExecutionHandler;
        }

        private void BeforeExecutionHandler(object sender, object[] objects)
        {
            _lastExecution = DateTime.Now;
            _semaphoreSlim.Release();
        }

        protected override async Task<TimeSpan> CalculateTimeToNextExecutionAsync(object[] arguments)
        {
            await _semaphoreSlim.WaitAsync();
            var difference = DateTime.Now - _lastExecution;
            if (difference > MinInterval)
            {
                return TimeSpan.Zero;
            }
            _semaphoreSlim.Release();
            return MinInterval - difference;
        }
    }

    public class TimeRateLimitedFunction : TimeRateLimitedFunctionBase
    {
        public TimeRateLimitedFunction(Action func, TimeSpan minAllowedInterval)
            : base(new Function(func), minAllowedInterval) { }

        public TimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval)
            : base(func, minAllowedInterval) { }

        public TimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval, Func<bool> doNotRateLimitWhen)
            : base(func, minAllowedInterval, async args => await Task.FromResult(doNotRateLimitWhen())) { }

        public void Execute()
            => ExecuteInnerAsync(null).GetAwaiter().GetResult();
        public TimeSpan GetTimeToNextExecution()
            => GetTimeToNextExecutionAsync(null).GetAwaiter().GetResult();
    }
    public class TimeRateLimitedFunction<T> : TimeRateLimitedFunctionBase
    {
        public TimeRateLimitedFunction(Func<T> func, TimeSpan minAllowedInterval)
            : base(new Function<T>(func), minAllowedInterval) { }

        public TimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval)
            : base(func, minAllowedInterval) { }

        public TimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval, Func<bool> doNotRateLimitWhen)
            : base(func, minAllowedInterval, async args => await Task.FromResult(doNotRateLimitWhen())) { }

        public T Execute()
            => (T) ExecuteInnerAsync(null).GetAwaiter().GetResult();
        public TimeSpan GetTimeToNextExecution()
            => GetTimeToNextExecutionAsync(null).GetAwaiter().GetResult();
    }

    public class AsyncTimeRateLimitedFunction : TimeRateLimitedFunctionBase
    {
        public AsyncTimeRateLimitedFunction(Func<Task> func, TimeSpan minAllowedInterval)
            : base(new AsyncFunction(func), minAllowedInterval) { }

        public AsyncTimeRateLimitedFunction(Func<Task> func, TimeSpan minAllowedInterval, Func<Task<bool>> doNotRateLimitWhen)
            : this(new AsyncFunction(func), minAllowedInterval, doNotRateLimitWhen) { }

        public AsyncTimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval)
            : base(func, minAllowedInterval) { }

        public AsyncTimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval, Func<Task<bool>> doNotRateLimitWhen)
            : base(func, minAllowedInterval, async args => await doNotRateLimitWhen()) { }

        public async Task ExecuteAsync()
            => await ExecuteInnerAsync(null);
        public async Task<TimeSpan> GetTimeToNextExecutionAsync()
            => await GetTimeToNextExecutionAsync(null);
    }
    public class AsyncTimeRateLimitedFunction<T> : TimeRateLimitedFunctionBase
    {
        public AsyncTimeRateLimitedFunction(Func<Task<T>> func, TimeSpan minAllowedInterval)
            : base(new AsyncFunction<T>(func), minAllowedInterval) { }

        public AsyncTimeRateLimitedFunction(Func<Task<T>> func, TimeSpan minAllowedInterval, Func<Task<bool>> doNotRateLimitWhen)
            : this(new AsyncFunction<T>(func), minAllowedInterval, doNotRateLimitWhen) { }

        public AsyncTimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval)
            : base(func, minAllowedInterval) { }

        public AsyncTimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval, Func<Task<bool>> doNotRateLimitWhen)
            : base(func, minAllowedInterval, async args => await doNotRateLimitWhen()) { }

        public async Task<T> ExecuteAsync()
            => (T)await ExecuteInnerAsync(null);
        public async Task<TimeSpan> GetTimeToNextExecutionAsync()
            => await GetTimeToNextExecutionAsync(null);
    }
    public class AsyncTimeRateLimitedFunction<TArg1, TResult> : TimeRateLimitedFunctionBase
    {
        public AsyncTimeRateLimitedFunction(Func<TArg1, Task<TResult>> func, TimeSpan minAllowedInterval)
            : base(new AsyncFunction<TArg1, TResult>(func), minAllowedInterval) { }

        public AsyncTimeRateLimitedFunction(Func<TArg1, Task<TResult>> func, TimeSpan minAllowedInterval, Func<TArg1, Task<bool>> doNotRateLimitWhen)
            : this(new AsyncFunction<TArg1, TResult>(func), minAllowedInterval, doNotRateLimitWhen) { }
        
        public AsyncTimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval)
            : base(func, minAllowedInterval) { }

        public AsyncTimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval, Func<TArg1, Task<bool>> doNotRateLimitWhen)
            : base(func, minAllowedInterval, async args => await doNotRateLimitWhen((TArg1)args[0])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1 });
        public async Task<TimeSpan> GetTimeToNextExecutionAsync(TArg1 arg1)
            => await GetTimeToNextExecutionAsync(new object[] { arg1 });
    }
    public class AsyncTimeRateLimitedFunction<TArg1, TArg2, TResult> : TimeRateLimitedFunctionBase
    {
        public AsyncTimeRateLimitedFunction(Func<TArg1, TArg2, Task<TResult>> func, TimeSpan minAllowedInterval)
            : base(new AsyncFunction<TArg1, TArg2, TResult>(func), minAllowedInterval) { }

        public AsyncTimeRateLimitedFunction(Func<TArg1, TArg2, Task<TResult>> func, TimeSpan minAllowedInterval, Func<TArg1, TArg2, Task<bool>> doNotRateLimitWhen)
            : this(new AsyncFunction<TArg1, TArg2, TResult>(func), minAllowedInterval, doNotRateLimitWhen) { }

        public AsyncTimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval)
            : base(func, minAllowedInterval) { }

        public AsyncTimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval, Func<TArg1, TArg2, Task<bool>> doNotRateLimitWhen)
            : base(func, minAllowedInterval, async args => await doNotRateLimitWhen((TArg1)args[0], (TArg2)args[1])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1, TArg2 arg2)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1, arg2 });
        public async Task<TimeSpan> GetTimeToNextExecutionAsync(TArg1 arg1, TArg2 arg2)
            => await GetTimeToNextExecutionAsync(new object[] { arg1, arg2 });
    }
    public class AsyncTimeRateLimitedFunction<TArg1, TArg2, TArg3, TResult> : TimeRateLimitedFunctionBase
    {
        public AsyncTimeRateLimitedFunction(Func<TArg1, TArg2, TArg3, Task<TResult>> func, TimeSpan minAllowedInterval)
            : base(new AsyncFunction<TArg1, TArg2, TArg3, TResult>(func), minAllowedInterval) { }

        public AsyncTimeRateLimitedFunction(Func<TArg1, TArg2, TArg3, Task<TResult>> func, TimeSpan minAllowedInterval, Func<TArg1, TArg2, TArg3, Task<bool>> doNotRateLimitWhen)
            : this(new AsyncFunction<TArg1, TArg2, TArg3, TResult>(func), minAllowedInterval, doNotRateLimitWhen) { }

        public AsyncTimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval)
            : base(func, minAllowedInterval) { }

        public AsyncTimeRateLimitedFunction(FunctionBaseDecorator func, TimeSpan minAllowedInterval, Func<TArg1, TArg2, TArg3, Task<bool>> doNotRateLimitWhen)
            : base(func, minAllowedInterval, async args => await doNotRateLimitWhen((TArg1)args[0], (TArg2)args[1], (TArg3)args[2])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1, arg2, arg3 });
        public async Task<TimeSpan> GetTimeToNextExecutionAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => await GetTimeToNextExecutionAsync(new object[] { arg1, arg2, arg3 });
    }
}