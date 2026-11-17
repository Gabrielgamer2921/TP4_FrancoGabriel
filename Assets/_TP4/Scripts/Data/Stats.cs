using System;
using UnityEngine;

namespace TP4
{
    [Serializable]
    public struct Stats
    {
        [Min(1)] public int fuerza;
        [Min(1)] public int agilidad;
        [Min(1)] public int vitalidad;
        [Min(1)] public int magia;
        [Min(0)] public int defensa;

        public Stats(int fuerza, int agilidad, int vitalidad, int magia, int defensa)
        {
            this.fuerza = fuerza;
            this.agilidad = agilidad;
            this.vitalidad = vitalidad;
            this.magia = magia;
            this.defensa = defensa;
        }

        public int Get(StatType type)
        {
            switch (type)
            {
                case StatType.Fuerza: return fuerza;
                case StatType.Agilidad: return agilidad;
                case StatType.Vitalidad: return vitalidad;
                case StatType.Magia: return magia;
                case StatType.Defensa: return defensa;
                default: return 0;
            }
        }
    }

    /// <summary>Modificador porcentual de una stat (ej: Defensa -0.25 = -25%).</summary>
    [Serializable]
    public struct StatModifierDef
    {
        public StatType stat;
        [Tooltip("0.3 = +30%, -0.25 = -25%")]
        [Range(-0.9f, 2f)] public float porcentaje;
    }
}
