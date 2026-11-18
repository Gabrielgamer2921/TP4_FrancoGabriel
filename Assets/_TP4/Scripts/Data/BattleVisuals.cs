using UnityEngine;

namespace TP4
{
    /// <summary>
    /// Recursos visuales/sonoros compartidos. Un solo lugar para cambiar materiales, fuente y sonidos.
    /// Cualquier sonido vacío se reemplaza por uno procedural (bip) para que siempre haya feedback.
    /// </summary>
    [CreateAssetMenu(menuName = "TP4/Visuales de batalla", fileName = "BattleVisuals")]
    public class BattleVisuals : ScriptableObject
    {
        [Header("Materiales (se crean con el asistente)")]
        public Material materialPlaceholder;
        public Material materialEmisivo;
        public Material materialTrail;
        public Material materialPiso;

        [Header("UI")]
        public Font fuente;
        public Color colorPanelArriba = new Color(0.16f, 0.24f, 0.72f, 0.97f);
        public Color colorPanelAbajo = new Color(0.03f, 0.05f, 0.28f, 0.97f);
        public Color colorBorde = new Color(0.85f, 0.88f, 0.95f, 1f);

        [Header("Sonidos de UI")]
        public AudioClip sfxMover;
        public AudioClip sfxConfirmar;
        public AudioClip sfxCancelar;
        public AudioClip sfxListo;

        [Header("Sonidos de combate")]
        public AudioClip sfxGolpe;
        public AudioClip sfxDisparo;
        public AudioClip sfxCurar;
        public AudioClip sfxEstado;
        public AudioClip sfxMuerte;
        public AudioClip sfxVictoria;
        public AudioClip sfxDerrota;
        public AudioClip sfxOverdrive;

        [Header("Música")]
        public AudioClip musicaMenu;
        public AudioClip musicaCombate;
        public AudioClip musicaJefe;
        [Range(0f, 1f)] public float volumenMusica = 0.5f;
    }
}
