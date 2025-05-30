﻿using System.Collections.Generic;
using KdTree;
using UnityEngine;    

namespace MightyTerrainMesh
{
    public class SampleVertexData
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
        public void Merge(SampleVertexData other)
        {
            Position = 0.5f * (Position + other.Position);
            Normal = 0.5f * (Normal + other.Normal);
            UV = 0.5f * (UV + other.UV);
        }
    }

    public interface ITerrainTreeScaner
    {
        void Run(Vector3 center, out Vector3 hitpos, out Vector3 hitnormal);
    }

    public abstract class SamplerBase
    {
        public virtual void RunSample(ITerrainTreeScaner scaner)
        {
            scaner.Run(mVertex.Position, out mVertex.Position, out mVertex.Normal);
        }
        protected SampleVertexData mVertex;
        //每个node都有这么一个字典
        public Dictionary<byte, SampleVertexData> Boundaries = new Dictionary<byte, SampleVertexData>();
        public abstract Vector3 Pos { get; }
        public abstract void GetData(List<SampleVertexData> lPos, Dictionary<byte, List<SampleVertexData>> bd);
        public abstract void AddBoundary(int subdivision, int x, int z, byte bk, SampleVertexData vert);
    }
    public class SamplerLeaf : SamplerBase
    {
        public override Vector3 Pos { get { return mVertex != null ? mVertex.Position : Vector3.zero; } }
        public Vector3 Normal { get { return mVertex != null ? mVertex.Normal : Vector3.up; } }
        public Vector2 UV { get { return mVertex != null ? mVertex.UV : Vector2.zero; } }
        public SamplerLeaf(Vector3 center, Vector2 uv)
        {
            mVertex = new SampleVertexData();
            mVertex.Position = center;
            mVertex.UV = uv;
        }
        public SamplerLeaf(SampleVertexData vert)
        {
            mVertex = vert;
        }
        public override void GetData(List<SampleVertexData> lData, Dictionary<byte, List<SampleVertexData>> bd)
        {
            lData.Add(mVertex);
            foreach(var k in Boundaries.Keys)
            {
                if (!bd.ContainsKey(k))
                    bd.Add(k, new List<SampleVertexData>());
                bd[k].Add(Boundaries[k]);
            }
        }
        public override void AddBoundary(int subdivision, int x, int z, byte bk, SampleVertexData vert)
        {
            Boundaries.Add(bk, vert);
        }
    }
    /// <summary>
    /// 每块一个Node
    /// </summary>
    public class SamplerNode : SamplerBase
    {
        public override Vector3 Pos { get { return mVertex != null ? mVertex.Position : Vector3.zero; } }
        public SamplerBase[] Children = new SamplerBase[4]; //对每块的内部再进行四叉树分割
        public bool isFullLeaf  //叶子节点的上一层节点
        {
            get
            {
                for (int i = 0; i < Children.Length; ++i)
                {
                    if (Children[i] == null || !(Children[i] is SamplerLeaf))   
                        return false;
                }
                return true;
            }
        }
        //
        protected SamplerNode() { }
        //build a full tree
        public SamplerNode(int sub, Vector3 center, Vector2 size, Vector2 uv, Vector2 uvstep)
        {
            mVertex = new SampleVertexData();
            mVertex.Position = center;
            mVertex.UV = uv;
            Vector2 subsize = 0.5f * size;
            Vector2 subuvstep = 0.5f * uvstep;
            if (sub > 1)
            {
                Children[0] = new SamplerNode(sub - 1,
                    new Vector3(center.x - 0.5f * subsize.x, center.y, center.z - 0.5f * subsize.y), subsize,
                    new Vector2(uv.x - 0.5f * subuvstep.x, uv.y - 0.5f * subuvstep.y), subuvstep);
                Children[1] = new SamplerNode(sub - 1,
                    new Vector3(center.x + 0.5f * subsize.x, center.y, center.z - 0.5f * subsize.y), subsize,
                    new Vector2(uv.x + 0.5f * subuvstep.x, uv.y - 0.5f * subuvstep.y), subuvstep);
                Children[2] = new SamplerNode(sub - 1,
                   new Vector3(center.x - 0.5f * subsize.x, center.y, center.z + 0.5f * subsize.y), subsize,
                   new Vector2(uv.x - 0.5f * subuvstep.x, uv.y + 0.5f * subuvstep.y), subuvstep);
                Children[3] = new SamplerNode(sub - 1,
                    new Vector3(center.x + 0.5f * subsize.x, center.y, center.z + 0.5f * subsize.y), subsize,
                    new Vector2(uv.x + 0.5f * subuvstep.x, uv.y + 0.5f * subuvstep.y), subuvstep);               
            }
            else
            {
                Children[0] = new SamplerLeaf(new Vector3(center.x - 0.5f * subsize.x, center.y, center.z - 0.5f * subsize.y),
                    new Vector2(uv.x - 0.5f * subuvstep.x, uv.y - 0.5f * subuvstep.y));
                Children[1] = new SamplerLeaf(new Vector3(center.x + 0.5f * subsize.x, center.y, center.z - 0.5f * subsize.y),
                    new Vector2(uv.x + 0.5f * subuvstep.x, uv.y - 0.5f * subuvstep.y));
                Children[2] = new SamplerLeaf(new Vector3(center.x - 0.5f * subsize.x, center.y, center.z + 0.5f * subsize.y),
                    new Vector2(uv.x - 0.5f * subuvstep.x, uv.y + 0.5f * subuvstep.y));
                Children[3] = new SamplerLeaf(new Vector3(center.x + 0.5f * subsize.x, center.y, center.z + 0.5f * subsize.y),
                    new Vector2(uv.x + 0.5f * subuvstep.x, uv.y + 0.5f * subuvstep.y));
            }
        }
        public override void GetData(List<SampleVertexData> lData, Dictionary<byte, List<SampleVertexData>> bd)
        {
            for (int i = 0; i < 4; ++i)
            {
                Children[i].GetData(lData, bd);
            }
            foreach (var k in Boundaries.Keys)
            {
                if (!bd.ContainsKey(k))
                    bd.Add(k, new List<SampleVertexData>());
                bd[k].Add(Boundaries[k]);
            }
        }
        public override void RunSample(ITerrainTreeScaner scaner)
        {
            base.RunSample(scaner);
            for (int i = 0; i < 4; ++i)
            {
                Children[i].RunSample(scaner);
            }
        }
        public override void AddBoundary(int subdivision, int x, int z, byte bk, SampleVertexData point)
        {
            //first grade
            int u = x >> subdivision; // x / power(2, subdivision);
            int v = z >> subdivision;
            int subx = x - u * (1 << subdivision);
            int subz = z - v * (1 << subdivision);
            --subdivision;
            int idx = (subz >> subdivision) * 2 + (subx >> subdivision);
            Children[idx].AddBoundary(subdivision, subx, subz, bk, point);
        }
        
        public SamplerLeaf Combine(float angleErr)
        {
            for (int i = 0; i < Children.Length; ++i)
            {
                if (Children[i] == null || !(Children[i] is SamplerLeaf))
                    return null;
            }
            for (int i = 0; i < Children.Length; ++i)
            {
                SamplerLeaf l = (SamplerLeaf)Children[i];
                float dot = Vector3.Dot(l.Normal.normalized, mVertex.Normal.normalized);
                if (Mathf.Rad2Deg * Mathf.Acos(dot) >= angleErr)
                    return null;
            }
            //走到这里代表：父节点采样点的法线与4个子节点法线之间夹角都小于我们设定的容差
            SamplerLeaf leaf = new SamplerLeaf(mVertex);
            for (int i = 0; i < Children.Length; ++i)
            {
                SamplerLeaf l = (SamplerLeaf)Children[i];
                foreach (var k in l.Boundaries.Keys)
                {
                    if (Boundaries.ContainsKey(k))
                        Boundaries[k].Merge(l.Boundaries[k]);
                    else
                        Boundaries.Add(k, l.Boundaries[k]);
                }
            }
            leaf.Boundaries = Boundaries;
            return leaf;
        }
        public void CombineNode(float angleErr) //由于有合并叶子节点的操作，所以最后的四叉树不一定是一颗完全四叉树
        {
            for (int i = 0; i < 4; ++i)
            {
                if (Children[i] is SamplerNode)
                {
                    SamplerNode subNode = (SamplerNode)Children[i];
                    subNode.CombineNode(angleErr);
                    if (subNode.isFullLeaf)
                    {
                        SamplerLeaf replacedLeaf = subNode.Combine(angleErr);
                        if (replacedLeaf != null)
                            Children[i] = replacedLeaf;
                    }
                }
            }
        }
    }
    /// <summary>
    /// 每块一个SamplerTree,一共4x4块
    /// </summary>
    public class SamplerTree
    {
        public const byte LBCorner = 0;     //byte：取值范围是从 0 到 255
        public const byte LTCorner = 1;
        public const byte RTCorner = 2;
        public const byte RBCorner = 3;
        public const byte BBorder = 4;
        public const byte TBorder = 5;
        public const byte LBorder = 6;
        public const byte RBorder = 7;
        private SamplerBase mNode;
        public List<SampleVertexData> Vertices = new List<SampleVertexData>();
        public Dictionary<byte, List<SampleVertexData>> Boundaries = 
            new Dictionary<byte, List<SampleVertexData>>();
        public HashSet<byte> StitchedBorders = new HashSet<byte>();
        public Vector3 Center { get { return mNode.Pos; } }
        public Bounds BND { get; set; }
        public Vector2 uvMin = Vector2.zero;
        public Vector2 uvMax = Vector2.one;
        private Dictionary<byte, KdTree<float, int>> BoundaryKDTree = new Dictionary<byte, KdTree<float, int>>();
        public SamplerTree(int sub, Vector3 center, Vector2 size, Vector2 uv, Vector2 uvstep)
        {
            mNode = new SamplerNode(sub, center, size, uv, uvstep);
            uvMin = uv - 0.5f * uvstep;
            uvMax = uv + 0.5f * uvstep;
        }
        private void CombineTree(float angleErr)
        {
            if (mNode is SamplerNode)
            {
                SamplerNode node = (SamplerNode)mNode;
                node.CombineNode(angleErr);
                if (node.isFullLeaf)    //因为对每块进行了三次细分，所以这段代码实际调用不到，细分一次时可以调到
                {
                    SamplerLeaf leaf = node.Combine(angleErr);
                    if (leaf != null)
                        mNode = leaf;
                }
            }
        }
        public void AddBoundary(int subdivision, int x, int z, byte bk, SampleVertexData vert)
        {
            if (mNode is SamplerNode)
            {
                SamplerNode node = (SamplerNode)mNode;
                node.AddBoundary(subdivision, x, z, bk, vert);
            }
        }
        public void InitBoundary()
        {
            for(byte flag = LBCorner; flag <= RBorder; ++flag)
            {
                Boundaries.Add(flag, new List<SampleVertexData>());
                var tree = new KdTree<float, int>(2, new KdTree.Math.FloatMath());
                BoundaryKDTree.Add(flag, tree);
            }
        }
        public void MergeBoundary(byte flag, float minDis, List<SampleVertexData> src)
        {
            if (!Boundaries.ContainsKey(flag) || !BoundaryKDTree.ContainsKey(flag))
            {
                Debug.LogError("the boundary need to merge not exists");
            }
            var tree = BoundaryKDTree[flag];
            foreach (var vt in src)
            {
                var nodes = tree.GetNearestNeighbours(new float[] { vt.Position.x, vt.Position.z }, 1);
                if (nodes != null && nodes.Length > 0)
                {
                    var dis = Vector2.Distance(new Vector2(vt.Position.x, vt.Position.z), new Vector2(nodes[0].Point[0], nodes[0].Point[1]));
                    if (dis <= minDis)
                        continue;
                }
                tree.Add(new float[] { vt.Position.x, vt.Position.z }, 0);
                Boundaries[flag].Add(vt);
            }
        }
        public void RunSampler(ITerrainTreeScaner scaner)
        {
            mNode.RunSample(scaner);
        }
        public void FillData(float angleErr)
        {
            if (angleErr > 0)
            {
                CombineTree(angleErr);
            }
            mNode.GetData(Vertices, Boundaries);
        }
        public void StitchBorder(byte flag, byte nflag, float minDis, SamplerTree neighbour)
        {
            if (neighbour == null)
                return;
            if (flag <= RBCorner || nflag <= RBCorner)
            {
                return;
            }
            if (!Boundaries.ContainsKey(flag))
            {
                MTLog.LogError("SamplerTree boundary doesn't contains corner : " + flag);
                return;
            }
            if (!neighbour.Boundaries.ContainsKey(nflag))
            {
                MTLog.LogError("SamplerTree neighbour boundary doesn't contains corner : " + nflag);
                return;
            }
            if (StitchedBorders.Contains(flag) && neighbour.StitchedBorders.Contains(nflag))
                return;
            if (Boundaries[flag].Count > neighbour.Boundaries[nflag].Count)
            {
                neighbour.Boundaries[nflag].Clear();
                neighbour.Boundaries[nflag].AddRange(Boundaries[flag]);
            }
            else
            {
                Boundaries[flag].Clear();
                Boundaries[flag].AddRange(neighbour.Boundaries[nflag]);
            }
            //
            StitchedBorders.Add(flag);
            neighbour.StitchedBorders.Add(nflag);
        }
    }
}
