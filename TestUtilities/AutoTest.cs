using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NI.TestUtilities
{
    public abstract class AutoTest : IMixinStateLocator
    {
        private readonly Dictionary<Type, object> _mixinStates = new Dictionary<Type, object>();

        public AutoTest()
        {
            var mixinInterfaces = GetType().GetInterfaces().Distinct();
            var stateTypes = mixinInterfaces
                .Select(i => i.GetCustomAttributes<TestMixinStateAttribute>().FirstOrDefault()?.StateType)
                .Where(t => t != null)
                .Distinct();

            foreach (var stateType in stateTypes)
            {
                _mixinStates[stateType] = Activator.CreateInstance(stateType);
            }
        }

        public TMixinState GetMixinState<TMixinState>()
            => (TMixinState)_mixinStates[typeof(TMixinState)];
    }
}
