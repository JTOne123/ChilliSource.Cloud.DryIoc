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

        public ScopeContext(IContainer scope)
        {
            _scope = scope;
        }

        public T Get<T>()
        {
            return _scope.Resolve<T>();
        }

        public T GetSingletonValue<T>()
        {
            return (T)_scope.Resolve<InScopeValuesHolder>().GetSingletonValue(typeof(T));
        }

        public void SetSingletonValue<T>(T value)
        {
            _scope.Resolve<InScopeValuesHolder>().SetSingletonValue(typeof(T), value);
        }

        bool disposed = false;
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            _scope.Dispose();
        }
    }
}
