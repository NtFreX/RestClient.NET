using System;
using System.Threading.Tasks;

namespace NtFreX.RestClient.NET.Flow
{
    public abstract class FunctionBaseDecorator
    {
        public abstract event EventHandler<object> AfterExecution;

        public abstract Task<object> ExecuteInnerAsync(object[] arguments);
    }
}