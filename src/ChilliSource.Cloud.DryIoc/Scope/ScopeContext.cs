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
        internal IContainer Scope { get; set; }

        public ScopeContext()
        {
        }

        public T Get<T>()
        {
            return Scope.Resolve<T>(ifUnresolved: IfUnresolved.ReturnDefault);
        }

        public T GetSingletonValue<T>()
        {
            return (T)Scope.Resolve<InScopeValuesHolder>().GetSingletonValue(typeof(T));
        }

        public void SetSingletonValue<T>(T value)
        {
            Scope.Resolve<InScopeValuesHolder>().SetSingletonValue(typeof(T), value);
        }

        bool disposed = false;
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Scope.Dispose();
        }
    }
}
