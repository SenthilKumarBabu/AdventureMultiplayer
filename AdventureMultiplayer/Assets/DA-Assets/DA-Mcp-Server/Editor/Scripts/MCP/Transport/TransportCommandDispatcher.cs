using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;

namespace DA_Assets.Shared.MCP
{
    public static class TransportCommandDispatcher
    {
        private static readonly Queue<Func<Task>> Queue = new();
        private static bool _isPumping;

        public static Task<T> RunOnMainThreadAsync<T>(Func<Task<T>> action)
        {
            var tcs = new TaskCompletionSource<T>();
            lock (Queue)
            {
                Queue.Enqueue(async () =>
                {
                    try
                    {
                        T result = await action();
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
            }
            RequestPump();
            return tcs.Task;
        }

        private static void RequestPump()
        {
            if (_isPumping)
            {
                return;
            }

            _isPumping = true;
            EditorApplication.update += Pump;
        }

        private static void Pump()
        {
            Func<Task> item = null;
            lock (Queue)
            {
                if (Queue.Count > 0)
                {
                    item = Queue.Dequeue();
                }
                else
                {
                    EditorApplication.update -= Pump;
                    _isPumping = false;
                    return;
                }
            }

            _ = item();
        }
    }
}
