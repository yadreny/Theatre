using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using UnityEngine.Rendering.HighDefinition;


namespace AlSo
{
    public interface IPoolObject
    {
        void RelaxInPool();
        void Reactivate();
    }
    public interface IObjectPooler<T> where T : IPoolObject
    {
        T GetInstance();
        void ToPool(T item);
    }

    public class ObjectPooler<T> : IObjectPooler<T> where T : IPoolObject
    {
        public Vector3 FarAway { get; } = new Vector3(10000, 10000, 10000);

        protected Func<T> Creator { get; }
        protected HashSet<T> Pool { get; } = new HashSet<T>();

        public ObjectPooler(Func<T> creator)
        {
            this.Creator = creator;
        }
        
        public T GetInstance()
        {
            if (Pool.FirstOrDefault()!=null)
            {
                T result = Pool.First();
                Pool.Remove(result);
                result.Reactivate();
                return result;
            } 
            return CreateNewInstance();
        }

        protected T CreateNewInstance() => Creator.Invoke();

        public void ToPool(T item) => Pool.Add(item);
    }
}