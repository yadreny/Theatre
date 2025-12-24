using System;
using UnityEngine;

namespace AlSo
{
    public static class IntExt
    {
        public static int Sign(this int f) => Math.Sign(f);
    }

    public static class FloatExt
    {
        public static bool IsValidFloat(this float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static int Clamp(this int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static int Sign(this float f)=> Math.Sign(f);
        public static int ToInt(this float f) => (int)f;
        public static float ToFloat(this double d) => Convert.ToSingle(d);
        public static int ToMilliseconds(this float f) => (int)(f * 1000);


        public static float Round(this float value, int digits)
        {
            float mult = Mathf.Pow(10, digits);
            return Mathf.Round(value * mult) / mult;
        }
    }

    public static class Vector2Ext
    {
        public static Vector3Int As3d(this Vector2Int c, int z = 0) => new Vector3Int(c.x, c.y, z);

        public static Vector3 As3d(this Vector2 self, float y=0) => new Vector3(self.x, y, self.y);

        public static Vector2 As2d(this Vector3 self) => new Vector2(self.x, self.z);

        public static Vector3 ModifyY(this Vector3 self, float y)
        {
            self.y = y;
            return self;
        }
        public static int ToInt(double x) => (int)x;

        public static float AngleModuleLess180(this float value)
        {
            value = value % 360;
            if (value > 180) value = value - 360;
            if (value < -180) value = value + 360;
            return value;
        }


        public static Vector2 Lerp(this float value, Vector2 min, Vector2 max) => Vector2.Lerp(min, max, value.Clamp(0, 1));

        public static float Lerp(this float value, float min = 0, float max = 1) => Mathf.Lerp(min, max, value.Clamp(0,1));

        //public static int Clamp(this int value, int min, int max)
        //{
        //    if (value < min) return min;
        //    if (value > max) return max;    
        //    return value;
        //}
        
        public static float Clamp(this float value, float min=0, float max=1) => Mathf.Clamp(value, min, max);

        public static float ClampAbs(this float value, float min = 0, float max = 1)
            => Mathf.Abs(value).Clamp(min, max) * Mathf.Sign(value);

        public static Vector2 ClampAbs(this Vector2 value, float min = 0, float max = 1) 
            => new Vector2(value.x.ClampAbs(min, max),  value.y.ClampAbs(min, max) );   

        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static float GetAngleRelativeToCenter(this Vector2 rotationCenter, Vector2 point)
        {
            Vector2 d = point - rotationCenter;
            float r = Mathf.Atan2(d.y, d.x);
            float res = r * Mathf.Rad2Deg;
            //Debug.LogError($"{r} : {res}");
            return res;
        }

        public static Quaternion AngleToQuaternion(this float angle, float adder = 0) => Quaternion.Euler(new Vector3(0, angle + adder, 0));
    }
}
