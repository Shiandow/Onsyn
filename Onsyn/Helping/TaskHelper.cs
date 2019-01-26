// Copyright (c) 2018 Shiandow
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Onsyn.Helping
{
    public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs sample);

    public static class TaskHelper
    {
        public static Task<B> Select<A, B>(this Task<A> task, Func<A, B> select)
        {
            return task.Then(t => select(t.Result));
        }

        public static Task<C> SelectMany<A, B, C>(this Task<A> task, Func<A, Task<B>> bind, Func<A, B, C> select)
        {
            return task
                .Select((a) => bind(a)
                .Select((b) => select(a, b)))
                .Unwrap();
        }

        public static Task<B> As<A, B>(this Task<A> task) where A : B => task.Select<A,B>(x => x);
        public static void As<A, B>(this Task<A> task, out Task<B> result) where A : B { result = task.As<A, B>(); }

        public static Task<B> Then<A, B>(this Task<A> task, Func<Task<A>, B> then) => task.ContinueWith(then, TaskContinuationOptions.OnlyOnRanToCompletion);
        public static Task Then<A>(this Task<A> task, Action<Task<A>> then) => task.ContinueWith(then, TaskContinuationOptions.OnlyOnRanToCompletion);
        public static Task<B> Then<B>(this Task task, Func<Task, B> then) => task.ContinueWith(then, TaskContinuationOptions.OnlyOnRanToCompletion);
        public static Task Then(this Task task, Action<Task> then) => task.ContinueWith(then, TaskContinuationOptions.OnlyOnRanToCompletion);

        public static Task<B> Then<A, B>(this Task<A> task, Func<Task<A>, Task<B>> then) => task.Then<A, Task<B>>(then).Unwrap();
        public static Task Then<A>(this Task<A> task, Func<Task<A>, Task> then) => task.Then<A, Task>(then).Unwrap();
        public static Task<B> Then<B>(this Task task, Func<Task, Task<B>> then) => task.Then<Task<B>>(then).Unwrap();
        public static Task Then(this Task task, Func<Task, Task> then) => task.Then<Task>(then).Unwrap();

        public static Task<B> Then<B>(this Task task, Func<B> then) => task.Then((_) => then());
        public static Task Then(this Task task, Action then) => task.Then((_) => then());
        public static Task<B> Then<B>(this Task task, Func<Task<B>> then) => task.Then((_) => then());
        public static Task Then(this Task task, Func<Task> then) => task.Then((_) => then());

        public static Task<A[]> WhenAll<A>(this IEnumerable<Task<A>> list) => Task.WhenAll(list);
        public static Task WhenAll(this IEnumerable<Task> list) => Task.WhenAll(list);

        public static Task<A> WhenAny<A>(this IEnumerable<Task<A>> list) => Task.WhenAny(list).Unwrap();
        public static Task WhenAny(this IEnumerable<Task> list) => Task.WhenAny(list);

        public static Task RunAll(params Action[] actions) => actions.RunAll();
        public static Task RunAll(this IEnumerable<Action> actions) => (from action in actions
                                                                        select Task.Run(action)).WhenAll();

        public static Task RunAny(params Action[] actions) => actions.RunAny();
        public static Task RunAny(this IEnumerable<Action> actions) => (from action in actions
                                                                        select Task.Run(action)).WhenAny();

        public static async Task InvokeAsync<TEventArgs>(this AsyncEventHandler<TEventArgs> eventhandler, object sender, TEventArgs args)
        {
            var list = eventhandler?.GetInvocationList();
            if (list == null)
                return;

            await Task.WhenAll(
                from AsyncEventHandler<TEventArgs> handler in list
                select handler(sender, args));
        }
    }
}
