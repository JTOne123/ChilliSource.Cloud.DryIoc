using DryIoc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChilliSource.Cloud.DryIoc
{
    public class DryIocDependecyResolver : ChilliSource.Cloud.Core.IResolver
    {
        IResolver _inner;
        public DryIocDependecyResolver(IResolver inner)
        {
            _inner = inner;
        }

        public T Get<T>()
        {
            return _inner.Resolve<T>();
        }

        public void Dispose()
        {
            /* Auto disposed at request end */
        }
    }
}
