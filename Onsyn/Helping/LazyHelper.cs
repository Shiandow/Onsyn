// Copyright (c) 2018 Shiandow
using System;
using System.Threading.Tasks;

namespace Onsyn.Helping
{
    public static class LazyHelper
    {
        #region Linq Helpers

        public static Lazy<B> Select<A,B>(this Lazy<A> lazy, Func<A, B> f)
        {
            return new Lazy<B>(() => f(lazy.Value));
        }

        public static Lazy<B> SelectMany<A, B>(this Lazy<A> lazy, Func<A, Lazy<B>> f)
        {
            return new Lazy<B>(() => f(lazy.Value).Value);
        }

        public static Lazy<C> SelectMany<A, B, C>(this Lazy<A> lazy, Func<A, Lazy<B>> bind, Func<A,B,C> select)
        {
            return lazy
                .SelectMany((a) => bind(a)
                .Select    ((b) => select(a, b)));
        }

        #endregion

        #region Utility functions

        public static A GetValue<A>(this Lazy<A> lazy)
        {
            return lazy.Value;
        }

        public static A ValueOrDefault<A>(this Lazy<A> lazy)
        {
            return lazy.IsValueCreated ? lazy.Value : default(A);
        }

        public static Task<A> Prefetch<A>(this Lazy<A> lazy)
        {
            return Task.Run(() => lazy.Value);
        }

        public static Lazy<A> Make<A>(Func<A> f)
        {
            return new Lazy<A>(f);
        }

        #endregion
    }
}
