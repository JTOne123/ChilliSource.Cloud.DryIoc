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
            var thisFactory = this;

            _container = new Container();
            _singletonTypes = new HashSet<Type>();

            _container.RegisterDelegate<ScopeValidation>(resolver => new ScopeValidation(thisFactory), Reuse.InCurrentScope);
            _container.Register<InScopeValuesHolder>(Reuse.InCurrentScope);

            //preventDisposal -> ScopeContext.Dispose will be called by the client, and should not be called again from the scope dipose itself.
            _container.RegisterDelegate<ScopeContext>(resolver => new ScopeContext(), Reuse.InCurrentScope);
            _container.RegisterDelegate<Core.IResolver>(resolver => resolver.Resolve<ScopeContext>(), Reuse.InCurrentScope);
        }

        public Core.IScopeContext CreateScope()
        {
            var scope = _container.OpenScope();
            var scopeContext = scope.Resolve<ScopeContext>();
            scopeContext.Scope = scope;

            return scopeContext;
        }

        public void RegisterSingletonType(Type type)
        {
            _singletonTypes.Add(type);

            _container.RegisterDelegate(type, (resolver) => resolver.Resolve<InScopeValuesHolder>().GetSingletonValue(type), Reuse.InCurrentScope);
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

    internal class ScopeValidation
    {
        ScopeContextFactory _factory;
        public ScopeValidation(ScopeContextFactory factory)
        {
            _factory = factory;
        }

        internal void ValidateSingletonType(Type type)
        {
            _factory.ValidateSingletonType(type);
        }
    }
}
