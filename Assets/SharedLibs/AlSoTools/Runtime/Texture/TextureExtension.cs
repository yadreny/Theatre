using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static AlSo.ShortCuts;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlSo
{
    public static class TextureExtension
    {
        public static void SaveTo(this Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

#if UNITY_EDITOR
        public static void SaveTextureAssetTo(this Texture2D texture, string savePath)
        {
            texture.SaveTo(savePath);
            AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
        }
#endif



        public static Sprite ToSprite(this Texture2D texture)
        {
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            Sprite sprite = Sprite.Create(texture, rect, pivot);
            return sprite;  
        }

        public static Texture2D InvertColors(this Texture2D original)
        {
            Texture2D inverted = new Texture2D(original.width, original.height, original.format, original.mipmapCount > 1);
            Color[] pixels = original.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(1.0f - pixels[i].r, 1.0f - pixels[i].g, 1.0f - pixels[i].b, pixels[i].a);
            }
            inverted.SetPixels(pixels);
            inverted.Apply();
            return inverted;
        }

        public static Texture2D GetMask(this Texture2D texture, Color colorToRemove, Color Valuble, Color Transparent)
        {
            Texture2D result = new Texture2D(texture.width, texture.height);
            for (int i = 0; i < texture.width; i++)
            {
                for (int j = 0; j < texture.height; j++)
                {
                    Color pixel = texture.GetPixel(i, j);
                    Color color = colorToRemove == pixel ? Transparent : Valuble;
                    result.SetPixel(i, j, color);
                }
            }
            result.Apply();
            return result;
        }

        public static Texture2D Clone(this Texture2D texOriginal)
        {
            Texture2D tex = new Texture2D(texOriginal.width, texOriginal.height);
            tex.SetPixels(texOriginal.GetPixels(0, 0, texOriginal.width, texOriginal.height));
            return tex;
        }

        public static Texture2D Fill(this Texture2D texture, Color color)
        {
            Color[] pixels = new Color[texture.width * texture.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        public static (Texture2D, Vector2, Vector2) GetValuble(this Texture2D texture, Color color)
        {
            int xmin = texture.width;
            int ymin = texture.height;
            int xmax = 0;
            int ymax = 0;

            for (int i = 1; i < texture.width-1; i++)
            {
                for (int j = 1; j < texture.height-1; j++)
                {
                    Color pixel = texture.GetPixel(i,j);
                    if (pixel == color) continue;

                    xmin = (int)Mathf.Min(xmin, i);
                    ymin = (int)Mathf.Min(ymin, j);

                    xmax = (int)Mathf.Max(xmax, i);
                    ymax = (int)Mathf.Max(ymax, j);
                }
            }

            int widht = xmax - xmin;
            int height = ymax - ymin;   
            Texture2D cropped = texture.Crop(new Rect(xmin, texture.height - ymin - height, widht, height));
            Vector2 lu = new Vector2(1f * xmin / texture.width, 1f * (texture.height - ymin - height) / texture.height);
            Vector2 size = new Vector2(1f * widht / texture.width, 1f * height / texture.height);
            return (cropped, lu, size);
        }

        public static Texture2D CloneTexture(this Texture2D texOriginal)
        {
            Texture2D tex = new Texture2D(texOriginal.width, texOriginal.height);
            tex.SetPixels(texOriginal.GetPixels(0, 0, texOriginal.width, texOriginal.height));
            tex.Apply();
            return tex;
        }

        public static Texture2D Crop(this Texture2D texture, Rect cropRect)
        {
            int width = (int)cropRect.width;
            int height = (int)cropRect.height;

            int x = (int)cropRect.x;
            int y = texture.height - (int)cropRect.y - height;

            Color[] croppedPixels = texture.GetPixels(x, y, width, height);

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(croppedPixels);
            result.Apply();

            return result;
        }

        public static Texture2D ResizeMe(this Texture2D texture, int newWidth)
        {
            if (texture == null) throw new Exception("null texture resize");
            int newHeight = texture.height * newWidth / texture.width;
            Material resizer = Resources.Load<Material>("Resizer");
            resizer.SetTexture("_Texture", texture);
            Texture2D result = resizer.RenderToTexture(newWidth, newHeight, ColorSpace.Gamma);
            return result;
        }

        public static Texture2D FillArea(this Texture2D background, Vector2 relativeCenter, Vector2 relativeSize, Color color)
        {
            int lbX = (int)((relativeCenter.x - relativeSize.x / 2) * background.width);
            int lbY = (int)((1 - relativeCenter.y - relativeSize.y / 2) * background.height);
            int areaWidht = (int)(relativeSize.x * background.width);
            int areaHeight = (int)(relativeSize.y * background.height);

            Rect rect = new Rect(lbX, lbY, areaWidht, areaHeight);
            rect = rect.GetIntersection(new Rect(0, 0, background.width, background.height));
            int sx = (int)rect.x;
            int sy = (int)rect.y;

            for (int i=0; i < rect.width; i++) 
            {
                for (int j = 0; j< rect.height; j++)
                {
                    background.SetPixel(sx + i, sy + j, color);
                }
            }
            background.Apply();
            return background;
        }


        public static Texture2D CombineWith(this Texture2D background, Texture2D overlay, Vector2 relativeLu)
        {
            Material material = Resources.Load<Material>("OverlayApplier");
            material.SetTexture("_Background", background);
            material.SetTexture("_Overlay", overlay);
            material.SetVector("_Lu", relativeLu);
            return material.RenderToTexture(background.width, background.height, ColorSpace.Gamma);
        }

        private static Material _colorReplacer;
        public static Material ColorReplacer => CreateIfNotExist(ref _colorReplacer, () => Resources.Load<Material>("ColorReplacer"));

        public static Texture2D ReplaceColor(this Texture2D shot, Color oldColor, Color newColor)
        {
            Material material = ColorReplacer;
            material.SetTexture("_Shot", shot);
            material.SetColor("_ColorOld", oldColor);
            material.SetColor("_ColorNew", newColor);

            Texture2D texture = material.RenderToTexture(shot.width, shot.height, ColorSpace.Gamma);
            return texture;
        }

    }
}
