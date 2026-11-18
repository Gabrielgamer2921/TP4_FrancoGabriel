using UnityEngine;

namespace TP4
{
    [CreateAssetMenu(menuName = "TP4/Habilidad", fileName = "HAB_Nueva")]
    public class AbilityData : ActionData
    {
        [Header("Habilidad")]
        [Min(0)] public int costoEnergia = 0;
        [Tooltip("Solo se puede usar con la barra de Overdrive llena (la vacía al usarse).")]
        public bool esOverdrive = false;
        [Tooltip("Enfriamiento: cuántas acciones propias tiene que esperar para volver a usarla (0 = sin espera).")]
        [Min(0)] public int enfriamiento = 0;
    }
}
