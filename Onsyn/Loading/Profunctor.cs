// Copyright (c) 2018 Shiandow
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Onsyn.Helping;

namespace Onsyn.Loading
{
    public interface ILoadable<in A, out B>
    {
        IAwaitable<B> Load(A value);
    }

    public class Loadable<A, B> : ILoadable<A, B>
    {
        private readonly TaskCompletionSource<A> m_Input;
        private readonly ILoadable<B> m_Output;

        public Loadable(Func<A, Task<B>> taskFactory)
        {
            m_Input = new TaskCompletionSource<A>();
            m_Output = new Loadable<B>(async () => await taskFactory(await m_Input.Task));
        }

        public IAwaitable<B> Load(A value)
        {
            m_Input.TrySetResult(value);
            return m_Output.Load();
        }
    }

    public static partial class LoadableHelper
    {
        #region Monad Implementation

        public static ILoadable<A, B> Return<A, B>(Func<A, B> f)
        {
            return new Loadable<A, B>(a => Task.FromResult(f(a)));
        }

        public static ILoadable<X, B> Map<X, A, B>(this ILoadable<X, A> input, Func<A, B> f)
        {
            return new Loadable<X, B>(async x => f(await input.Load(x)));
        }

        public static ILoadable<X, B> Bind<X, A, B>(this ILoadable<X, A> input, Func<A, ILoadable<X, B>> f)
        {
            return new Loadable<X, B>(async x => await f(await input.Load(x)).Load(x));
        }

        #region Semi-Monad Implementation

        public static ILoadable<X, B> Bind<X, A, B>(this ILoadable<A> input, Func<A, ILoadable<X, B>> f)
        {
            return new Loadable<X, B>(async x => await f(await input.Load()).Load(x));
        }

        public static ILoadable<X, B> Bind<X, A, B>(this ILoadable<X, A> input, Func<A, ILoadable<B>> f)
        {
            return new Loadable<X, B>(async x => await f(await input.Load(x)).Load());
        }

        #endregion

        #endregion

        #region Cofunctor Implementation

        public static ILoadable<Y, A> MapLeft<X, Y, A>(this ILoadable<X, A> input, Func<Y, X> f)
        {
            return new Loadable<Y, A>(async y => await input.Load(f(y)));
        }

        #endregion

        #region Profunctor Implementation

        public static ILoadable<A, C> Compose<A, B, C>(this ILoadable<A, B> lhs, ILoadable<B, C> rhs)
        {
            return new Loadable<A, C>(async a => await rhs.Load(await lhs.Load(a)));
        }

        public static ILoadable<B> Eval<A, B>(this ILoadable<A, B> loadable, ILoadable<A> value)
        {
            return new Loadable<B>(async () => await loadable.Load(await value.Load()));
        }

        public static ILoadable<A, ILoadable<B, C>> Curry<A, B, C>(this ILoadable<Tuple<A, B>, C> loadable)
        {
            return Return<A, ILoadable<B, C>>(
                a => new Loadable<B, C>(async 
                b => await loadable.Load(Tuple.Create(a,b))));
        }

        #endregion

        #region Linq Helpers

        public static ILoadable<X, B> Select<X, A, B>(this ILoadable<X, A> loadable, Func<A, B> f)
        {
            return loadable.Map(f);
        }

        public static ILoadable<X, B> SelectMany<X, A, B>(this ILoadable<X, A> loadable, Func<A, ILoadable<X, B>> f)
        {
            return loadable.Bind(f);
        }

        public static ILoadable<X, C> SelectMany<X, A, B, C>(this ILoadable<X, A> loadable, Func<A, ILoadable<X, B>> bind, Func<A, B, C> select)
        {
            return loadable
                .Bind((a) => bind(a)
                .Map((b) => select(a, b)));
        }

        #endregion

        #region Utility Functions

        public static ILoadable<X, A> With<X, A>(this ILoadable<X, A> loadable, Action<A> action)
        {
            return loadable.Select(a => { action(a); return a; });
        }

        public static ILoadable<X, IEnumerable<A>> Fold<X, A>(this IEnumerable<ILoadable<X, A>> loadables)
        {
            return new Loadable<X, IEnumerable<A>>(async x 
                => await loadables.Select(async loadable => await loadable.Load(x)).WhenAll());
        }

        #endregion
    }
}
