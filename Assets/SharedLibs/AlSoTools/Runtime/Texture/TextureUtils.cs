using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public static class TextureUtils
    {
        public static Texture2D LoadPNG(string filePath)
        {

            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); 
            }
            return tex;
        }

        public static Texture2D ToTexture(this Sprite sprite)
        {
            if (sprite == null) return null;

            // Create a new Texture2D with the size of the sprite's rect
            Texture2D newTexture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
            newTexture.filterMode = sprite.texture.filterMode;
            newTexture.wrapMode = TextureWrapMode.Clamp;

            // Copy pixel data from the sprite's original texture
            Color[] pixels = sprite.texture.GetPixels(
                (int)sprite.textureRect.x,
                (int)sprite.textureRect.y,
                (int)sprite.textureRect.width,
                (int)sprite.textureRect.height);

            newTexture.SetPixels(pixels);
            newTexture.Apply();

            return newTexture;
        }
    }
}
