/*MIT License

Copyright(c) 2018 Vili Volčini / viliwonka

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

// change to !KDTREE_DUPLICATES
// if you know for sure you will not use duplicate coordinates (all unique)
#define KDTREE_DUPLICATES

using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DataStructures.ViliWonka.KDTree {

    // 2D kd-tree: the points live on the ground plane, x = world X, y = world Z.
    // Vector3 inputs (Points, Build, queries) are projected on XZ, the height is ignored.
    public class KDTree {

        public KDNode RootNode { get; private set; }

        public Vector2[] Points2D { get { return points; } } // points on which kd-tree is built (x = world X, y = world Z)
        private Vector2[] points;

        // Optional 3D source, projected on XZ into Points2D at each Rebuild. Null when the tree was built from Vector2.
        // Kept so that code writing world positions in Points then calling Rebuild keeps working.
        public Vector3[] Points { get { return sourcePoints; } }
        private Vector3[] sourcePoints;

        public int[] Permutation { get { return permutation; } } // index aray, that will be permuted
        private int[] permutation;

        public int Count { get; private set; }

        private int maxPointsPerLeafNode = 32;

        private KDNode[] kdNodesStack;
        private int kdNodesCount = 0;

        public KDTree(int maxPointsPerLeafNode = 32) {

            Count       = 0;
            points      = new Vector2[0];
            permutation = new     int[0];

            kdNodesStack = new KDNode[64];

            this.maxPointsPerLeafNode = maxPointsPerLeafNode;
        }

        public KDTree(Vector2[] points, int maxPointsPerLeafNode = 32) {

            this.points = points;
            this.permutation = new int[points.Length];

            Count = points.Length;
            kdNodesStack = new KDNode[64];

            this.maxPointsPerLeafNode = maxPointsPerLeafNode;

            Rebuild();
        }

        // The array is kept as the 3D source: changing its values then calling Rebuild updates the tree
        public KDTree(Vector3[] points, int maxPointsPerLeafNode = 32) {

            this.sourcePoints = points;
            this.points = new Vector2[points.Length];
            this.permutation = new int[points.Length];

            Count = points.Length;
            kdNodesStack = new KDNode[64];

            this.maxPointsPerLeafNode = maxPointsPerLeafNode;

            Rebuild();
        }

        public void Build(Vector2[] newPoints, int maxPointsPerLeafNode = -1) {

            sourcePoints = null;
            SetCount(newPoints.Length);

            Array.Copy(newPoints, points, Count);

            Rebuild(maxPointsPerLeafNode);
        }

        public void Build(List<Vector2> newPoints, int maxPointsPerLeafNode = -1) {

            sourcePoints = null;
            SetCount(newPoints.Count);

            newPoints.CopyTo(0, points, 0, Count);

            Rebuild(maxPointsPerLeafNode);
        }

        public void Build(Vector3[] newPoints, int maxPointsPerLeafNode = -1) {

            EnsureSourcePoints(newPoints.Length);
            SetCount(newPoints.Length);

            Array.Copy(newPoints, sourcePoints, Count);

            Rebuild(maxPointsPerLeafNode);
        }

        public void Build(List<Vector3> newPoints, int maxPointsPerLeafNode = -1) {

            EnsureSourcePoints(newPoints.Count);
            SetCount(newPoints.Count);

            newPoints.CopyTo(0, sourcePoints, 0, Count);

            Rebuild(maxPointsPerLeafNode);
        }

        public void Rebuild(int maxPointsPerLeafNode = -1) {

            if(sourcePoints != null) {
                for(int i = 0; i < Count; i++) {
                    points[i] = ToXZ(sourcePoints[i]);
                }
            }

            for(int i = 0; i < Count; i++) {
                permutation[i] = i;
            }

            if(maxPointsPerLeafNode > 0) {
                this.maxPointsPerLeafNode = maxPointsPerLeafNode;
            }

            BuildTree();
        }

        public void SetCount(int newSize) {

            Count = newSize;
            // upsize internal arrays
            if(Count > points.Length) {

                Array.Resize(ref points,        Count);
                Array.Resize(ref permutation,   Count);
            }

            if(sourcePoints != null && Count > sourcePoints.Length) {
                Array.Resize(ref sourcePoints, Count);
            }
        }

        // World position to the tree plane
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToXZ(Vector3 v) {
            return new Vector2(v.x, v.z);
        }

        // Faster than the Vector2 indexer, which checks the index and throws
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetAxis(Vector2 v, int axis) {
            return axis == 0 ? v.x : v.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetAxis(ref Vector2 v, int axis, float value) {
            if(axis == 0) v.x = value;
            else          v.y = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float SqrDistance(Vector2 a, Vector2 b) {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        void EnsureSourcePoints(int size) {

            if(sourcePoints == null) {
                sourcePoints = new Vector3[Math.Max(size, points.Length)];
            }
        }

        void BuildTree() {

            ResetKDNodeStack();

            RootNode = GetKDNode();
            RootNode.bounds = MakeBounds();
            RootNode.start = 0;
            RootNode.end = Count;

            SplitNode(RootNode);
        }

        KDNode GetKDNode() {

            KDNode node = null;

            if(kdNodesCount < kdNodesStack.Length) {

                if(kdNodesStack[kdNodesCount] == null) {
                    kdNodesStack[kdNodesCount] = node = new KDNode();
                }
                else {
                    node = kdNodesStack[kdNodesCount];
                    node.partitionAxis = -1;
                }
            }
            else {

                // automatic resize of KDNode pool array
                Array.Resize(ref kdNodesStack, kdNodesStack.Length * 2);
                node = kdNodesStack[kdNodesCount] = new KDNode();
            }

            kdNodesCount++;

            return node;
        }

        void ResetKDNodeStack() {
            kdNodesCount = 0;
        }

        /// <summary>
        /// For calculating root node bounds
        /// </summary>
        /// <returns>Boundary of all points</returns>
        KDBounds MakeBounds() {

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for(int i = 0; i < Count; i++) {

                Vector2 p = points[i];

                if(p.x < min.x) min.x = p.x;
                if(p.x > max.x) max.x = p.x;
                if(p.y < min.y) min.y = p.y;
                if(p.y > max.y) max.y = p.y;
            }

            KDBounds b = new KDBounds();
            b.min = min;
            b.max = max;

            return b;
        }

        /// <summary>
        /// Recursive splitting procedure
        /// </summary>
        /// <param name="parent">This is where root node goes</param>
        void SplitNode(KDNode parent) {

            KDBounds parentBounds = parent.bounds;
            Vector2 parentBoundsSize = parentBounds.size;

            // Find axis where bounds are largest
            int splitAxis = parentBoundsSize.x >= parentBoundsSize.y ? 0 : 1;

            // Our axis min-max bounds
            float boundsStart = GetAxis(parentBounds.min, splitAxis);
            float boundsEnd   = GetAxis(parentBounds.max, splitAxis);

            // Calculate the spliting coords
            float splitPivot = CalculatePivot(parent.start, parent.end, boundsStart, boundsEnd, splitAxis);

            parent.partitionAxis = splitAxis;
            parent.partitionCoordinate = splitPivot;

            // 'Spliting' array to two subarrays
            int splittingIndex = Partition(parent.start, parent.end, splitPivot, splitAxis);

            // Negative / Left node
            Vector2 negMax = parentBounds.max;
            SetAxis(ref negMax, splitAxis, splitPivot);

            KDNode negNode = GetKDNode();
            negNode.bounds = parentBounds;
            negNode.bounds.max = negMax;
            negNode.start = parent.start;
            negNode.end = splittingIndex;
            parent.negativeChild = negNode;

            // Positive / Right node
            Vector2 posMin = parentBounds.min;
            SetAxis(ref posMin, splitAxis, splitPivot);

            KDNode posNode = GetKDNode();
            posNode.bounds = parentBounds;
            posNode.bounds.min = posMin;
            posNode.start = splittingIndex;
            posNode.end = parent.end;
            parent.positiveChild = posNode;

            // check if we are actually splitting it anything
            // this if check enables duplicate coordinates, but makes construction a bit slower
#if KDTREE_DUPLICATES
            if(negNode.Count != 0 && posNode.Count != 0) {
            #endif
                // Constraint function deciding if split should be continued
                if(ContinueSplit(negNode))
                    SplitNode(negNode);


                if(ContinueSplit(posNode))
                    SplitNode(posNode);

#if KDTREE_DUPLICATES
            }
#endif
        }

        /// <summary>
        /// Sliding midpoint splitting pivot calculation
        /// 1. First splits node to two equal parts (midPoint)
        /// 2. Checks if elements are in both sides of splitted bounds
        /// 3a. If they are, just return midPoint
        /// 3b. If they are not, then points are only on left or right bound.
        /// 4. Move the splitting pivot so that it shrinks part with points completely (calculate min or max dependent) and return.
        /// </summary>
        float CalculatePivot(int start, int end, float boundsStart, float boundsEnd, int axis) {

            //! sliding midpoint rule
            float midPoint = (boundsStart + boundsEnd) / 2f;

            bool negative = false;
            bool positive = false;

            float negMax = Single.MinValue;
            float posMin = Single.MaxValue;

            // this for loop section is used both for sorted and unsorted data
            for (int i = start; i < end; i++) {

                if (GetAxis(points[permutation[i]], axis) < midPoint)
                    negative = true;
                else
                    positive = true;

                if (negative == true && positive == true)
                    return midPoint;
            }

            if (negative) {

                for (int i = start; i < end; i++) {
                    float value = GetAxis(points[permutation[i]], axis);
                    if (negMax < value)
                        negMax = value;
                }

                return negMax;
            }
            else {

                for (int i = start; i < end; i++) {
                    float value = GetAxis(points[permutation[i]], axis);
                    if (posMin > value)
                        posMin = value;
                }

                return posMin;
            }
        }

        /// <summary>
        /// Similar to Hoare partitioning algorithm (used in Quick Sort)
        /// Modification: pivot is not left-most element but is instead argument of function
        /// Calculates splitting index and partially sorts elements (swaps them until they are on correct side - depending on pivot)
        /// Complexity: O(n)
        /// </summary>
        /// <returns>
        /// Returns splitting index that subdivides array into 2 smaller arrays
        /// left = [start, pivot),
        /// right = [pivot, end)
        /// </returns>
        int Partition(int start, int end, float partitionPivot, int axis) {

            // note: increasing right pointer is actually decreasing!
            int LP = start - 1; // left pointer (negative side)
            int RP = end;       // right pointer (positive side)

            int temp;           // temporary var for swapping permutation indexes

            while (true) {

                do {
                    // move from left to the right until "out of bounds" value is found
                    LP++;
                }
                while (LP < RP && GetAxis(points[permutation[LP]], axis) < partitionPivot);

                do {
                    // move from right to the left until "out of bounds" value found
                    RP--;
                }
                while (LP < RP && GetAxis(points[permutation[RP]], axis) >= partitionPivot);

                if (LP < RP) {
                                // swap
                               temp = permutation[LP];
                    permutation[LP] = permutation[RP];
                    permutation[RP] = temp;
                }
                else {

                    return LP;
                }
            }
        }

        /// <summary>
        /// Constraint function. You can add custom constraints here - if you have some other data/classes binded to the points
        /// </summary>
        bool ContinueSplit(KDNode node) {

            return (node.Count > maxPointsPerLeafNode);
        }
    }
}
