using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChilliSource.Cloud.DryIoc
{
    internal class InScopeValuesHolder
    {
        private Dictionary<Type, object> _singletonObjects;
        private ScopeValidation _scopeValidation;

        public InScopeValuesHolder(ScopeValidation scopeValidation)
        {
            _singletonObjects = new Dictionary<Type, object>();
            _scopeValidation = scopeValidation;
        }

        public object GetSingletonValue(Type type)
        {
            _scopeValidation.ValidateSingletonType(type);

            object value = null;
            _singletonObjects.TryGetValue(type, out value);

            return value;
        }

        public void SetSingletonValue(Type type, object value)
        {
            _scopeValidation.ValidateSingletonType(type);

            if (value != null && !type.IsAssignableFrom(value.GetType()))
            {
                throw new ApplicationException("SetSingletonValue: incompatible type and value");
            }

            _singletonObjects[type] = value;
        }
    }
}
