using UnityEngine;
using UnityEngine.UI;

namespace TP4
{
    /// <summary>
    /// Texto traducible.
    /// • Texto fijo: el ESPAÑOL es lo que escribas en el componente Text (como cualquier texto);
    ///   acá solo completás el INGLÉS.
    /// • Plantilla (ej: "Ronda {0}/{1}"): el juego completa los {0}, {1}... Editá las plantillas
    ///   en los campos Español / Inglés de este componente.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class TextoLoc : MonoBehaviour
    {
        [Tooltip("Plantilla con {0}, {1}... que completa el juego (ej: 'Ronda {0}/{1}'). " +
                 "Si no es plantilla, el español es el texto del componente Text.")]
        public bool esPlantilla;

        [Tooltip("Si NO es plantilla, se copia solo del componente Text (editá el Text).")]
        [TextArea(1, 3)] public string espanol;

        [TextArea(1, 3)] public string ingles;

        Text texto;
        string espanolOriginal;

        void Awake()
        {
            texto = GetComponent<Text>();
            espanolOriginal = esPlantilla || texto == null ? espanol : texto.text;
        }

        void OnEnable()
        {
            if (esPlantilla) return;
            Ajustes.Cambio += Refrescar;
            Refrescar();
        }

        void OnDisable()
        {
            Ajustes.Cambio -= Refrescar;
        }

        /// <summary>Plantilla en el idioma actual (para textos con {0}, {1}...).</summary>
        public string Plantilla => Ajustes.Ingles && !string.IsNullOrEmpty(ingles) ? ingles : espanol;

        public void Refrescar()
        {
            if (esPlantilla) return;
            if (texto == null) texto = GetComponent<Text>();
            if (texto == null) return;
            if (espanolOriginal == null) espanolOriginal = texto.text;
            texto.text = Ajustes.Ingles && !string.IsNullOrEmpty(ingles) ? ingles : espanolOriginal;
        }

        /// <summary>Escribe la plantilla completada con los valores.</summary>
        public void Formatear(params object[] valores)
        {
            if (texto == null) texto = GetComponent<Text>();
            if (texto == null) return;
            try { texto.text = string.Format(Plantilla, valores); }
            catch (System.FormatException) { texto.text = Plantilla; }
        }

#if UNITY_EDITOR
        // En el editor, el campo "Español" sigue al componente Text (que es el que se edita).
        void OnValidate()
        {
            if (Application.isPlaying || esPlantilla) return;
            var t = GetComponent<Text>();
            if (t != null) espanol = t.text;
        }
#endif
    }
}
