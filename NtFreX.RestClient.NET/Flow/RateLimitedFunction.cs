using System;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Flow
{
    public abstract class RateLimitedFunctionBase : FunctionBaseDecorator
    {
        private readonly FunctionBaseDecorator _funcBase;
        private readonly Func<object[], Task<bool>> _ignoreRateLimitFunc;

        public override event EventHandler<object> AfterExecution;

        protected RateLimitedFunctionBase(FunctionBaseDecorator funcBase, Func<object[], Task<bool>> ignoreRateLimitFunc)
        {
            _funcBase = funcBase;
            _ignoreRateLimitFunc = ignoreRateLimitFunc;

            _funcBase.AfterExecution += (sender, objects) => AfterExecution?.Invoke(sender, objects);
        }

        protected abstract Task<TimeSpan> CalculateTimeToNextExecutionAsync(object[] arguments);

        public override async Task<object> ExecuteInnerAsync(object[] arguments)
        {
            TimeSpan rateLimtDelay;
            while ((rateLimtDelay = await GetTimeToNextExecutionAsync(arguments)).Ticks > 0)
            {
                await Task.Delay(rateLimtDelay);
            }

            return await _funcBase.ExecuteInnerAsync(arguments);
        }

        protected async Task<TimeSpan> GetTimeToNextExecutionAsync(object[] arguments)
        {
            if (await _ignoreRateLimitFunc(arguments))
            {
                return TimeSpan.Zero;
            }

            return await CalculateTimeToNextExecutionAsync(arguments);
        }
    }    
}
