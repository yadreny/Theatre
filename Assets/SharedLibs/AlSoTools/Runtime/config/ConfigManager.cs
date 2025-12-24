using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static AlSo.ShortCuts;

namespace AlSo
{
    public class ConfigPath : AlsoAttribute
    { 
        public string Path { get; private set; }

        public ConfigPath(string path)
        {
            this.Path = path;
        }

        public string GetConfigText()
        {
            string data = FileUtils.Load(Path);
            return data;
        }
    }

    public interface IConfig : IKnownType { }

    public class ConfigManager 
    {
        public static T GetConfig<T>() where T : IConfig  => Instance.GetConfigOf<T>();
        private static ConfigManager Instance { get; } = new ConfigManager();
        private ConfigManager() { }

        private IConfig[] _instances;
        private IConfig[] Instances => CreateIfNotExist(ref _instances, GetConfigs);

        private T GetConfigOf<T>() where T : IConfig
        {
            T[] configs = Instances.OfType<T>().ToArray();
            //if (configs.Length > 1) Debug.LogError($"duiplicated config problem {typeof(T)} ");
            if (configs.Length == 0) throw new Exception($"config not found {typeof(T)} ");
            return configs.First();
        }

        private string GetErrorMessage<T>()
        {
            Type type = typeof(T);
            bool anyConfigClassExist = Instances.Length == 0;
            string message = anyConfigClassExist ?
                $"can't find {type} config, please and this interface to one of following classes: {Instances.Select(x => x.ToString()).Join("\n")}"
                : $"can't find {type} config, project contais no one non-abstract config class";
            return message;
        }

        private IConfig[] GetConfigs()
        {
            IConfig[] res =TypeExtensions.GetExtendedFrom<IConfig>().Select(GetConfigFromFile).ToArray();
            foreach (IConfig c in res)
            {
                Debug.Log($"{c.GetType()} config found");
            }
            return res;
        }

        private IConfig GetConfigFromFile(Type type)
        {
            return Activator.CreateInstance(type, true) as IConfig;
#if PLATFORM_WEBGL
            return Activator.CreateInstance(type, true) as IConfig;
            //var func = WebGLFabric[type];
            //IConfig res = WebGLFabric[type].Invoke();
            //if (res == null) Debug.LogError($"config problem {type}");
            //return res;
#endif
            Debug.LogError("fuck");
            ConfigPath cp = type.GetClassMeta<ConfigPath>();
            if (cp == null) throw new Exception($"{type} has no ConfigPath attribute");


            string text = null;
            try
            {
                text = cp.GetConfigText();
            }
            catch
            {
                throw new Exception($"bad config path on {type}: {cp.Path}");
            }

            try
            {
                IConfig result = Converter.Converter.FromJson(type, text) as IConfig;
                return result;
            }
            catch
            {
                //Debug.LogError($"looks like json data of {type} is not valid");
                throw new Exception($"looks like json data of {type} is not valid");
                
                //throw new Exception($"looks like json data of {type} is not valid");
            }
        }
    }
}