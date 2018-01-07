using System;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Flow
{
    public abstract class WeightRateLimitedFunctionBase : RateLimitedFunctionBase
    {
        private readonly int _weight;
        private readonly WeightRateLimitedFunctionConfiguration _configuration;

        protected WeightRateLimitedFunctionBase(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration)
            : this(func, weight, configuration, args => Task.FromResult(false))
        { }

        protected WeightRateLimitedFunctionBase(FunctionBaseDecorator func, int weight, WeightRateLimitedFunctionConfiguration configuration, Func<object[], Task<bool>> ignoreRateLimitFunc)
            : base(func, ignoreRateLimitFunc)
        {
            _weight = weight;
            _configuration = configuration;

            func.AfterExecution += (sender, objects) => _configuration.AddUsedWeight(weight);
        }

        protected override async Task<TimeSpan> CalculateTimeToNextExecutionAsync(object[] arguments)
            => await Task.FromResult(_configuration.GetLeftWeight() > _weight ? TimeSpan.Zero : TimeSpan.FromSeconds(1));
    }

    public class WeightRateLimitedFunction<T> : WeightRateLimitedFunctionBase
    {
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