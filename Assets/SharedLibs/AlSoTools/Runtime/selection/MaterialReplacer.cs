using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static AlSo.ShortCuts;

namespace AlSo
{
    public class MaterialReplacer
    {
        public static Material _material;
        public static Material Material => CreateIfNotExist(ref _material, () => Resources.Load<Material>("Detector"));
        GameObject[] Objects { get; }
        Color[] Colors { get; }
        Material[][] DetectorMaterials { get; }

        Dictionary<GameObject, Renderer[]> ObjectToRenderers { get; } = new Dictionary<GameObject, Renderer[]>();
        Dictionary<Renderer, Material[]> RendererToMaterials { get; } = new Dictionary<Renderer, Material[]>();

        public MaterialReplacer(GameObject[] objects, Color[] colors)
        {
            Objects = objects;
            Colors = colors;
            DetectorMaterials = Colors.Select(GetColoredMaterail).ToArray();

            foreach (GameObject obj in Objects)
            {
                Renderer[] parts = obj.GetComponentsInChildren<Renderer>();
                ObjectToRenderers.Add(obj, parts);

                foreach (Renderer smr in parts)
                {
                    RendererToMaterials.Add(smr, null);
                }
            }
        }

        public void Colorize()
        {
            foreach (Renderer renderer in RendererToMaterials.Keys.ToArray())
            {
                RendererToMaterials[renderer] = renderer.materials;
            }

            for (int i = 0; i < Objects.Length; i++)
            {
                GameObject obj = Objects[i];
                Material[] colored = DetectorMaterials[i];
                foreach (Renderer smr in ObjectToRenderers[obj]) smr.materials = colored;
            }
        }

        public void Restore()
        {
            foreach (Renderer renderer in RendererToMaterials.Keys)
            {
                renderer.materials = RendererToMaterials[renderer];
            }
        }

        Material[] GetColoredMaterail(Color color)
        {
            Material result = new Material(Material);
            result.SetColor("_Color", color);
            return new Material[] { result };
        }
    }
}