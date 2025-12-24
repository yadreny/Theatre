using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public interface IOutlineService : IService
    {
        void AddOutline(Renderer renderer, bool visible);
    }
}
