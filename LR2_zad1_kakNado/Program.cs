using System;
using System.Collections.Generic;
using System.Threading;

namespace LR2_zad1_kakNado
{
    public delegate void TaskDelegate();

    class Program
    {
        static void Main(string[] args)
        {
            TaskQueue taskQueue = new TaskQueue(1000);
            TaskDelegate task;
            task = Hello;

            for(int i=0;i<100;i++)
            {
                taskQueue.EnqueueTask(task);
            }

            Console.ReadKey();
        }

        static void Hello()
        {
            Thread.Sleep(10);
            Console.WriteLine("Hello ThreadPool!");            
        }
    }

    public class TaskQueue
    {
        private int numberThreads;
        private Thread[] threads;

        private Dictionary<int, ManualResetEvent> threadsEvent;
        private ManualResetEvent scheduleEvent;
        private Thread scheduleThread;

        private Queue<TaskDelegate> tasks = new Queue<TaskDelegate>();


        // Создает пул потоков с указанным количеством потоков
        public TaskQueue(int numberThreads)
        {
            if (numberThreads <= 0)
                throw new ArgumentException("numberThreads", "Количество потоков должно быть больше нуля.");

            this.numberThreads = numberThreads;
            threads = new Thread[numberThreads];

            threadsEvent = new Dictionary<int, ManualResetEvent>(numberThreads);
            scheduleEvent = new ManualResetEvent(false);
            scheduleThread = new Thread(SelectAndStartFreeThread) { Name = "Schedule Thread", IsBackground = true };
            scheduleThread.Start();

            for (int i = 0; i < numberThreads; i++)
            {
                threads[i] = new Thread(ThreadWork) { Name = "Thread " + i.ToString(), IsBackground = true };
                threadsEvent.Add(threads[i].ManagedThreadId, new ManualResetEvent(false));
                threads[i].Start();
            }
        }

        // Выбор свободного потока
        private void SelectAndStartFreeThread()
        {
            while (true)
            {
                scheduleEvent.WaitOne();
                lock (threads)
                {
                    foreach (var thread in threads)
                    {
                        if (threadsEvent[thread.ManagedThreadId].WaitOne(0) == false)
                        {
                            threadsEvent[thread.ManagedThreadId].Set();
                            break;
                        }
                    }
                }
                scheduleEvent.Reset();
            }
        }

        // Выполения делегатов из очереди
        private void ThreadWork()
        {
            TaskDelegate task;

            while (true)
            {
                threadsEvent[Thread.CurrentThread.ManagedThreadId].WaitOne();

                if(tasks.TryDequeue(out task))
                {
                    try 
                    { 
                       task();
                       Console.WriteLine(Thread.CurrentThread.Name);
                    }
                    finally
                    {
                        scheduleEvent.Set();
                        threadsEvent[Thread.CurrentThread.ManagedThreadId].Reset();
                    }

                }
            }
        }

        // Добавление в очередь делегатов
        public void EnqueueTask(TaskDelegate task)
        {
            tasks.Enqueue(task);
            scheduleEvent.Set();
            //Console.WriteLine("Task add: " + task.Method);
        }      

    }
}
