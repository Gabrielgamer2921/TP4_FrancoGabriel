using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    [CreateAssetMenu(menuName = "TP4/Estado alterado", fileName = "EST_Nuevo")]
    public class StatusData : ScriptableObject
    {
        public string nombre = "Estado";
        [Tooltip("Texto corto que aparece en la UI (ej: QUE, PAR, COR).")]
        public string abreviatura = "EST";
        public string nombreEN;
        public string abreviaturaEN;
        public Sprite icono;
        public Color color = Color.red;

        [Header("Duración")]
        [Min(0.5f)] public float duracion = 8f;

        [Header("Daño periódico")]
        [Min(0.25f)] public float intervaloTick = 2f;
        [Tooltip("Daño por tick como % de la vida máxima (0.05 = 5%).")]
        [Range(0f, 0.3f)] public float danoPorTickPct = 0f;

        [Header("Control")]
        [Tooltip("Si es true, la barra ATB no avanza mientras dure (parálisis).")]
        public bool congelaATB = false;

        [Tooltip("Si es true, un aliado puede liberarlo con el comando 'Liberar' (gasta su turno). Ej: Red eléctrica.")]
        public bool liberable = false;

        [Header("Modificadores mientras dure")]
        public List<StatModifierDef> modificadores = new List<StatModifierDef>();

        [Header("Presentación")]
        public GameObject vfxLoop;
        public AudioClip sfxAplicar;

        public string Nombre => Loc.Elegir(nombre, nombreEN);
        public string Abreviatura => Loc.Elegir(abreviatura, abreviaturaEN);
    }
}
