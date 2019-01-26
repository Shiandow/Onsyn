// Copyright (c) 2018 Shiandow
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Onsyn.Loading
{
    public interface IAwaiter<out T> : ICriticalNotifyCompletion, INotifyCompletion
    {
        bool IsCompleted { get; }
        T GetResult();
    }

    public interface IAwaitable<out T>
    {
        IAwaiter<T> GetAwaiter();
    }

    public class Awaitable<T> : IAwaitable<T>
    {
        private readonly Task<T> m_Task;

        public Awaitable(Task<T> task)
        {
            m_Task = task;
        }

        public IAwaiter<T> GetAwaiter()
            => m_Task.GetAwaiter().Promote();
    }

    public static class AwaitableHelper
    {
        #region Classes

        public struct FromTask<A> : IAwaitable<A>
        {
            private Task<A> m_Task;

            public FromTask(Task<A> task)
            {
                m_Task = task;
            }

            public IAwaiter<A> GetAwaiter()
            {
                return Promote(m_Task.GetAwaiter());
            }
        }

        public struct FromTaskAwaiter<A> : IAwaiter<A>
        {
            public FromTaskAwaiter(TaskAwaiter<A> taskAwaiter)
            {
                m_TaskAwaiter = taskAwaiter;
            }

            #region IAwaiter Passthrough

            private TaskAwaiter<A> m_TaskAwaiter;

            public bool IsCompleted => m_TaskAwaiter.IsCompleted;
            public A GetResult() => m_TaskAwaiter.GetResult();
            public void OnCompleted(Action continuation) 
                => m_TaskAwaiter.OnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation)
                => m_TaskAwaiter.UnsafeOnCompleted(continuation);

            #endregion
        }

        #endregion

        #region Promotion

        public static IAwaiter<A> Promote<A>(this TaskAwaiter<A> taskAwaiter)
            => new FromTaskAwaiter<A>(taskAwaiter);

        public static IAwaitable<A> Promote<A>(this Task<A> task)
            => new FromTask<A>(task);

        #endregion

        #region Utility Functions

        public static A ResultOrDefault<A>(this IAwaitable<A> awaitable)
        {
            var awaiter = awaitable.GetAwaiter();
            return awaiter.IsCompleted
                ? awaiter.GetResult()
                : default(A);
        }

        public static async Task<A> AsTask<A>(this IAwaitable<A> awaitable)
        {
            return await awaitable;
        }

        #endregion
    }
}
