using UnityEngine;

namespace TP4
{
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
        [Tooltip("Prefabs de UI editables (TP4 > Generar UI editable). Si están vacíos, la UI se arma por código.")]
        public GameObject prefabUICombate;
        public GameObject prefabUIMenu;
        public GameObject prefabUIPausa;
        [Tooltip("Sprites de degradé de las ventanas y barras (se generan como assets).")]
        public Sprite spriteVentana;
        public Sprite spriteRelleno;
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
