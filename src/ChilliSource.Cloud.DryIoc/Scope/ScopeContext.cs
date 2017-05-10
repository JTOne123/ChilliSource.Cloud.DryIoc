using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DryIoc;

namespace ChilliSource.Cloud.DryIoc
{
    internal class ScopeContext : Core.IScopeContext
    {
        private IContainer _scope;
        private Core.IResolver _resolver;

        public ScopeContext(IContainer scope)
        {
            this._scope = scope;
            this._resolver = scope.Resolve<Core.IResolver>();
        }

        bool disposed = false;
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            _scope.Dispose();
        }

        public T Get<T>()
        {
            return _resolver.Get<T>();
        }

        public T GetSingletonValue<T>()
        {
            return (T)_resolver.Get<InScopeValuesHolder>().GetSingletonValue(typeof(T));
        }

        public void SetSingletonValue<T>(T value)
        {
            _resolver.Get<InScopeValuesHolder>().SetSingletonValue(typeof(T), value);
        }
    }
}
