using System;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Flow
{
    public abstract class FunctionBase<T> : FunctionBaseDecorator
    {
        private readonly Func<object[], Task<T>> _func;

        public override event EventHandler<object> AfterExecution;

        protected FunctionBase(Func<object[], Task<T>> func)
        {
            _func = func;
        }

        public override async Task<object> ExecuteInnerAsync(object[] arguments)
        {
            var result = await _func(arguments);
            AfterExecution?.Invoke(this, result);
            return result;
        }
    }

    public class AsyncFunction : FunctionBase<object>
    {
        public AsyncFunction(Func<Task> func)
            : base(GetFunction(func)) { }

        private static Func<object[], Task<object>> GetFunction(Func<Task> func)
        {
            return async args =>
            {
                await func();
                return null;
            };
        }
    }
    public class AsyncFunction<T> : FunctionBase<object>
    {
        public AsyncFunction(Func<Task<T>> func)
            : base(async args => await func()) { }

        public async Task<T> ExecuteAsync()
            => (T)await ExecuteInnerAsync(null);
    }
    public class AsyncFunction<TArg1, TResult> : FunctionBase<object>
    {
        public AsyncFunction(Func<TArg1, Task<TResult>> func)
            : base(async args => await func((TArg1)args[0])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1)
            => (TResult) await ExecuteInnerAsync(new object[] { arg1 });
    }
    public class AsyncFunction<TArg1, TArg2, TResult> : FunctionBase<object>
    {
        public AsyncFunction(Func<TArg1, TArg2, Task<TResult>> func)
            : base(async args => await func((TArg1)args[0], (TArg2)args[1])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1, TArg2 arg2)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1, arg2 });
    }
    public class AsyncFunction<TArg1, TArg2, TArg3, TResult> : FunctionBase<object>
    {
        public AsyncFunction(Func<TArg1, TArg2, TArg3, Task<TResult>> func)
            : base(async args => await func((TArg1)args[0], (TArg2)args[1], (TArg3)args[2])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1, arg2, arg3 });
    }
}