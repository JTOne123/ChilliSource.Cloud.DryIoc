using DryIoc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChilliSource.Cloud.DryIoc
{
    internal class ScopeContextFactory : Core.IScopeContextFactory, IScopeContextFactorySetup
    {
        private Container _container;
        private HashSet<Type> _singletonTypes;

        public ScopeContextFactory()
        {
            _container = new Container();
            _singletonTypes = new HashSet<Type>();

            _container.RegisterDelegate<ScopeContextFactory>(resolver => this, Reuse.InCurrentScope);
            _container.Register<InScopeValuesHolder>(Reuse.InCurrentScope);

            _container.RegisterDelegate<Core.IResolver>(resolver => new DryIocDependecyResolver(resolver), Reuse.InCurrentScope);
        }

        public Core.IScopeContext CreateScope()
        {
            return new ScopeContext(_container.OpenScope(Guid.NewGuid()));
        }

        public void RegisterSingletonType(Type type)
        {
            _singletonTypes.Add(type);

            _container.RegisterDelegate(type, (resolver) => resolver.Resolve<InScopeValuesHolder>().GetSingletonValue(type));
        }

        public void RegisterServices(ScopeContextHelper.RegisterServices registerServicesAction)
        {
            registerServicesAction(_container, Reuse.InCurrentScope);
        }

        internal void ValidateSingletonType(Type type)
        {
            if (!_singletonTypes.Contains(type))
                throw new ApplicationException($"Type {type.FullName} not registered as singleton type.");
        }


        bool disposed = false;
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            _container.Dispose();
        }
    }
}
