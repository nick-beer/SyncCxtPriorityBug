using NI.TestUtilities;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit;

using static System.Windows.Threading.DispatcherPriority;

namespace SyncContext_Bug
{
    public class SyncContextTests : AutoTest, IAsyncTest
    {
        [Fact]
        public void SyncContextPriority_Fails()
        {
            this.RunTestAsync(async () =>
            {
                var t1 = InvokeOnDispatcherAsync(ApplicationIdle, () =>
                {
                    var priority = CurrentPriorityFromSynchronizationContext();
                    Assert.Equal(ApplicationIdle, priority);
                });

                var t2 = InvokeOnDispatcherAsync(Render, () =>
                {
                    var priority = CurrentPriorityFromSynchronizationContext();
                    Assert.Equal(Render, priority);
                });

                await Task.WhenAll(t1, t2);
            });
        }

        [Fact]
        public void InvokePriority_Succeeds()
        {
            this.RunTestAsync(async () =>
            {
                int hitCount = 0;
                var t1 = InvokeOnDispatcherAsync(ApplicationIdle, () =>
                {
                    Assert.Equal(1, hitCount);
                    hitCount++;
                });

                var t2 = InvokeOnDispatcherAsync(Render, () =>
                {
                    Assert.Equal(0, hitCount);
                    hitCount++;
                });

                await Task.WhenAll(t1, t2);
            });
        }

        private static Task InvokeOnDispatcherAsync(DispatcherPriority priority, Action action)
            => Dispatcher.CurrentDispatcher.InvokeAsync(action, priority).Task;

        public static DispatcherPriority CurrentPriorityFromSynchronizationContext()
        {
            var type = typeof(DispatcherSynchronizationContext);
            var field = type.GetField("_priority", BindingFlags.Instance | BindingFlags.NonPublic);
            return (DispatcherPriority)field.GetValue(SynchronizationContext.Current);
        }
    }
}
