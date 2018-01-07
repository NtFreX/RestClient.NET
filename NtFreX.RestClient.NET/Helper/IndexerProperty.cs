using System;

namespace NtFreX.RestClient.NET.Helper
{
    public class IndexerProperty<TArg, TResult>
    {
        private readonly Func<TArg, TResult> _getter;

        public TResult this[TArg arg] => _getter(arg);

        public IndexerProperty(Func<TArg, TResult> getter)
        {
            _getter = getter;
        }
    }
}