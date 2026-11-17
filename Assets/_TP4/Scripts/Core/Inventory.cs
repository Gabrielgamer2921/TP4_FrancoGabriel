using System.Collections.Generic;

namespace TP4
{
    /// <summary>Inventario compartido del equipo del jugador.</summary>
    public class Inventory
    {
        readonly List<ItemData> orden = new List<ItemData>();
        readonly Dictionary<ItemData, int> cantidades = new Dictionary<ItemData, int>();

        public IReadOnlyList<ItemData> Items => orden;

        public int Cantidad(ItemData item) => item != null && cantidades.TryGetValue(item, out int n) ? n : 0;

        public void Agregar(ItemData item, int cantidad)
        {
            if (item == null || cantidad <= 0) return;
            if (!cantidades.ContainsKey(item))
            {
                cantidades[item] = 0;
                orden.Add(item);
            }
            cantidades[item] = System.Math.Min(item.maximoEnInventario, cantidades[item] + cantidad);
        }

        public bool Consumir(ItemData item)
        {
            if (Cantidad(item) <= 0) return false;
            cantidades[item]--;
            return true;
        }

        public void Devolver(ItemData item) => Agregar(item, 1);

        public bool TieneAlgo
        {
            get
            {
                foreach (var kv in cantidades) if (kv.Value > 0) return true;
                return false;
            }
        }

        public Dictionary<ItemData, int> Snapshot() => new Dictionary<ItemData, int>(cantidades);

        public void Restaurar(Dictionary<ItemData, int> snap)
        {
            cantidades.Clear();
            orden.Clear();
            foreach (var kv in snap)
            {
                cantidades[kv.Key] = kv.Value;
                orden.Add(kv.Key);
            }
        }
    }
}
