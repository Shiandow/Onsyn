// Copyright (c) 2018 Shiandow
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Onsyn.Merging
{
    public interface IMergeable<in T>
    {
        void MergeWith(T data);
    }

    public static class MergeableHelper
    {
        private struct Mapped<A,B> : IMergeable<A>
        {
            private readonly IMergeable<B> m_Mergeable;
            private readonly Func<A,B> m_Func;

            public Mapped(IMergeable<B> mergeable, Func<A,B> func)
            {
                m_Mergeable = mergeable;
                m_Func = func;
            }

            public void MergeWith(A data)
            {
                m_Mergeable.MergeWith(m_Func(data));
            }
        }

        public static void Add<T>(this IMergeable<IEnumerable<T>> mergeable, T data)
        {
            mergeable.MergeWith(new[] { data });
        }

        public static IMergeable<A> Map<A,B>(IMergeable<B> mergeable, Func<A, B> func)
        {
            return new Mapped<A, B>(mergeable, func);
        }
    }

    public class MergeableCollection<TValue> : Collection<TValue>, IMergeable<IEnumerable<TValue>>
    {
        public void MergeWith(IEnumerable<TValue> data)
        {
            foreach (var x in data) Add(x);
        }
    }
}
