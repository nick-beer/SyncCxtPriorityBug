using System;
using System.Collections.Generic;
using System.Text;

namespace NI.TestUtilities
{
    public interface IMixinStateLocator
    {
        TMixinState GetMixinState<TMixinState>();
    }
}
