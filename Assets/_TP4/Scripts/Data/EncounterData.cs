using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    [CreateAssetMenu(menuName = "TP4/Encuentro", fileName = "ENC_Nuevo")]
    public class EncounterData : ScriptableObject
    {
        public string nombre = "Ronda 1";
        public string nombreRival = "Rival";
        public List<RobotData> enemigos = new List<RobotData>();
        public bool esJefe = false;
        public AudioClip musica;
    }
}
