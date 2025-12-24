using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public enum ColorSpace { Linear, Gamma }

    public static class Rasterizer
    {
        public static Texture2D RenderToTexture(this Material material, int width, int height, ColorSpace colorSpace, bool nothing=false)
        {
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture.active = renderTexture;

            Graphics.Blit(null, renderTexture, material, 0);

            Texture2D newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, colorSpace == ColorSpace.Linear);
            newTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            

            bool applyMipsmaps = false;
            newTexture.Apply(applyMipsmaps);
            //bool highQuality = true;
            //newTexture.Compress(highQuality);

            RenderTexture.active = null;
            if (Application.isPlaying) GameObject.Destroy(renderTexture);
            else GameObject.DestroyImmediate(renderTexture);

            return newTexture;
        }

        public static Texture2D RenderToTextureAlpha(this Material material, int width, int height)
        {
            RenderTexture renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            renderTexture.Create();

            GameObject camHolder = new GameObject();
            Camera camera = camHolder.AddComponent<Camera>();
            camera.backgroundColor = new Color(0, 0, 0, 0); // Set the camera's background color to transparent.
            camera.targetTexture = renderTexture;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = false;
            camera.Render();
            Graphics.Blit(null, renderTexture, material, 0);

            RenderTexture.active = renderTexture;

            Texture2D newTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            newTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            newTexture.Apply();

            RenderTexture.active = null;
            GameObject.DestroyImmediate(camera);
            GameObject.DestroyImmediate(renderTexture);
            GameObject.DestroyImmediate(camHolder);

            return newTexture;
        }
    }
}