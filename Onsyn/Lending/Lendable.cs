﻿// Copyright (c) 2018 Shiandow
using System;
using System.Collections.Generic;
using System.Linq;

namespace Onsyn.Lending
{
    using static LeaseHelper;

    /// <summary>
    /// Represents a value that can be 'leased'
    /// </summary>
    public interface ILendable<out TValue>
    {
        ILease<TValue> GetLease();
    }

    /// <summary>
    /// Simple reference counting implementation
    /// Exposes methods for allocating, calculating, deallocating the value as needed
    /// </summary>
    public abstract class Lendable<TValue> : ILendable<TValue>
    {
        protected abstract void Allocate();

        protected abstract TValue Value { get; }

        protected abstract void Deallocate();

        #region ILeaseable Implementation

        // Counted reference
        private struct Lease : ILease<TValue>
        {
            private readonly Lendable<TValue> m_Owner;

            public Lease(Lendable<TValue> owner)
            {
                m_Owner = owner;
                m_Disposed = false;
            }

            public TValue Value { get { return m_Owner.Value; } }

            #region Resource Management

            private bool m_Disposed; // It's important we only dispose once

            public void Dispose()
            {
                if (!m_Disposed)
                {
                    m_Disposed = true;
                    m_Owner.Release();
                }
            }

            #endregion
        }

        // Reference counter
        private int m_Leases = 0;

        public ILease<TValue> GetLease()
        {
            if (m_Leases <= 0)
                Allocate();

            m_Leases += 1;
            return new Lease(this);
        }

        private void Release()
        {
            m_Leases -= 1;
            if (m_Leases <= 0)
                Deallocate();
        }

        #endregion
    }

    public static class LendableHelper
    {
        #region Classes

        public class Just<TValue> : ILendable<TValue>
        {
            private readonly TValue m_Value;

            public Just(TValue value)
            {
                m_Value = value;
            }

            public ILease<TValue> GetLease()
            {
                return LeaseHelper.Return(m_Value);
            }
        }

        public class Deferred<TOutput> : Lendable<TOutput>
        {
            private readonly Func<ILease<TOutput>> m_Func;

            public Deferred(Func<ILease<TOutput>> func)
            {
                m_Func = func;
            }

            #region ILendable Implementation

            private ILease<TOutput> m_Lease;

            protected override TOutput Value { get { return m_Lease.Value; } }

            protected override void Allocate()
            {
                m_Lease = m_Func();
            }

            protected override void Deallocate()
            {
                m_Lease.Dispose();
            }

            #endregion
        }

        private class Mapped<TInput, TOutput> : Deferred<TOutput>
        {
            public Mapped(ILendable<TInput> lendable, Func<ILease<TInput>, ILease<TOutput>> func)
                : base(() => func(lendable.GetLease()))
            { }
        }

        private class Bound<TInput, TOutput> : Mapped<TInput, TOutput>
        {
            public Bound(ILendable<TInput> lendable, Func<ILease<TInput>, ILease<ILendable<TOutput>>> func) 
                : base(lendable, lease => func(lease).Bind(x => x.GetLease()))
            { }
        }

        private class Folded<TValue> : Deferred<IReadOnlyList<TValue>>
        {
            public Folded(IReadOnlyList<ILendable<TValue>> lendables)
                : base(() => lendables.Select(x => x.GetLease()).Fold())
            { }
        }

        #endregion

        #region Extension Methods

        public static void Extract<A>(this ILendable<A> lendable, Action<A> callback)
        {
            lendable.GetLease().Extract(callback);
        }

        public static B Extract<A, B>(this ILendable<A> lendable, Func<A, B> callback)
        {
            return lendable.GetLease().Extract(callback);
        }

        public static ILendable<IReadOnlyList<A>> Fold<A>(this IEnumerable<ILendable<A>> lendables)
        {
            return new Folded<A>(lendables.ToList());
        }

        #endregion

        #region Monad Implementation

        public static ILendable<A> Return<A>(A value)
        {
            return new Just<A>(value);
        }

        public static ILendable<B> BindLease<A, B>(this ILendable<A> lendable, Func<ILease<A>, ILease<ILendable<B>>> f)
        {
            return new Bound<A, B>(lendable, f);
        }

        public static ILendable<B> MapLease<A, B>(this ILendable<A> lendable, Func<ILease<A>, ILease<B>> f)
        {
            return new Mapped<A, B>(lendable, f);
        }

        public static ILendable<B> Bind<A, B>(this ILendable<A> lendable, Func<A, ILendable<B>> f)
        {
            return new Bound<A, B>(lendable, x => x.Map(f));
        }

        public static ILendable<B> Map<A, B>(this ILendable<A> lendable, Func<A, B> f)
        {
            return new Mapped<A, B>(lendable, x => x.Map(f));
        }

        #endregion

        #region Linq Helpers

        public static ILendable<B> Select<A, B>(this ILendable<A> lendable, Func<A, B> f)
        {
            return lendable.Map(f);
        }

        public static ILendable<B> SelectMany<A, B>(this ILendable<A> lendable, Func<A, ILendable<B>> f)
        {
            return lendable.Bind(f);
        }

        public static ILendable<C> SelectMany<A, B, C>(this ILendable<A> lendable, Func<A, ILendable<B>> bind, Func<A, B, C> select)
        {
            return lendable
                .Bind((a) => bind(a)
                .Map ((b) => select(a, b)));
        }

        #endregion

        #region Makers

        public static ILendable<A> MakeLazy<A>(this ILendable<A> lendable)
        {
            return new Deferred<A>(() => new Lazy<A>(() => lendable.GetLease()));
        }

        public static ILendable<A> MakeDeferred<A>(this Func<A> f)
        {
            return new Deferred<A>(() => LeaseHelper.Return(f()));
        }

        #endregion
    }
}