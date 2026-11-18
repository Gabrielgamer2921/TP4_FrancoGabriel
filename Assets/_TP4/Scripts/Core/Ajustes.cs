using System;
using UnityEngine;

namespace TP4
{
    public enum Idioma
    {
        Espanol,
        Ingles
    }

    /// <summary>
    /// Opciones del jugador (se guardan en PlayerPrefs; NO es un guardado de partida).
    /// </summary>
    public static class Ajustes
    {
        const string KeyATB = "tp4.atbActivo";
        const string KeyVolumen = "tp4.volumen";
        const string KeyIdioma = "tp4.idioma";

        static bool atbActivo;
        static int volumen = 80;
        static Idioma idioma = Idioma.Espanol;

        /// <summary>Se dispara cuando cambia cualquier opción (la UI se refresca).</summary>
        public static event Action Cambio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Cargar()
        {
            Cambio = null;
            atbActivo = PlayerPrefs.GetInt(KeyATB, 0) == 1;
            volumen = Mathf.Clamp(PlayerPrefs.GetInt(KeyVolumen, 80), 1, 100);
            idioma = (Idioma)Mathf.Clamp(PlayerPrefs.GetInt(KeyIdioma, 0), 0, 1);
            AplicarVolumen();
        }

        /// <summary>
        /// true = combate en tiempo real con barras ATB.
        /// false = combate por turnos clásico: primero todo tu equipo, después todos los rivales.
        /// </summary>
        public static bool ATBActivo
        {
            get => atbActivo;
            set
            {
                atbActivo = value;
                PlayerPrefs.SetInt(KeyATB, value ? 1 : 0);
                Guardar();
            }
        }

        /// <summary>Volumen general, de 1 a 100.</summary>
        public static int Volumen
        {
            get => volumen;
            set
            {
                volumen = Mathf.Clamp(value, 1, 100);
                PlayerPrefs.SetInt(KeyVolumen, volumen);
                AplicarVolumen();
                Guardar();
            }
        }

        public static Idioma Idioma
        {
            get => idioma;
            set
            {
                idioma = value;
                PlayerPrefs.SetInt(KeyIdioma, (int)value);
                Guardar();
            }
        }

        public static bool Ingles => idioma == Idioma.Ingles;

        /// <summary>
        /// Con la ATB activada se usa el modo "Espera" (como FF7): el tiempo corre, pero se frena
        /// en los submenús y durante las animaciones. Con la ATB desactivada el combate es por turnos
        /// (ver BattleManager.ModoTurnos) y este valor no se usa.
        /// </summary>
        public static ModoATB ModoATB => atbActivo ? ModoATB.Espera : ModoATB.Clasico;

        static void AplicarVolumen() => AudioListener.volume = volumen / 100f;

        static void Guardar()
        {
            PlayerPrefs.Save();
            Cambio?.Invoke();
        }
    }
}
