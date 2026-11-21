using System;
using UnityEngine;

namespace TP4
{
    public enum Idioma
    {
        Espanol,
        Ingles
    }

    public static class Ajustes
    {
        const string KeyATB = "tp4.atbActivo";
        const string KeyVolumen = "tp4.volumen";
        const string KeyIdioma = "tp4.idioma";

        static bool atbActivo;
        static int volumen = 80;
        static Idioma idioma = Idioma.Espanol;

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

        public static ModoATB ModoATB => atbActivo ? ModoATB.Espera : ModoATB.Clasico;

        static void AplicarVolumen() => AudioListener.volume = volumen / 100f;

        static void Guardar()
        {
            PlayerPrefs.Save();
            Cambio?.Invoke();
        }
    }
}
