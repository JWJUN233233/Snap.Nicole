using Microsoft.Extensions.ObjectPool;
using Microsoft.UI.Dispatching;
using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Snap.Nicole.Core.Threading;

internal static class DispatcherQueueExtensions
{
    private static readonly ObjectPool<ManualResetEventSlim> EventPool = new DefaultObjectPoolProvider().Create(new PooledManualResetEventSlimPolicy());

    extension(DispatcherQueue dispatcherQueue)
    {
        public void Invoke(Action action)
        {
            if (dispatcherQueue.HasThreadAccess)
            {
                action();
                return;
            }

            ExceptionDispatchInfo? exceptionDispatchInfoBox = null;
            ManualResetEventSlim blockEvent = EventPool.Get();

            bool queued = dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exceptionDispatchInfoBox = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    blockEvent.Set();
                }
            });

            WaitAndReturnEvent(queued, blockEvent);
            exceptionDispatchInfoBox?.Throw();
        }

        public void Invoke(DispatcherQueuePriority priority, Action action)
        {
            if (dispatcherQueue.HasThreadAccess)
            {
                action();
                return;
            }

            ExceptionDispatchInfo? exceptionDispatchInfo = null;
            ManualResetEventSlim blockEvent = EventPool.Get();
            bool queued = dispatcherQueue.TryEnqueue(priority, () =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    blockEvent.Set();
                }
            });

            WaitAndReturnEvent(queued, blockEvent);
            exceptionDispatchInfo?.Throw();
        }

        public T Invoke<T>(Func<T> func)
        {
            if (dispatcherQueue.HasThreadAccess)
            {
                return func();
            }

            T result = default!;
            ExceptionDispatchInfo? exceptionDispatchInfo = null;
            ManualResetEventSlim blockEvent = EventPool.Get();
            bool queued = dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    blockEvent.Set();
                }
            });

            WaitAndReturnEvent(queued, blockEvent);
            exceptionDispatchInfo?.Throw();
            return result;
        }

        public T Invoke<T>(DispatcherQueuePriority priority, Func<T> func)
        {
            if (dispatcherQueue.HasThreadAccess)
            {
                return func();
            }

            T result = default!;
            ExceptionDispatchInfo? exceptionDispatchInfo = null;
            ManualResetEventSlim blockEvent = EventPool.Get();
            bool queued = dispatcherQueue.TryEnqueue(priority, () =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                }
                finally
                {
                    blockEvent.Set();
                }
            });

            WaitAndReturnEvent(queued, blockEvent);
            exceptionDispatchInfo?.Throw();
            return result;
        }
    }

    private static void WaitAndReturnEvent(bool queued, ManualResetEventSlim blockEvent)
    {
        if (queued)
        {
            blockEvent.Wait();
            EventPool.Return(blockEvent);
        }
        else
        {
            EventPool.Return(blockEvent);
            throw new InvalidOperationException("Failed to enqueue action to the DispatcherQueue.");
        }
    }

    private sealed class PooledManualResetEventSlimPolicy : PooledObjectPolicy<ManualResetEventSlim>
    {
        public override ManualResetEventSlim Create()
        {
            return new(false);
        }

        public override bool Return(ManualResetEventSlim @event)
        {
            try
            {
                @event.Reset();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }
    }
}