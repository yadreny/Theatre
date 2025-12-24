using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public static class TextureRoutine
    {
        public static Texture2D Crop(this Texture2D self, float x, float y, float widthF, float heightF)
        {
            int width = (int)widthF;
            int height = (int)heightF;

            Color[] pixels = self.GetPixels((int)x, self.height - height - (int)y, width, height, 0);
            Texture2D croped = new Texture2D(width, height);
            croped.SetPixels(pixels);
            croped.Apply();
            return croped;
        }

        public static void Resize(Texture2D texture2D, int targetX, int targetY, bool mipmap = true, FilterMode filter = FilterMode.Bilinear)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetX, targetY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture.active = rt;

            Graphics.Blit(texture2D, rt);

            texture2D.Reinitialize(targetX, targetY, texture2D.format, mipmap);
            texture2D.filterMode = filter;

            try
            {
                texture2D.ReadPixels(new Rect(0.0f, 0.0f, targetX, targetY), 0, 0);
                texture2D.Apply();
            }
            catch
            {
                Debug.LogError("Read/Write is not enabled on texture " + texture2D.name);
            }

            RenderTexture.ReleaseTemporary(rt);
        }
    }
   
}
