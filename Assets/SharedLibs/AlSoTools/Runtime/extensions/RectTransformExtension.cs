using System;

namespace UnityEngine.UI
{
    public static class RectTransformExtension
    {
        public static void Stretch(this RectTransform rt)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void SetPaddings(this RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void SetAbsoluteFlashPosition(this GameObject obj, Vector2 v)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) throw new Exception("rect is null");
            rect.SetAbsoluteFlashPosition(v);
        }

        public static void SetAbsoluteFlashPosition(this RectTransform rect, Vector2 v)
        {
            rect.transform.position = new Vector2(v.x, Screen.height - v.y);
        }

        public static void SetRelativeFlashPosition(this GameObject obj, Vector2 v)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) throw new Exception("rect is null");
            rect.SetRelativeFlashPosition(v);
        }

        public static void SetRelativeFlashPosition(this RectTransform obj, Vector2 v)
        {
            GameObject parent = obj.transform.parent.gameObject;
            if (parent == null) Debug.LogError($"{obj.name} has no parent");

            RectTransform parentRect = obj.transform.parent.GetComponent<RectTransform>();
            if (parentRect == null) throw new Exception($"{obj.transform.parent} has no RectTransfrom");

            Vector2 parentLu = parentRect.AbsoluteBeginPositionFlash();
            Vector2 res = new Vector2(parentLu.x + v.x, parentLu.y + v.y);
            obj.SetAbsoluteFlashPosition(res);
        }

        public static RectTransform GetRect(this MonoBehaviour monoBehaviour)
            => monoBehaviour.GetComponent<RectTransform>();

        public static void SetPosition(this RectTransform rect, Vector2 v)
        {
            if (rect == null) throw new Exception("rect is null");
            if (rect.transform.parent == null) throw new Exception($"{rect.name} parent is null");
            rect.position = rect.transform.parent.transform.position + new Vector3(v.x, -v.y, 0);
        }

        public static void SetWidth(this RectTransform rect, float x)
        {
            if (rect == null) throw new Exception("rect is null");
            rect.sizeDelta = new Vector2(x, rect.sizeDelta.y);
        }

        public static void SetHeight(this RectTransform rect, float y)
        {
            if (rect == null) throw new Exception("rect is null");
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, y);
        }

        public static void SetPosition(this GameObject obj, Vector2 v)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) throw new Exception("rect is null");
            rect.SetPosition(v);
        }

        public static Vector2 Size(this RectTransform rect)
        {
            if (rect == null) throw new Exception("rect is null");
            return new Vector2(rect.rect.width, rect.rect.height);
        }

        public static Vector2 RelativeMousePosition(this RectTransform rect)
        {
            return rect.AbsoluteMousePositionFlash() - rect.AbsoluteBeginPositionFlash();

        }


        public static Vector2 AbsoluteMousePositionFlash(this RectTransform rect)
        {
            return new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        }

        public static Vector2 RelativeBeginPositionFlash(this RectTransform rect)
        {
            return AbsoluteBeginPositionFlash(rect) - AbsoluteBeginPositionFlash(rect.parent as RectTransform);
        }

        public static Vector2 AbsoluteBeginPositionFlash(this RectTransform rect)
        {
            Vector2 v = AbsoluteBeginPositionUnity(rect);
            return new Vector2(v.x, Screen.height + v.y);
        }

        public static Vector2 AbsoluteBeginPositionUnity(this RectTransform rect)
        {
            Vector3[] vs = new Vector3[4];
            rect.GetWorldCorners(vs);

            float x = float.MaxValue;
            float y = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                Vector3 v = vs[i];
                if (v.x < x) x = v.x;
                if (v.y > y) y = v.y;
            }

            return new Vector2(x, -y);
        }

        public static Rect Bounds(this RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Rect bounds = new Rect(corners[0], Vector2.zero);
            for (int i = 1; i < corners.Length; i++)
            {
                bounds.xMin = Mathf.Min(bounds.xMin, corners[i].x);
                bounds.xMax = Mathf.Max(bounds.xMax, corners[i].x);
                bounds.yMin = Mathf.Min(bounds.yMin, corners[i].y);
                bounds.yMax = Mathf.Max(bounds.yMax, corners[i].y);
            }

            Rect result = new Rect(bounds.x, Screen.height - bounds.y - bounds.height, bounds.width, bounds.height);
            return result;
        }

        public static Vector2 RelativeMousePositionUnity(this RectTransform rect)
        {
            return AbsoluteMousePositionUnity() - rect.AbsoluteBeginPositionUnity();
        }

        public static Vector2 RelativeMousePositionFlash(this RectTransform rect)
        {
            return rect.AbsoluteMousePositionFlash() - rect.AbsoluteBeginPositionFlash();
        }

        public static Vector2 AbsoluteMousePositionUnity()
        {
            return new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        }

        public static Vector2 LocalLu(this RectTransform child)
        {
            return new Vector2(child.rect.x, -child.rect.y);
        }

        public static Vector2 LocalRd(this RectTransform child)
        {
            return new Vector2(child.rect.x + child.rect.width, -child.rect.y + child.rect.height);
        }

        public static bool IsUnderMouse(this RectTransform rect, float canvasScale)
        {
            Vector2 mouse = rect.AbsoluteMousePositionFlash();
            Vector2 lu = rect.AbsoluteBeginPositionFlash();
            if (mouse.x < lu.x || mouse.y < lu.y) return false;
            if (mouse.x > lu.x + rect.rect.width * canvasScale || mouse.y > lu.y + rect.rect.height * canvasScale) return false;
            return true;
        }
    }
}