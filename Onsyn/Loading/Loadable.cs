// Copyright (c) 2018 Shiandow
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Onsyn.Helping;

namespace Onsyn.Loading
{
    public interface ILoadable<out T>
    {
        IAwaitable<T> Load();
    }

    public class Loadable<T> : ILoadable<T>
    {
        private readonly Lazy<IAwaitable<T>> m_LazyAsync;

        public Loadable(Func<Task<T>> taskFactory)
        {
            m_LazyAsync = LazyHelper.Make(() => taskFactory().Promote());
        }

        public IAwaitable<T> Load()
            => m_LazyAsync.Value;
    }

    public static partial class LoadableHelper
    {
        #region Monad Implementation

        public static ILoadable<A> Return<A>(A value)
        {
            return new Loadable<A>(() => Task.FromResult(value));
        }

        public static ILoadable<A> Return<A>(Task<A> value)
        {
            return new Loadable<A>(() => value);
        }

        public static ILoadable<B> Map<A, B>(this ILoadable<A> input, Func<A, B> f)
        {
            return new Loadable<B>(async () => f(await input.Load()));
        }

        public static ILoadable<B> Bind<A,B>(this ILoadable<A> input, Func<A, ILoadable<B>> f)
        {
            return new Loadable<B>(async () => await f(await input.Load()).Load());
        }

        #endregion

        #region Linq Helpers

        public static ILoadable<B> Select<A, B>(this ILoadable<A> loadable, Func<A, B> f)
        {
            return loadable.Map(f);
        }

        public static ILoadable<B> SelectMany<A, B>(this ILoadable<A> loadable, Func<A, ILoadable<B>> f)
        {
            return loadable.Bind(f);
        }

        public static ILoadable<C> SelectMany<A, B, C>(this ILoadable<A> loadable, Func<A, ILoadable<B>> bind, Func<A, B, C> select)
        {
            return loadable
                .Bind((a) => bind(a)
                .Map((b) => select(a, b)));
        }

        #endregion

        #region Utility Functions

        public static ILoadable<A> Create<A>()
            where A : new()
        {
            return Return(Task.Run(() => new A()));
        }

        public static ILoadable<A> With<A>(this ILoadable<A> loadable, Action<A> action)
        {
            return loadable.Select(x => { action(x); return x; });
        }

        public static ILoadable<IEnumerable<A>> Fold<A>(this IEnumerable<ILoadable<A>> loadables)
        {
            return new Loadable<IEnumerable<A>>(async () 
                => await loadables.Select(async loadable => await loadable.Load()).WhenAll());
        }

        #endregion
    }
}
