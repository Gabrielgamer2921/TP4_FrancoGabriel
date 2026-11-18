using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    /// <summary>Base común de habilidades e ítems: a quién apunta, qué efectos tiene y cómo se ve.</summary>
    public abstract class ActionData : ScriptableObject
    {
        [Header("General")]
        public string nombre = "Acción";
        [TextArea(2, 4)] public string descripcion;
        [Tooltip("Nombre en inglés (si está vacío se usa el español).")]
        public string nombreEN;
        [TextArea(2, 4)] public string descripcionEN;
        public Sprite icono;
        public TargetMode objetivo = TargetMode.EnemigoUnico;

        [Tooltip("Costo de recuperación ATB. 1 = normal. 1.6 = tarda 60% más en volver a actuar. 0.6 = vuelve rápido.")]
        [Range(0.2f, 3f)] public float recuperacion = 1f;

        public List<ActionEffect> efectos = new List<ActionEffect>();

        [Header("Presentación")]
        public ActionAnim animacion = ActionAnim.Ataque;
        [Tooltip("Si es true, el robot avanza hacia el objetivo antes de golpear (cuerpo a cuerpo).")]
        public bool cuerpoACuerpo = false;
        [Tooltip("Color del proyectil / destello si no hay VFX asignado.")]
        public Color colorEfecto = new Color(0.4f, 0.8f, 1f);
        public GameObject vfxLanzamiento;
        public GameObject vfxImpacto;
        public AudioClip sfxLanzamiento;
        public AudioClip sfxImpacto;

        [Header("Cinemática (oculta la UI)")]
        public bool esCinematica = false;
        [Tooltip("Duración de la toma de cámara antes del impacto.")]
        public float duracionCinematica = 2.5f;

        public string Nombre => Loc.Elegir(nombre, nombreEN);
        public string Descripcion => Loc.Elegir(descripcion, descripcionEN);

        public bool ApuntaAEnemigos => objetivo == TargetMode.EnemigoUnico || objetivo == TargetMode.TodosEnemigos;
        public bool EsMultiObjetivo => objetivo == TargetMode.TodosEnemigos || objetivo == TargetMode.TodosAliados;
        public bool RequiereElegirObjetivo => objetivo == TargetMode.EnemigoUnico || objetivo == TargetMode.AliadoUnico;

        public bool HaceDano
        {
            get
            {
                foreach (var e in efectos) if (e.EsDano) return true;
                return false;
            }
        }
    }
}
