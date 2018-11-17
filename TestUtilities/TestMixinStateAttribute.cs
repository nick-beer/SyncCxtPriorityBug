using System;
using System.Collections.Generic;
using System.Text;

namespace NI.TestUtilities
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class TestMixinStateAttribute : Attribute
    {
        public TestMixinStateAttribute(Type stateType)
        {
            StateType = stateType;
        }

        public Type StateType { get; }
    }
}
