using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AlSo
{
    // ---------------- DATA ----------------

    [Serializable]
    public class FloatInterval
    {
        [SerializeField] protected float min; // не используется дравером
        [SerializeField] protected float max; // не используется дравером

        public float start;
        public float end;

        public FloatInterval(float start, float end)
        {
            this.min = 0f;
            this.max = 1f;
            this.start = start;
            this.end = end;
        }

        public void SetBounds(float min, float max)
        {
            this.min = min; this.max = max;
            EnsureInBounds();
        }

        public void EnsureInBounds()
        {
            float lo = min, hi = max;
            if (hi <= lo) hi = lo + Mathf.Max(1e-4f, Mathf.Abs(lo) * 1e-4f + 1e-4f);
            start = Mathf.Clamp(start, lo, hi);
            end = Mathf.Clamp(end, lo, hi);
            if (start > end) { float mid = 0.5f * (start + end); start = mid; end = mid; }
        }

        public void Set01IfZeros()
        {
            if (Mathf.Approximately(start, 0f) && Mathf.Approximately(end, 0f))
            {
                min = 0f; max = 1f; start = 0f; end = 1f;
            }
            EnsureInBounds();
        }
    }

    public sealed class FloatIntervalRangeAttribute : PropertyAttribute
    {
        public readonly float Min;
        public readonly float Max;
        public FloatIntervalRangeAttribute(float min, float max)
        {
            Min = min; Max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class FloatIntervalOnEndDragAttribute : PropertyAttribute
    {
        public readonly string MethodName;
        public FloatIntervalOnEndDragAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }

    [CustomPropertyDrawer(typeof(FloatInterval))]
    public sealed class FloatIntervalDrawer : PropertyDrawer
    {
        const float NumW = 70f;
        const float Pad = 2f;
        const float TwoLineWidth = 360f;

        private static readonly Dictionary<int, int> s_FieldHotControl = new Dictionary<int, int>();
        private static readonly HashSet<int> s_IsDragging = new HashSet<int>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool two = EditorGUIUtility.currentViewWidth < TwoLineWidth;
            float h = EditorGUIUtility.singleLineHeight;
            return two ? h * 2f + EditorGUIUtility.standardVerticalSpacing : h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var startProp = property.FindPropertyRelative("start");
            var endProp = property.FindPropertyRelative("end");

            GetBoundsFromAttribute(out float boundsMin, out float boundsMax);

            float start = Mathf.Clamp(startProp.floatValue, boundsMin, boundsMax);
            float end = Mathf.Clamp(endProp.floatValue, boundsMin, boundsMax);

            if (Mathf.Approximately(start, 0f) && Mathf.Approximately(end, 0f))
            {
                start = 0f; end = 1f;
                startProp.floatValue = start;
                endProp.floatValue = end;
            }

            Rect content = EditorGUI.PrefixLabel(position, label);
            bool two = EditorGUIUtility.currentViewWidth < TwoLineWidth;
            float lineH = EditorGUIUtility.singleLineHeight;
            float y = content.y;

            Rect sliderRect, startRect, endRect;

            if (!two)
            {
                Rect endRectL = new Rect(content.xMax - NumW, y, NumW, lineH);
                Rect startRectL = new Rect(endRectL.x - Pad - NumW, y, NumW, lineH);
                Rect sliderRectL = new Rect(content.x, y, startRectL.x - Pad - content.x, lineH);

                sliderRect = sliderRectL; startRect = startRectL; endRect = endRectL;
            }
            else
            {
                Rect sliderRectL = new Rect(content.x, y, content.width, lineH);
                y += lineH + EditorGUIUtility.standardVerticalSpacing;
                float half = (content.width - Pad) * 0.5f;
                Rect startRectL = new Rect(content.x, y, half, lineH);
                Rect endRectL = new Rect(startRectL.xMax + Pad, y, half, lineH);

                sliderRect = sliderRectL; startRect = startRectL; endRect = endRectL;
            }

            EditorGUI.BeginChangeCheck();
            Draw(sliderRect, startRect, endRect, boundsMin, boundsMax, ref start, ref end);
            bool valueChanged = EditorGUI.EndChangeCheck();

            startProp.floatValue = start;
            endProp.floatValue = end;

            int hotNow = GUIUtility.hotControl;
            int fieldHash = property.propertyPath.GetHashCode();

            if (!s_IsDragging.Contains(fieldHash))
            {
                if (hotNow != 0 && sliderRect.Contains(Event.current.mousePosition))
                {
                    s_IsDragging.Add(fieldHash);
                    s_FieldHotControl[fieldHash] = hotNow;
                    UnityEngine.Debug.Log($"[FloatInterval] Drag START path='{property.propertyPath}', hot={hotNow}, label='{label?.text}'");
                }
            }
            else
            {
                s_FieldHotControl.TryGetValue(fieldHash, out int tracked);
                bool released = (hotNow == 0) || (tracked != 0 && hotNow != tracked);

                if (released && (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout))
                {
                    UnityEngine.Debug.Log($"[FloatInterval] Drag END   path='{property.propertyPath}', hotWas={tracked} -> {hotNow}, start={start:F4}, end={end:F4}");
                    InvokeEndDragHandlerIfAny(property, start, end);
                    s_IsDragging.Remove(fieldHash);
                    s_FieldHotControl.Remove(fieldHash);
                }
            }

            if (valueChanged && !s_IsDragging.Contains(fieldHash) &&
                (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout))
            {
                UnityEngine.Debug.LogWarning($"[FloatInterval] Value CHANGED (no drag) path='{property.propertyPath}', start={start:F4}, end={end:F4}");
                InvokeEndDragHandlerIfAny(property, start, end);
            }

            EditorGUI.EndProperty();
        }

        private static void Draw(Rect sliderRect, Rect startRect, Rect endRect,
                                 float min, float max, ref float start, ref float end)
        {
            float s = start;
            float e = end;

            s = EditorGUI.FloatField(startRect, s);
            e = EditorGUI.FloatField(endRect, e);

            EditorGUI.MinMaxSlider(sliderRect, ref s, ref e, min, max);

            s = Mathf.Clamp(s, min, max);
            e = Mathf.Clamp(e, min, max);
            if (s > e) { float mid = 0.5f * (s + e); s = mid; e = mid; }

            start = s;
            end = e;
        }

        private void GetBoundsFromAttribute(out float min, out float max)
        {
            var rangeAttr = fieldInfo.GetCustomAttribute<FloatIntervalRangeAttribute>();
            if (rangeAttr != null)
            {
                min = rangeAttr.Min; max = rangeAttr.Max;
            }
            else
            {
                min = 0f; max = 1f;
            }

            if (max <= min)
                max = min + Mathf.Max(1e-4f, Mathf.Abs(min) * 1e-4f + 1e-4f);
        }

        private void InvokeEndDragHandlerIfAny(SerializedProperty property, float start, float end)
        {
            var handlerAttr = fieldInfo?.GetCustomAttribute<FloatIntervalOnEndDragAttribute>();
            if (handlerAttr == null || string.IsNullOrEmpty(handlerAttr.MethodName))
            {
                UnityEngine.Debug.Log($"[FloatInterval] No [FloatIntervalOnEndDrag] on '{fieldInfo?.DeclaringType?.Name}.{fieldInfo?.Name}' (path='{property.propertyPath}')");
                return;
            }

            string methodToken = handlerAttr.MethodName?.Trim();
            UnityEngine.Debug.Log($"[FloatInterval] EndDrag → try invoke '{methodToken}' (path='{property.propertyPath}')");

            var tried = new List<string>();

            object owner = TryGetOwnerObject(property, out string ownerDbgPath);
            if (owner != null)
            {
                if (TryInvokeOnTarget(owner, methodToken, property, start, end, out string why))
                {
                    UnityEngine.Debug.Log($"[FloatInterval] Handler INVOKED on OWNER '{owner.GetType().Name}' (ownerPath='{ownerDbgPath}')");
                    return;
                }
                tried.Add($"Owner:{owner.GetType().FullName} — {why}");
            }
            else
            {
                tried.Add("Owner: <null> (failed to resolve owner)");
            }

            var targets = property.serializedObject.targetObjects;
            bool invokedOnTarget = false;
            foreach (var t in targets)
            {
                if (t == null) { tried.Add("Target:<null>"); continue; }

                if (TryInvokeOnTarget(t, methodToken, property, start, end, out string why))
                {
                    UnityEngine.Debug.Log($"[FloatInterval] Handler INVOKED on TARGET '{t.GetType().Name}'");
                    invokedOnTarget = true;
                    break;
                }
                tried.Add($"Target:{t.GetType().FullName} — {why}");
            }
            if (invokedOnTarget) return;

            if (TryInvokeStatic(methodToken, property, start, end, out string whyStatic))
            {
                UnityEngine.Debug.Log($"[FloatInterval] Static handler '{methodToken}' INVOKED");
                return;
            }
            tried.Add($"Static:{methodToken} — {whyStatic}");

            UnityEngine.Debug.LogError(
                $"[FloatInterval] FAILED to invoke handler '{methodToken}' for path='{property.propertyPath}'.\n" +
                $"Field: '{fieldInfo?.DeclaringType?.FullName}.{fieldInfo?.Name}'\n" +
                $"Tried:\n - {string.Join("\n - ", tried)}"
            );
        }

        private static bool TryInvokeOnTarget(object target, string methodToken, SerializedProperty property, float start, float end, out string reason)
        {
            reason = string.Empty;
            var type = target.GetType();

            MethodInfo mi =
                type.GetMethod(methodToken, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)
                ?? type.GetMethod(methodToken, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(FloatInterval) }, null)
                ?? type.GetMethod(methodToken, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(float) }, null);

            if (mi == null)
            {
                reason = "method not found on instance";
                return false;
            }

            try
            {
                var pars = mi.GetParameters();
                if (pars.Length == 0)
                {
                    mi.Invoke(target, null);
                }
                else if (pars.Length == 1 && pars[0].ParameterType == typeof(FloatInterval))
                {
                    object boxed = GetBoxedFloatInterval(property);
                    mi.Invoke(target, new[] { boxed });
                }
                else
                {
                    mi.Invoke(target, new object[] { start, end });
                }
                return true;
            }
            catch (Exception ex)
            {
                reason = $"exception: {ex.GetType().Name}: {ex.Message}";
                UnityEngine.Debug.LogException(ex);
                return false;
            }
        }

        private static bool TryInvokeStatic(string methodToken, SerializedProperty property, float start, float end, out string reason)
        {
            reason = string.Empty;

            int lastDot = methodToken.LastIndexOf('.');
            if (lastDot <= 0 || lastDot >= methodToken.Length - 1)
            {
                reason = "not a static token (no Type.Method format)";
                return false;
            }

            string typeName = methodToken.Substring(0, lastDot);
            string methodName = methodToken.Substring(lastDot + 1);

            var type = Type.GetType(typeName);
            if (type == null)
            {
                reason = $"type '{typeName}' not found (ensure full name incl. assembly if needed)";
                return false;
            }

            MethodInfo mi =
                type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)
                ?? type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(FloatInterval) }, null)
                ?? type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(float) }, null);

            if (mi == null)
            {
                reason = "static method not found on type";
                return false;
            }

            try
            {
                var pars = mi.GetParameters();
                if (pars.Length == 0)
                {
                    mi.Invoke(null, null);
                }
                else if (pars.Length == 1 && pars[0].ParameterType == typeof(FloatInterval))
                {
                    object boxed = GetBoxedFloatInterval(property);
                    mi.Invoke(null, new[] { boxed });
                }
                else
                {
                    mi.Invoke(null, new object[] { start, end });
                }
                return true;
            }
            catch (Exception ex)
            {
                reason = $"exception: {ex.GetType().Name}: {ex.Message}";
                UnityEngine.Debug.LogException(ex);
                return false;
            }
        }

        private static object GetBoxedFloatInterval(SerializedProperty property)
        {
            object current = property.serializedObject.targetObject;
            Type currentType = current.GetType();

            string[] parts = property.propertyPath.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                if (part == "Array") continue;

                if (part.StartsWith("data[", StringComparison.Ordinal))
                {
                    int lb = part.IndexOf('[') + 1;
                    int rb = part.IndexOf(']');
                    int idx = int.Parse(part.Substring(lb, rb - lb));
                    if (current is IList list && idx >= 0 && idx < list.Count)
                    {
                        current = list[idx];
                        currentType = current?.GetType();
                    }
                    continue;
                }

                var fi = currentType.GetField(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi != null)
                {
                    current = fi.GetValue(current);
                    currentType = fi.FieldType;
                    continue;
                }

                var pi = currentType.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi != null)
                {
                    current = pi.GetValue(current);
                    currentType = pi.PropertyType;
                }
            }
            return current;
        }

        private static object TryGetOwnerObject(SerializedProperty property, out string debugPath)
        {
            debugPath = string.Empty;
            try
            {
                string[] parts = property.propertyPath.Split('.');
                if (parts.Length == 0) return null;

                int cutLen = parts.Length - 1;

                object current = property.serializedObject.targetObject;
                Type currentType = current.GetType();
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < cutLen; i++)
                {
                    string part = parts[i];
                    if (sb.Length > 0) sb.Append('.');
                    sb.Append(part);

                    if (part == "Array") continue;

                    if (part.StartsWith("data[", StringComparison.Ordinal))
                    {
                        int lb = part.IndexOf('[') + 1;
                        int rb = part.IndexOf(']');
                        int idx = int.Parse(part.Substring(lb, rb - lb));
                        if (current is IList list && idx >= 0 && idx < list.Count)
                        {
                            current = list[idx];
                            currentType = current?.GetType();
                        }
                        continue;
                    }

                    var fi = currentType.GetField(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fi != null)
                    {
                        current = fi.GetValue(current);
                        currentType = fi.FieldType;
                        continue;
                    }

                    var pi = currentType.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pi != null)
                    {
                        current = pi.GetValue(current);
                        currentType = pi.PropertyType;
                    }
                }

                debugPath = sb.ToString();
                return current;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return null;
            }
        }
    }
}
