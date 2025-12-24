using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public static class RectangleExtensions
    {
        public static Rect Shift(this Rect rect, Vector2 arg) => new Rect(rect.x + arg.x, rect.y + arg.y, rect.width, rect.height);

        public static Rect Resize(this Rect rect, float mult)
        {
            return new Rect(rect.x * mult, rect.y * mult, rect.width * mult, rect.height * mult);
        }

        public static Rect GetIntersection(this Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMax = Mathf.Min(a.yMax, b.yMax);

            if (xMax >= xMin && yMax >= yMin)
            {
                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            }
            else
            {
                return new Rect();
            }
        }

        public static Vector3[] GetCorners(this Bounds bounds)
        {
            return new Vector3[]
            {
                new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.max.z),
            };
        }
    }
}
