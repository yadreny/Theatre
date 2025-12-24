using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
 
    public class ImageResource
    {
        protected Func<string> CreatePath { get; }
        protected Action<string> SavePath { get; }
        protected Func<string> GetPath { get; }

        public ImageResource(Func<string> createPath, Action<string> savePath, Func<string> getPath)
        {
            CreatePath = createPath;
            SavePath = savePath;
            GetPath = getPath;
        }

        protected Texture2D _texture;

        public Texture2D Texture
        {
            get
            {
                string path = GetPath();
                if (path == null) return null;
                if (_texture == null) _texture = TextureUtils.LoadPNG(path);
                return _texture;
            }
            set
            {
                _texture = value;
                if (_texture == null)
                {
                    SavePath(null);
                    return;
                }
                string path = CreatePath();
                _texture.SaveTo(path);
                SavePath(path);
            }
        }
    }

}