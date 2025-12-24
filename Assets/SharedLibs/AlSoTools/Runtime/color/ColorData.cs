using UnityEngine;
using UnityEngine.UI;
using System;
using AlSo;
using static AlSo.ShortCuts;

namespace AlSo
{
    public class ColorData
    {
        private static int HueLength = 360;
        public Action OnChanged { get; set; }
        public Color Color { get; private set; }

        public string AsString => Color.ColorToHex();
        
        public ColorData(string color)
        {
            Color = color.ToColor();
            HsvColor hsv = HSVUtil.ConvertRgbToHsv(Color);
            (_hue, _saturation, _brightness) = (Mathf.InverseLerp(0, HueLength, (float)hsv.H), (float)hsv.S, (float)hsv.V);
        }

        private void UpdateAndReport()
        {
            Color = HSVUtil.ConvertHsvToRgb(Mathf.Lerp(0, HueLength, Hue), Saturation, Brightness, 1);
            OnChanged?.Invoke();
        }

        private float _hue;
        public float Hue
        {
            get => _hue;
            set
            {
                _hue = value;
                UpdateAndReport();
            }
        }

        private float _saturation;
        public float Saturation
        {
            get => _saturation;
            set
            {
                _saturation = value;
                UpdateAndReport();
            }
        }

        private float _brightness;
        public float Brightness
        {
            get => _brightness;
            set
            {
                _brightness = value;
                UpdateAndReport();
            }
        }

        public void SetSaturationAndBrightness(float s, float b)
        {
            _saturation = s;
            _brightness = b;
            UpdateAndReport();
        }



        private static Texture2D _hueArea;
        public static Texture2D HueArea => CreateIfNotExist(ref _hueArea, GetHueArea);

        private static Texture2D GetHueArea()
        {
            Texture2D texture = new Texture2D(1, HueLength);
            Color32[] colors = new Color32[HueLength];

            for (int i = 0; i < HueLength; i++) colors[i] = HSVUtil.ConvertHsvToRgb(i, 1, 1, 1);

            texture.SetPixels32(colors);
            texture.Apply();
            return texture;
        }

        private static readonly int TextureWidth = 100;
        private static readonly int TextureHeight = 100;

        public Texture2D UpdateColorArea(Texture2D texture)
        {
            if (texture == null) texture = new Texture2D(TextureWidth, TextureHeight);

            double h = Hue * 360;
            for (int s = 0; s < texture.width; s++)
            {
                Color32[] colors = new Color32[texture.height];
                for (int v = 0; v < texture.height; v++)
                {
                    colors[v] = HSVUtil.ConvertHsvToRgb(h, (float)s / texture.width, (float)v / texture.height, 1);
                }
                texture.SetPixels32(s, 0, 1, texture.height, colors);
            }
            texture.Apply();
            return texture;
        }
    }
}