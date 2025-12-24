using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlSo
{
    public interface IFreeCamConfig
    {
        float ShiftBoost { get; }
        float KeyPanSensivity { get; }
        float KeyTopDownSensivity { get; }
        float FreeLookSensitivity { get; }
    }


    public class FreeCamConfig : IFreeCamConfig
    {
        public float ShiftBoost { get; } = 5;
        public float KeyPanSensivity { get; } = 0.025f;
        public float KeyTopDownSensivity { get; } = 0.025f;
        public float FreeLookSensitivity { get; } = 3f;
    }
}
