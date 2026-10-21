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

using System.Collections.Generic;
using UnityEngine;

namespace DataStructures.ViliWonka.KDTree {

    public partial class KDQuery {

        // Interval on the XZ plane, the heights of min and max are ignored
        public void Interval(KDTree tree, Vector3 min, Vector3 max, List<int> resultIndices) {
            Interval(tree, KDTree.ToXZ(min), KDTree.ToXZ(max), resultIndices);
        }

        public void Interval(KDTree tree, Vector2 min, Vector2 max, List<int> resultIndices) {

            Reset();

            Vector2[] points = tree.Points2D;
            int[] permutation = tree.Permutation;

            var rootNode = tree.RootNode;

            PushToQueue(

                rootNode,
                rootNode.bounds.ClosestPoint((min + max) / 2)
            );

            KDQueryNode queryNode = null;
            KDNode node = null;


            // KD search with pruning (don't visit areas which distance is more away than range)
            // Recursion done on Stack
            while(LeftToProcess > 0) {

                queryNode = PopFromQueue();
                node = queryNode.node;

                if(!node.Leaf) {

                    int partitionAxis = node.partitionAxis;
                    float partitionCoord = node.partitionCoordinate;

                    Vector2 tempClosestPoint = queryNode.tempClosestPoint;

                    if((KDTree.GetAxis(tempClosestPoint, partitionAxis) - partitionCoord) < 0) {

                        // we already know we are inside negative bound/node,
                        // so we don't need to test for distance
                        PushToQueue(node.negativeChild, tempClosestPoint);

                        KDTree.SetAxis(ref tempClosestPoint, partitionAxis, partitionCoord);

                        // testing other side
                        if(node.positiveChild.Count != 0
                        && partitionCoord <= KDTree.GetAxis(max, partitionAxis)) {

                            PushToQueue(node.positiveChild, tempClosestPoint);
                        }
                    }
                    else {

                        // we already know we are inside positive bound/node,
                        // so we don't need to test for distance
                        PushToQueue(node.positiveChild, tempClosestPoint);

                        // project the tempClosestPoint to other bound
                        KDTree.SetAxis(ref tempClosestPoint, partitionAxis, partitionCoord);

                        // testing other side
                        if(node.negativeChild.Count != 0
                        && partitionCoord >= KDTree.GetAxis(min, partitionAxis)) {

                            PushToQueue(node.negativeChild, tempClosestPoint);
                        }
                    }
                }
                else {

                    // LEAF

                    // testing if node bounds are inside the query interval
                    if(node.bounds.min.x >= min.x
                    && node.bounds.min.y >= min.y

                    && node.bounds.max.x <= max.x
                    && node.bounds.max.y <= max.y) {

                        for(int i = node.start; i < node.end; i++) {

                            resultIndices.Add(permutation[i]);
                        }

                    }
                    // node is not inside query interval, need to do test on each point separately
                    else {

                        for(int i = node.start; i < node.end; i++) {

                            int index = permutation[i];

                            Vector2 v = points[index];

                            if(v.x >= min.x
                            && v.y >= min.y

                            && v.x <= max.x
                            && v.y <= max.y) {

                                resultIndices.Add(index);
                            }
                        }
                    }

                }
            }
        }
    }

}
