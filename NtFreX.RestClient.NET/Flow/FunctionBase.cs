using System;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Flow
{
    public abstract class FunctionBase<T> : FunctionBaseDecorator
    {
        private readonly Func<object[], Task<T>> _func;

        public override event EventHandler<object> AfterExecution;
        public override event EventHandler<object[]> BeforeExecution;

        protected FunctionBase(Func<object[], Task<T>> func)
        {
            _func = func;
        }

        public override async Task<object> ExecuteInnerAsync(object[] arguments)
        {
            BeforeExecution?.Invoke(this, arguments);
            object result = null;
            try
            {
                result = await _func(arguments);
                return result;
            }
            finally
            {
                AfterExecution?.Invoke(this, result);
            }
            
        }
    }
    public class Function : FunctionBase<object>
    {
        public Function(Action func)
            : base(GetFunction(func)) { }

        private static Func<object[], Task<object>> GetFunction(Action func)
        {
            return async args =>
            {
                func();
                return await Task.FromResult<object>(null);
            };
        }

        public static implicit operator Function(Action x)
            => new Function(x);
    }
    public class Function<T> : FunctionBase<object>
    {
        public Function(Func<T> func)
            : base(GetFunction(func)) { }

        private static Func<object[], Task<object>> GetFunction(Func<T> func)
        {
            return async args =>
            {
                var result = func();
                return await Task.FromResult(result);
            };
        }

        public static implicit operator Function<T>(Func<T> x)
            => new Function<T>(x);
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

        public static implicit operator AsyncFunction(Func<Task> x)
            => new AsyncFunction(x);
    }
    public class AsyncFunction<T> : FunctionBase<object>
    {
        public AsyncFunction(Func<Task<T>> func)
            : base(async args => await func()) { }

        public async Task<T> ExecuteAsync()
            => (T)await ExecuteInnerAsync(null);

        public static implicit operator AsyncFunction<T>(Func<Task<T>> x)
            => new AsyncFunction<T>(x);
    }
    public class AsyncFunction<TArg1, TResult> : FunctionBase<object>
    {
        public AsyncFunction(Func<TArg1, Task<TResult>> func)
            : base(async args => await func((TArg1)args[0])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1)
            => (TResult) await ExecuteInnerAsync(new object[] { arg1 });

        public static implicit operator AsyncFunction<TArg1, TResult>(Func<TArg1, Task<TResult>> x)
            => new AsyncFunction<TArg1, TResult>(x);
    }
    public class AsyncFunction<TArg1, TArg2, TResult> : FunctionBase<object>
    {
        public AsyncFunction(Func<TArg1, TArg2, Task<TResult>> func)
            : base(async args => await func((TArg1)args[0], (TArg2)args[1])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1, TArg2 arg2)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1, arg2 });

        public static implicit operator AsyncFunction<TArg1, TArg2, TResult>(Func<TArg1, TArg2, Task<TResult>> x)
            => new AsyncFunction<TArg1, TArg2, TResult>(x);
    }
    public class AsyncFunction<TArg1, TArg2, TArg3, TResult> : FunctionBase<object>
    {
        public AsyncFunction(Func<TArg1, TArg2, TArg3, Task<TResult>> func)
            : base(async args => await func((TArg1)args[0], (TArg2)args[1], (TArg3)args[2])) { }

        public async Task<TResult> ExecuteAsync(TArg1 arg1, TArg2 arg2, TArg3 arg3)
            => (TResult)await ExecuteInnerAsync(new object[] { arg1, arg2, arg3 });

        public static implicit operator AsyncFunction<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, Task<TResult>> x)
            => new AsyncFunction<TArg1, TArg2, TArg3, TResult>(x);
    }
}