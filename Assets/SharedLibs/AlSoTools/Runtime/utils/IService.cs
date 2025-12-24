using System;
using System.Linq;
using static AlSo.ShortCuts;

namespace AlSo
{
    public interface IService : IKnownType { }

    public class ServiceManager
    {
        private ServiceManager() { }
        public static ServiceManager Instance { get; } = new ServiceManager();

        protected IService[] _services;
        protected IService[] Services => CreateIfNotExist(ref _services, 
            ()=> typeof(IService).GetExtendedFrom(false, false).Select(x => Activator.CreateInstance(x, true) as IService).ToArray());

        public T GetService<T>() where T : IService
            => Services.OfType<T>().SingleOrDefault();
    }
}