using System;
using System.Collections.Concurrent;
using Pholus.Editor.Core;
using UnityEditor;

namespace Pholus.Editor.Providers
{
    /// <summary>
    /// Utility to dispatch actions to Unity's main thread from background threads.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityMainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> _pendingActions = new ConcurrentQueue<Action>();

        static UnityMainThreadDispatcher()
        {
            EditorApplication.update += ProcessQueue;
        }

        /// <summary>
        /// Enqueues an action to be executed on the main thread.
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action != null)
            {
                _pendingActions.Enqueue(action);
            }
        }

        private static void ProcessQueue()
        {
            while (_pendingActions.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    PholusLogger.LogError($"Main thread dispatch error: {ex.Message}");
                }
            }
        }
    }
}
