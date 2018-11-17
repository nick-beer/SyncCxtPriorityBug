using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NI.TestUtilities
{
    [TestMixinState(typeof(AsyncTestState))]
    public interface IAsyncTest : ITestMixin { }

    public static class AsyncTest
    {
        public static void RunTest(this IAsyncTest instance, Action test)
            => new SyncTestRunner().RunTestAsync(instance, test);

        public static void RunTestAsync(this IAsyncTest instance, Func<Task> testAsync)
            => new AsyncTestRunner().RunTestAsync(instance, testAsync);

        private sealed class AsyncTestRunner : TestRunner<Func<Task>>
        {
            protected override Func<Task> ToFuncOfTask(Func<Task> @delegate)
                => @delegate;

            protected override Func<Task> ToFuncOfTask(MethodInfo mi, object obj)
                => new Func<Task>(() => (Task)mi.Invoke(obj, null));
        }

        private sealed class SyncTestRunner : TestRunner<Action>
        {
            protected override Func<Task> ToFuncOfTask(Action @delegate)
                => new Func<Task>(() =>
                {
                    @delegate();
                    return Task.CompletedTask;
                });

            protected override Func<Task> ToFuncOfTask(MethodInfo mi, object obj)
                => ToFuncOfTask((Action)mi.CreateDelegate(typeof(Action), obj));
        }

        private abstract class TestRunner<T> where T : Delegate
        {
            public void RunTestAsync(IAsyncTest instance, T test)
            {
                if (instance.GetMixinState<AsyncTestState>().UseLoadContext)
                {
                    try
                    {
                        ExecuteInNewLoadContext(test);
                    }
                    catch (Exception e)
                    {
                        ThrowExceptionForCallingLoadContext(e);
                    }
                }
                else
                {
                    RunTestAsync(ToFuncOfTask(test), useLoadContext: false);
                }
            }

            protected abstract Func<Task> ToFuncOfTask(MethodInfo mi, object obj);

            protected abstract Func<Task> ToFuncOfTask(T @delegate);

            private void ExecuteInNewLoadContext(T test)
            {
                var context = new TestAssemblyLoadContext();

                try
                {
                    var mi = test.GetMethodInfo();
                    var assemblyPath = mi.DeclaringType.Assembly.Location;
                    var typeName = mi.DeclaringType.FullName;
                    var methodName = mi.Name;

                    var assembly = context.LoadFromAssemblyPath(GetType().Assembly.Location);
                    var type = assembly.GetType(GetType().FullName);
                    var method = type.GetMethod(nameof(ReflectionRunTestAsync), BindingFlags.NonPublic | BindingFlags.Instance);

                    method.Invoke(null, new object[] { assemblyPath, typeName, methodName });
                }
                finally
                {
                    context.Unload();
                }
            }

            private void ReflectionRunTestAsync(string path, string typeName, string methodName)
            {
                var context = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
                var assembly = context.LoadFromAssemblyPath(path);
                var type = assembly.GetType(typeName);
                var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                var obj = method.IsStatic ? null : Activator.CreateInstance(type);

                RunTestAsync(ToFuncOfTask(method, obj), useLoadContext: true);
            }

            private static void RunTestAsync(Func<Task> testAsync, bool useLoadContext)
            {
                ExceptionDispatchInfo edi = null;
                var t = new Thread((o) =>
                {
                    var d = Dispatcher.CurrentDispatcher;
                    var op = d.InvokeAsync(async () =>
                    {
                        try
                        {
                            await testAsync();
                        }
                        finally
                        {
                            d.InvokeShutdown();
                        }
                    });
                    Dispatcher.Run();
                    if (op.Result.Exception != null)
                    {
                        edi = ExceptionDispatchInfo.Capture(op.Result.Exception.GetBaseException());
                    }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Name = "AsyncTest TestHarness";

                t.Start();
                t.Join();
                edi?.Throw();
            }

            private static void ThrowExceptionForCallingLoadContext(Exception e)
            {
                var conext = AssemblyLoadContext.GetLoadContext(typeof(AsyncTest).Assembly);
                if (conext != AssemblyLoadContext.GetLoadContext(e.GetType().Assembly))
                {
                    using (var ms = new MemoryStream())
                    {
                        var f = new BinaryFormatter();
                        f.Serialize(ms, e);
                        e = (Exception)f.Deserialize(ms);
                    }
                }
                ExceptionDispatchInfo.Capture(e).Throw();
            }
        }
    }

    public sealed class AsyncTestState
    {
        public bool UseLoadContext { get; set; }
    }
}
