using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChilliSource.Cloud.DryIoc
{
    /// <summary>
    /// Allows customization of ScopeContextFactory
    /// </summary>
    public interface IScopeContextFactorySetup
    {
        /// e.g. kernel.Bind&lt;MyServiceA&gt;().ToSelf().InScopeAction(scopeAction);
        /// </summary>
        /// <param name="registerServices">Delegate to bind services</param>
        void RegisterServices(ScopeContextHelper.RegisterServices registerServicesAction);

        /// <summary>
        /// Registers a type as a Singleton type. The object creation is not handled by the kernel. <br/>
        /// Instead, the caller can provide an object instance for the scope.
        /// </summary>
        /// <param name="type">A object type</param>
        void RegisterSingletonType(Type type);
    }
}
