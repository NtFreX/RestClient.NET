using System;
using System.Threading;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Flow
{
    public abstract class ConcurrentFunctionBase : FunctionBaseDecorator
    {
        private readonly FunctionBaseDecorator _funcBase;
        private readonly SemaphoreSlim _semaphoreSlim;

        public override event EventHandler<object[]> BeforeExecution;
        public override event EventHandler<object> AfterExecution;
        public override event EventHandler<object[]> ExecutionDelayed;

        protected ConcurrentFunctionBase(FunctionBaseDecorator funcBase, int maxConcurrency)
        {
            _funcBase = funcBase;
            _semaphoreSlim = new SemaphoreSlim(maxConcurrency);

            _funcBase.BeforeExecution += (sender, objects) => BeforeExecution?.Invoke(sender, objects);
            _funcBase.AfterExecution += (sender, objects) => AfterExecution?.Invoke(sender, objects);
            _funcBase.ExecutionDelayed += (sender, objects) => ExecutionDelayed?.Invoke(sender, objects);
        }

        public override async Task<object> ExecuteInnerAsync(object[] arguments)
        {
            _semaphoreSlim.Wait();
            try
            {
                return await _funcBase.ExecuteInnerAsync(arguments);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }

    public class ConcurrentFunction : ConcurrentFunctionBase
    {
        public ConcurrentFunction(Action func, int maxConcurrency)
            : this(new Function(func), maxConcurrency) { }

        public ConcurrentFunction(FunctionBaseDecorator func, int maxConcurrency)
            : base(func, maxConcurrency) { }

        public void Execute()
            => ExecuteInnerAsync(null).GetAwaiter().GetResult();
    }
    public class ConcurrentFunction<T> : ConcurrentFunctionBase
    {
        public ConcurrentFunction(Func<T> func, int maxConcurrency)
            : this(new Function<T>(func), maxConcurrency) { }

        public ConcurrentFunction(FunctionBaseDecorator func, int maxConcurrency)
            : base(func, maxConcurrency) { }

        public T Execute()
            => (T) ExecuteInnerAsync(null).GetAwaiter().GetResult();
    }
    public class AsyncConcurrentFunction : ConcurrentFunctionBase
    {
        public AsyncConcurrentFunction(Func<Task> func, int maxConcurrency)
            : this(new AsyncFunction(func), maxConcurrency) { }

        public AsyncConcurrentFunction(FunctionBaseDecorator func, int maxConcurrency)
            : base(func, maxConcurrency) { }

        public async Task ExecuteAsync()
            => await ExecuteInnerAsync(null);
    }
    public class AsyncConcurrentFunction<T> : ConcurrentFunctionBase
    {
        public AsyncConcurrentFunction(Func<Task<T>> func, int maxConcurrency)
            : this(new AsyncFunction<T>(func), maxConcurrency) { }

        public AsyncConcurrentFunction(FunctionBaseDecorator func, int maxConcurrency)
            : base(func, maxConcurrency) { }

        public async Task<T> ExecuteAsync()
            => (T)await ExecuteInnerAsync(null);
    }
    public class AsyncConcurrentFunction<TArg1, TResult> : ConcurrentFunctionBase
    {
        public AsyncConcurrentFunction(Func<TArg1, Task<TResult>> func, int maxConcurrency)
            : this(new AsyncFunction<TArg1, TResult>(func), maxConcurrency) { }

        public AsyncConcurrentFunction(FunctionBaseDecorator func, int maxConcurrency)
            : base(func, maxConcurrency) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1 });
    }
}
