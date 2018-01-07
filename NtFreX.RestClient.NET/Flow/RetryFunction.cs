using System;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Flow
{
    public abstract class RetryFunctionBase : FunctionBaseDecorator
    {
        private readonly FunctionBaseDecorator _funcBase;
        private readonly int _maxRetryCount;
        private readonly Func<object, bool> _retryOnResult;
        private readonly Func<Exception, bool> _retryOnException;

        public override event EventHandler<object[]> BeforeExecution;
        public override event EventHandler<object> AfterExecution;

        protected RetryFunctionBase(FunctionBaseDecorator funcBase, int maxRetryCount, Func<object, bool> retryOnResult, Func<Exception, bool> retryOnException)
        {
            _funcBase = funcBase;
            _maxRetryCount = maxRetryCount;
            _retryOnResult = retryOnResult;
            _retryOnException = retryOnException;

            _funcBase.BeforeExecution += (sender, objects) => BeforeExecution?.Invoke(sender, objects);
            _funcBase.AfterExecution += (sender, objects) => AfterExecution?.Invoke(sender, objects);
        }

        private async Task<object> ExecuteInnerAsync(object[] arguments, int currentRetryCount)
        {

            try
            {
                var result = await _funcBase.ExecuteInnerAsync(arguments);
                if (_retryOnResult(result) && currentRetryCount < _maxRetryCount)
                {
                    return await ExecuteInnerAsync(arguments, currentRetryCount + 1);
                }
                return result;
            }
            catch (Exception exce)
            {
                if (_retryOnException(exce) && currentRetryCount < _maxRetryCount)
                {
                    return await ExecuteInnerAsync(arguments, currentRetryCount + 1);
                }
                throw;
            }
        }

        public override async Task<object> ExecuteInnerAsync(object[] arguments)
            => await ExecuteInnerAsync(arguments, 0);
    }

    public class AsyncRetryFunction<T> : RetryFunctionBase
    {
        public AsyncRetryFunction(FunctionBaseDecorator func, int maxRetryCount, Func<T, bool> retryOnResult, Func<Exception, bool> retryOnException)
            : base(func, maxRetryCount, arg => retryOnResult((T)arg), retryOnException) { }

        public async Task<T> ExecuteAsync()
            => (T)await ExecuteInnerAsync(null);
    }
    public class AsyncRetryFunction<TArg1, TResult> : RetryFunctionBase
    {
        public AsyncRetryFunction(FunctionBaseDecorator func, int maxRetryCount, Func<TResult, bool> retryOnResult, Func<Exception, bool> retryOnException)
            : base(func, maxRetryCount, arg1 => retryOnResult((TResult)arg1), retryOnException) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1 });
    }
}
