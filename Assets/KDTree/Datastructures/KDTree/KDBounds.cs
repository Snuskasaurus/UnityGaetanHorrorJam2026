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

using UnityEngine;

namespace DataStructures.ViliWonka.KDTree {

    // 2D bounds on the ground plane: x = world X, y = world Z
    public struct KDBounds {

        public Vector2 min;
        public Vector2 max;

        public Vector2 size {

            get {
                return max - min;
            }
        }

        // returns unity bounds, flat on the XZ plane
        public Bounds Bounds {

            get {
                Vector2 center = (min + max) / 2;
                Vector2 extent = max - min;
                return new Bounds(
                    new Vector3(center.x, 0f, center.y),
                    new Vector3(extent.x, 0f, extent.y)
                );
            }
        }


        public Vector2 ClosestPoint(Vector2 point) {

            // X axis
            if(point.x < min.x) point.x = min.x;
            else
            if(point.x > max.x) point.x = max.x;

            // Y axis (world Z)
            if(point.y < min.y) point.y = min.y;
            else
            if(point.y > max.y) point.y = max.y;

            return point;
        }
    }
}
