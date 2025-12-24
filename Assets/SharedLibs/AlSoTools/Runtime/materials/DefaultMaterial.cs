using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEngine;

namespace AlSo
{
    public class DefaultMaterial
    {
        public static Material GetDefaultMaterial(Color color = default)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            string shaderName = default;

            if (pipeline == null) shaderName = "Standard";
            else
            {
                string pipelineType = pipeline.GetType().ToString();
                if (pipelineType.Contains("UniversalRenderPipeline")) shaderName = "Universal Render Pipeline/Lit";
                if (pipelineType.Contains("HDRenderPipeline")) shaderName = "HDRP/Lit";
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"cant find shader:{shaderName}");
                return null;
            }

            Material m = new Material(shader);
            if (color != default)
            { 
                m = new Material(m);
                m.color = color;    
            }
            return m;
        }
    }
}