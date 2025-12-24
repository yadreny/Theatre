using AlSo;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace AlSo
{
    public interface IMold
    {
        IBlendShapeDescription Description { get; }
        float Value { get; }
        bool ApplyTo(SkinnedMeshRenderer smr)
        {
            if (!Description.IsComportable(smr.sharedMesh))
            {
                Debug.LogError($"cant apply to mesh {Description.Name})");
                return false;
            }
            int index = smr.sharedMesh.GetBlendShapeIndex(Description.Name);
            if (index == -1)
            {
                Debug.LogWarning($"cant find blendshape {Description.Name}");
            }
            else smr.SetBlendShapeWeight(index, Value);
            return true;
        }
    }

    public interface IMoldEditable : IMold
    {
        float SetValue(float value);
    }

    public interface IMoldGroup
    { 
        IEnumerable<IMold> Elements { get; }
        bool ApplyTo(SkinnedMeshRenderer smr) => Elements.All(x => x.ApplyTo(smr));
    }


    public interface IEditableMoldGroup : IMoldGroup
    { 
        IEnumerable<IMoldEditable> EditableElements { get; }

        void Reset();
        void Save();
    }

    [Serializable]
    public struct Mold : IMold
    {
        public BlendShapeDescription description;
        public IBlendShapeDescription Description => description;

        public float value;
        public float Value => value;

        public Mold(BlendShapeDescription description, float value)
        {
            this.description = description;
            this.value = value;
        }

        public Mold Modify(float newValue) => new Mold(description, newValue);


    }
}