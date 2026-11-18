using System;
using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    [Serializable]
    public class ItemStack
    {
        public ItemData item;
        [Min(0)] public int cantidad = 1;
    }

    /// <summary>Configuración global del torneo: plantel, peleas e inventario inicial.</summary>
    [CreateAssetMenu(menuName = "TP4/Torneo", fileName = "Torneo")]
    public class TournamentData : ScriptableObject
    {
        public string nombre = "Torneo de Mechas";
        [Tooltip("Robots que el jugador puede elegir.")]
        public List<RobotData> plantel = new List<RobotData>();
        [Min(1)] public int tamanoEquipo = 3;
        [Tooltip("Peleas en orden. La última suele ser el jefe.")]
        public List<EncounterData> peleas = new List<EncounterData>();
        [Tooltip("Acción común 'Liberar' (aparece en el menú cuando un aliado está atrapado, ej: Red eléctrica).")]
        public AbilityData accionLiberar;
        [Tooltip("Inventario con el que arranca el torneo. No se repone entre peleas.")]
        public List<ItemStack> inventarioInicial = new List<ItemStack>();

        [Header("Ritmo")]
        [Tooltip("Multiplicador global de la velocidad de la ATB.")]
        [Range(0.25f, 3f)] public float velocidadBatalla = 1f;
    }
}
