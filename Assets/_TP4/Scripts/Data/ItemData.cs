using UnityEngine;

namespace TP4
{
    [CreateAssetMenu(menuName = "TP4/Ítem", fileName = "ITEM_Nuevo")]
    public class ItemData : ActionData
    {
        [Header("Ítem")]
        [Tooltip("Los ítems no escalan con stats: usá 'Valor fijo' en los efectos.")]
        [Min(1)] public int maximoEnInventario = 9;
    }
}
