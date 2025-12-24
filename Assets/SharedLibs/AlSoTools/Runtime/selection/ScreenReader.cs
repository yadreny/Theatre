using AlSo;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public class ScreenReader
    {
        public Camera Camera { get; }

        public ScreenReader(Camera camera)
        {
            Camera = camera;
        }

        //private static int counter = 0;

        public Texture2D Capture(int width, bool switchToSolidColor, Color color = default)
        {
            int wholeWidht = (int)Mathf.Round(width / Camera.rect.width);
            float hRatio = Camera.rect.height * Screen.height/ (Screen.width * Camera.rect.width);
            int height = (int)Math.Round(width * hRatio);
            int wholeHeight = (int)Mathf.Round(height / Camera.rect.height);

            RenderTexture RenderTexture = new RenderTexture(wholeWidht, wholeHeight, 24);
            Camera.targetTexture = RenderTexture;

            CameraClearFlags flags = Camera.clearFlags;
            if (switchToSolidColor)
            {
                Camera.clearFlags = CameraClearFlags.SolidColor;
                Camera.backgroundColor = color;
            }
            Camera.Render();

            if (switchToSolidColor) Camera.clearFlags = flags;

            RenderTexture.active = RenderTexture;

            Rect uarea = new Rect(Mathf.Round(Camera.rect.x * wholeWidht), Mathf.Round((1 - Camera.rect.y - Camera.rect.height) * wholeHeight), width, height);
            Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.ReadPixels(uarea, 0, 0);
            result.Apply();

            //result.SaveTo($"Assets/capture{counter++}.png");

            Camera.targetTexture = null;
            RenderTexture.active = null;
            GameObject.Destroy(RenderTexture);

            return result;
        }

        public void Destory() { }
    }
}