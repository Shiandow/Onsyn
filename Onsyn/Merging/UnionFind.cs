// Copyright (c) 2018 Shiandow
using System;
using System.Collections.Generic;

namespace Onsyn.Merging
{
    public interface IUnionFind<TData> : IMergeable<IUnionFind<TData>>, IMergeable<TData>
    {
        IUnionFind<TData> Root { get; }
        int Depth { get; }

        TData Data { get; }
    }

    public class UnionFind<IData, TData> : IUnionFind<IData>
        where TData : class, IData, IMergeable<IData>, new()
    {
        // Union-find structure augmented with data

        public UnionFind() { m_Root = this; }

        #region IUnionFind Implementation

        int IUnionFind<IData>.Depth {get {return m_Depth;} }

        IUnionFind<IData> IUnionFind<IData>.Root { get { return Root; } }

        public IData Data { get { return (m_Root == this) ? m_Data.Value : Root.Data; } }

        #endregion

        #region IMergeable Implementation

        public void MergeWith(IData data)
        {
            if (Root != this)
                Root.MergeWith(data);
            else
                m_Data.Value.MergeWith(data);
        }

        public void MergeWith(IUnionFind<IData> tree)
        {
            if (Root != this)
                Root.MergeWith(tree.Root);
            else
                MergeWithRoot(tree.Root);
        }

        private void MergeWithRoot(IUnionFind<IData> root)
        {
            if (m_Depth < root.Depth)
                Root = root;
            else if (m_Depth > root.Depth)
                root.MergeWith(this);
            else if (root != this)
            {
                m_Depth += 1;
                root.MergeWith(this);
            }
        }

        #endregion

        #region Implementation

        private IUnionFind<IData> m_Root;
        private int m_Depth;

        private Lazy<TData> m_Data = new Lazy<TData>(() => new TData());

        private IUnionFind<IData> Root
        {
            get
            {
                if (m_Root != this)
                    m_Root = m_Root.Root;
                return m_Root;
            }

            set
            {
                if (value != m_Root && m_Data != null)
                {
                    value.MergeWith(m_Data.Value);
                    m_Data = null;
                }
                m_Root = value;
            }
        }

        #endregion
    }

    public class UnionCollection<TValue> : UnionFind<IEnumerable<TValue>, MergeableCollection<TValue>> { }
}
