using System;
using System.Threading;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Flow
{
    public abstract class WeightRateLimitedFunctionBase : RateLimitedFunctionBase
    {
        private readonly int _weight;
        private readonly WeightRateLimitedFunctionConfiguration _configuration;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        protected WeightRateLimitedFunctionBase(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration)
            : this(func, weight, configuration, args => Task.FromResult(false))
        { }

        protected WeightRateLimitedFunctionBase(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration, Func<object[], Task<bool>> ignoreRateLimitFunc)
            : base(func, ignoreRateLimitFunc)
        {
            _weight = weight;
            _configuration = configuration;

            // ReSharper disable once VirtualMemberCallInConstructor
            BeforeExecution += BeforeExecutionHandler;
            // ReSharper disable once VirtualMemberCallInConstructor
            ExecutionDelayed += ExecutionCanceledHandler;
        }

        private void ExecutionCanceledHandler(object sender, object[] objects)
        {
            _semaphoreSlim.Release();
        }

        private void BeforeExecutionHandler(object sender, object[] objects)
        {
            _configuration.AddUsedWeight(_weight);
            _semaphoreSlim.Release();
        }

        protected override async Task<TimeSpan> CalculateTimeToNextExecutionAsync(object[] arguments)
        {
            await _semaphoreSlim.WaitAsync();
            var difference = _configuration.GetLeftWeight() - _weight;
            if (difference >= 0)
            {
                return TimeSpan.Zero;
            }
            return TimeSpan.FromMilliseconds(10);
        }
    }

    public class WeightRateLimitedFunction<T> : WeightRateLimitedFunctionBase
    {
        public WeightRateLimitedFunction(Func<T> func, int weight, WeightRateLimitedFunctionConfiguration configuration)
            : base(new Function<T>(func), weight, configuration) { }

        public WeightRateLimitedFunction(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration)
            : base(func, weight, configuration) { }

        public WeightRateLimitedFunction(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration, Func<bool> doNotRateLimitWhen)
            : base(func, weight, configuration, async args => await Task.FromResult(doNotRateLimitWhen())) { }

        public T Execute()
            => (T) ExecuteInnerAsync(null).GetAwaiter().GetResult();
        public TimeSpan GetTimeToNextExecution()
            => GetTimeToNextExecutionAsync(null).GetAwaiter().GetResult();
    }

    public class AsyncWeightRateLimitedFunction : WeightRateLimitedFunctionBase
    {
        public AsyncWeightRateLimitedFunction(Func<Task> func, int weight, WeightRateLimitedFunctionConfiguration configuration)
            : base(new AsyncFunction(func), weight, configuration) { }

        public AsyncWeightRateLimitedFunction(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration)
            : base(func, weight, configuration) { }

        public AsyncWeightRateLimitedFunction(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration, Func<Task<bool>> doNotRateLimitWhen)
            : base(func, weight, configuration, async args => await doNotRateLimitWhen()) { }

        public async Task ExecuteAsync()
            => await ExecuteInnerAsync(null);
        public async Task<TimeSpan> GetTimeToNextExecutionAsync()
            => await GetTimeToNextExecutionAsync(null);
    }
    public class AsyncWeightRateLimitedFunction<T> : WeightRateLimitedFunctionBase
    {
        public AsyncWeightRateLimitedFunction(Func<Task<T>> func, int weight, WeightRateLimitedFunctionConfiguration configuration)
            : base(new AsyncFunction<T>(func), weight, configuration) { }

        public AsyncWeightRateLimitedFunction(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration)
            : base(func, weight, configuration) { }

        public AsyncWeightRateLimitedFunction(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration, Func<Task<bool>> doNotRateLimitWhen)
            : base(func, weight, configuration, async args => await doNotRateLimitWhen()) { }

        public async Task<T> ExecuteAsync()
            => (T)await ExecuteInnerAsync(null);
        public async Task<TimeSpan> GetTimeToNextExecutionAsync()
            => await GetTimeToNextExecutionAsync(null);
    }
    public class AsyncWeightRateLimitedFunction<TArg1, TResult> : WeightRateLimitedFunctionBase
    {
        public AsyncWeightRateLimitedFunction(Func<TArg1, Task<TResult>> func, int weight, WeightRateLimitedFunctionConfiguration configuration)
            : base(new AsyncFunction<TArg1, TResult>(func), weight, configuration) { }

        public AsyncWeightRateLimitedFunction(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration)
            : base(func, weight, configuration) { }

        public AsyncWeightRateLimitedFunction(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration, Func<TArg1, Task<bool>> doNotRateLimitWhen)
            : base(func, weight, configuration, async args => await doNotRateLimitWhen((TArg1)args[0])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1 });
        public async Task<TimeSpan> GetTimeToNextExecutionAsync(TArg1 arg1)
            => await GetTimeToNextExecutionAsync(new object[] { arg1 });
    }
}