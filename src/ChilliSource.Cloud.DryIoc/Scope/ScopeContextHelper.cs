using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DryIoc;

namespace ChilliSource.Cloud.DryIoc
{
    /// <summary>
    /// Allows the creation of a scope context Factory
    /// </summary>
    public static class ScopeContextHelper
    {
        /// <summary>
        /// Delegate to bind services in the container using a specific reuse.
        /// </summary>
        /// <param name="container">A dryioc container</param>
        /// <param name="scopeAction">A reuse to register services with</param>               
        public delegate void RegisterServices(IContainer container, IReuse reuse);

        /// <summary>
        /// Creates a scope context factory
        /// </summary>
        /// <param name="defaultKernel">A default kernel</param>
        /// <param name="kernelBinder">A kernel binder delegate</param>
        /// <returns>Returns a scope context factory</returns>
        public static Core.IScopeContextFactory CreateFactory(Action<IScopeContextFactorySetup> setupAction)
        {
            var factory = new ScopeContextFactory();
            setupAction(factory);

            return factory;
        }
    }
}
