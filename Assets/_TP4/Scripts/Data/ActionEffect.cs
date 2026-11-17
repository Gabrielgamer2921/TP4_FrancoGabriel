using System;
using UnityEngine;

namespace TP4
{
    /// <summary>
    /// Un efecto dentro de una acción (habilidad o ítem). Una acción puede tener varios
    /// (ej: Plasma = Daño de energía + Aplicar estado Quemadura).
    /// Cada tipo usa solo algunos campos; los tooltips indican cuáles.
    /// </summary>
    [Serializable]
    public class ActionEffect
    {
        public EffectType tipo = EffectType.DanoFisico;

        [Tooltip("Daño/Curar/Energía: multiplicador de la stat del que actúa (Fuerza o Magia).\n" +
                 "Modificar ATB: cantidad sumada a la barra (1 = barra completa, negativo = retrasa).")]
        public float potencia = 1f;

        [Tooltip("Daño/Curar/Energía: cantidad fija que se suma al resultado.")]
        public float valorFijo = 0f;

        [Tooltip("Modificar stat: qué stat y cuánto.")]
        public StatModifierDef modificador;

        [Tooltip("Modificar stat / Provocar: duración en segundos.")]
        public float duracion = 15f;

        [Tooltip("Aplicar estado: qué estado.")]
        public StatusData estado;

        [Tooltip("Aplicar estado: probabilidad de que se aplique.")]
        [Range(0f, 1f)] public float probabilidad = 1f;

        public bool EsDano => tipo == EffectType.DanoFisico || tipo == EffectType.DanoEnergia || tipo == EffectType.DanoFijo;
    }
}
