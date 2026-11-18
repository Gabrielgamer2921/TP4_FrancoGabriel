using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    [CreateAssetMenu(menuName = "TP4/Encuentro", fileName = "ENC_Nuevo")]
    public class EncounterData : ScriptableObject
    {
        public string nombre = "Ronda 1";
        public string nombreRival = "Rival";
        public string nombreEN;
        public string nombreRivalEN;
        public List<RobotData> enemigos = new List<RobotData>();
        public bool esJefe = false;
        public AudioClip musica;

        public string Nombre => Loc.Elegir(nombre, nombreEN);
        public string NombreRival => Loc.Elegir(nombreRival, nombreRivalEN);
    }
}
