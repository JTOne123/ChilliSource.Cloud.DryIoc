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

            _container.RegisterDelegate<Core.IResolver>(resolver => new DryIocDependecyResolver(resolver), Reuse.InCurrentScope);
        }

        public Core.IScopeContext CreateScope()
        {
            return new ScopeContext(_container.OpenScope());
        }

        public void RegisterSingletonType(Type type)
        {
            _singletonTypes.Add(type);
            var singletonRegistration = (ISingletonRegistration)Activator.CreateInstance(typeof(SingletonRegistration<>).MakeGenericType(type));

            singletonRegistration.RegisterInContainer(_container);
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

    internal interface ISingletonRegistration
    {
        void RegisterInContainer(IContainer container);
    }
    internal class SingletonRegistration<T> : ISingletonRegistration
    {
        private static readonly Type _singletonType = typeof(T);

        public void RegisterInContainer(IContainer container)
        {
            container.RegisterDelegate<T>(ResolveSingletonValue, Reuse.InCurrentScope);
        }

        public T ResolveSingletonValue(IResolver resolver)
        {
            return (T)resolver.Resolve<InScopeValuesHolder>().GetSingletonValue(_singletonType);
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
