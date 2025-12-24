using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlSo
{
    [Serializable]
    public class EditableMold : IMoldEditable
    {
        [HideInInspector]
        public float _value;
        public float Value => _value;
        public float SetValue(float value) => _value = value;

        public BlendShapeDescription description;
        public IBlendShapeDescription Description => description;

        public EditableMold(BlendShapeDescription description, float value = 0)
        {
            _value = value;
            this.description = description;
        }
    }



#if UNITY_EDITOR
    public static class MoldGroupEditableGenerator
    {
        [MenuItem("Assets/BlendShapes/Create Editable Mold Group")]
        public static void Create()
        { 
            BlendShapeDescriptionGroup group = Selection.activeObject as BlendShapeDescriptionGroup;
            if (group == null)
            {
                Debug.LogError("no BlendShapeDescriptionGroup selected");
                return;
            }
            string path = $"{AssetDatabase.GetAssetPath(group).ParentFolderPath()}/{group.name}_editable.asset";
            EditableMoldGroup result = ScriptableObject.CreateInstance<EditableMoldGroup>();
            result.Initialize(group);
            AssetDatabase.CreateAsset(result, path);
            AssetDatabase.Refresh();
        }
    }
#endif
}