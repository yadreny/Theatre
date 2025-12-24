using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlSo
{
#if UNITY_EDITOR

    public static class EditableMoldGroupFixer
    {
        [MenuItem("Assets/BlendShapes/Fix Frame Mult")]
        static void FixMultiplier()
        {
            BlendShapeDescriptionGroup description = Selection.activeObject as BlendShapeDescriptionGroup;
            if (description == null) return;

            Type t = typeof(EditableMoldGroup);
            string[] guids = AssetDatabase.FindAssets($"t:{t.ToString().Split(".").Last()}", new[] { "Assets" });
            string[] paths = guids.Select(x => AssetDatabase.GUIDToAssetPath(x)).ToArray();
            EditableMoldGroup[] result = paths.Select(x => AssetDatabase.LoadAssetAtPath(x, t)).OfType<EditableMoldGroup>().ToArray();

            foreach (EditableMoldGroup group in result)
            {
                foreach (var elem in group.elements)
                {
                    elem.SetValue(elem.Value * 100);
                }
                EditorUtility.SetDirty(group);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("fixed");
        }

        [MenuItem("Assets/BlendShapes/Fix descriptions")]
        static void FixDesctiption()
        {
            BlendShapeDescriptionGroup description = Selection.activeObject as BlendShapeDescriptionGroup;
            if (description == null) return;

            string[] names = Enumerable.Range(0, description.source.sharedMesh.blendShapeCount)
                .Select(x => description.source.sharedMesh.GetBlendShapeName(x)).ToArray();

            string[] newNames = names.Where(name => !description.descriptions.Select(x => x.name).Contains(name)).ToArray();

            foreach (string name in newNames)
            {
                Debug.Log(name);
            }

            EditorUtility.SetDirty(description);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("fixed");
        }

        [MenuItem("Assets/BlendShapes/Fix Molds")]
        static void FixMolds()
        {
            BlendShapeDescriptionGroup description = Selection.activeObject as BlendShapeDescriptionGroup;
            if (description == null) return;

            Type t = typeof(EditableMoldGroup);
            string[] guids = AssetDatabase.FindAssets($"t:{t.ToString().Split(".").Last()}", new[] { "Assets" });
            string[] paths = guids.Select(x => AssetDatabase.GUIDToAssetPath(x)).ToArray();
            EditableMoldGroup[] result = paths.Select(x => AssetDatabase.LoadAssetAtPath(x, t)).OfType<EditableMoldGroup>().ToArray();

            //foreach (EditableMoldGroup group in result)
            //{
            //    if (group.description != null) continue;
            //    group.description = description;
            //    EditorUtility.SetDirty(group);
            //}

            foreach (EditableMoldGroup group in result)
            {
                if (description.Descriptions.Count() == group.elements.Length) continue;

                BlendShapeDescription[] notFounded = description.Descriptions.Where(desc => !group.elements.Select(x => x.Description.Name).Contains(desc.Name)).OfType<BlendShapeDescription>().ToArray();

                List<EditableMold> newItems = notFounded.Select(x => new EditableMold(x)).ToList();
                group.elements = group.elements.Union(newItems).ToArray();

                EditorUtility.SetDirty(group);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("fixed");
        }
    }
#endif

    public class EditableMoldGroup : ScriptableObject, IEditableMoldGroup
    {
        public BlendShapeDescriptionGroup description;

        public EditableMold[] elements;
        public IEnumerable<IMoldEditable> EditableElements => elements;
        public IEnumerable<IMold> Elements => EditableElements;

        public void Initialize(BlendShapeDescriptionGroup group)
        {
            elements = group.descriptions.Select(x => new EditableMold(x)).ToArray();
        }

        public void Reset()
        {
            elements.ForEach(x => x.SetValue(0));
        }

        

        public void Save()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }
}