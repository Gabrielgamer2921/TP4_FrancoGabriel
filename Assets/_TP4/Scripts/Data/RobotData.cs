using System;
using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    /// <summary>
    /// Definición de un robot. La usan tanto los aliados como los enemigos y el jefe.
    /// </summary>
    [CreateAssetMenu(menuName = "TP4/Robot", fileName = "ROB_Nuevo")]
    public class RobotData : ScriptableObject
    {
        [Header("Identidad")]
        public string nombre = "Robot";
        public RobotRole rol = RobotRole.Asalto;
        [TextArea(2, 4)] public string descripcion;
        public Sprite retrato;

        [Header("Modelo")]
        [Tooltip("Prefab/FBX del modelo con Animator. Si está vacío se usa una cápsula de color.")]
        public GameObject modelo;
        public float escala = 1f;
        [Tooltip("Color del placeholder y de la UI.")]
        public Color colorPrincipal = Color.gray;
        [Tooltip("Override del Animator Controller (opcional). Si es null se usa el del modelo.")]
        public RuntimeAnimatorController animatorController;

        [Header("Estadísticas")]
        public Stats stats = new Stats(15, 15, 20, 15, 10);
        [Min(0)] public int energiaMax = 50;

        [Header("Acciones")]
        public AbilityData ataqueBasico;
        public List<AbilityData> habilidades = new List<AbilityData>();
        public AbilityData overdrive;

        [Header("IA (solo enemigos)")]
        [Tooltip("0 = siempre la mejor jugada, 1 = bastante aleatorio.")]
        [Range(0f, 1f)] public float azarIA = 0.25f;
        [Tooltip("Fases del jefe: se activan al bajar de cierto % de vida.")]
        public List<BossPhase> fases = new List<BossPhase>();

        public int VidaMax => Formulas.VidaMax(stats.vitalidad);
    }

    [Serializable]
    public class BossPhase
    {
        public string mensaje = "¡El jefe se enfurece!";
        [Range(0.05f, 0.95f)] public float umbralVida = 0.5f;
        [Tooltip("Multiplica la velocidad de la ATB.")]
        [Min(0.1f)] public float multiplicadorVelocidad = 1.3f;
        public List<AbilityData> habilidadesNuevas = new List<AbilityData>();
        public Color colorTinte = new Color(1f, 0.3f, 0.2f);
    }
}
