using System.Reflection;
using System.Runtime.Loader;

namespace NI.TestUtilities
{
    internal sealed class TestAssemblyLoadContext : AssemblyLoadContext
    {
        public TestAssemblyLoadContext()
            : base(isCollectible: true)
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);
            return LoadFromAssemblyPath(assembly.Location);
        }
    }
}
