using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.BLE.Util
{
    static class ListExtensions
    {
        public static void Enqueue<T>(this List<T> list, T item)
        {
            list.Add(item);
        }

        public static T Dequeue<T>(this List<T> list)
        {
            var item = list.FirstOrDefault();

            if (item != null)
                list.RemoveAt(0);

            return item;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ThreadWorker : IDisposable
    {
        readonly object _locker = new object();
        readonly Thread _worker;
        public readonly List<Action> _taskQueue = new List<Action>();

        /// <summary>
        /// Has any queued tasks?
        /// </summary>
        public bool HasWork
        {
            get
            {
                return Tasks > 0 || currentTask != null;
            }
        }

        /// <summary>
        /// Count of queued tasks
        /// </summary>
        public int Tasks
        {
            get
            {
                return _taskQueue.Count;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SuperQueue{T}"/> class.
        /// </summary>
        /// <param name="workerCount">The worker count.</param>
        /// <param name="dequeueAction">The dequeue action.</param>
        public ThreadWorker(string name = null)
        {
            _worker = new Thread(Consume) { IsBackground = true, Name = $"SuperQueue worker `{name}`" };
            _worker.Start();

        }


        /// <summary>
        ///  Enqueues the task.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="skipIfAny">Skip task execution if any other task are queued</param>
        public void Post(Action task)
        {
            if (disposed)
                throw new InvalidOperationException("Worker is already disposed");


            Task.Run(() =>
            {
                lock (_locker)
                {
                    _taskQueue.Enqueue(task);
                    Monitor.PulseAll(_locker);
                }
            });
        }


        private Action currentTask;
        private bool disposed = false;

        /// <summary>
        /// Consumes this instance.
        /// </summary>
        void Consume()
        {
            while (!disposed)
            {
                lock (_locker)
                {
                    while (_taskQueue.Count == 0) Monitor.Wait(_locker);

                    currentTask = _taskQueue.Dequeue();

                    try
                    {
                        currentTask.Invoke();
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                    finally
                    {
                        currentTask = null;
                    }
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
#if DEBUG
            if (disposed)
                throw new InvalidOperationException("Worker is already disposed");
#endif

            disposed = true;
        }
    }
}
