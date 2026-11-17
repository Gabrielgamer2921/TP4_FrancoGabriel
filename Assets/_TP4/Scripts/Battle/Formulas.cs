using UnityEngine;

namespace TP4
{
    /// <summary>
    /// Todas las fórmulas de combate en un solo lugar para balancear fácil.
    /// </summary>
    public static class Formulas
    {
        // ---------- Vida ----------
        public static int VidaMax(int vitalidad) => 50 + vitalidad * 10;

        // ---------- ATB ----------
        /// <summary>Cuánto de la barra (0..1) se llena por segundo.</summary>
        public static float VelocidadATB(int agilidad) => 0.07f + agilidad * 0.006f;

        // ---------- Daño ----------
        const float FactorDefensa = 4f;

        public static int DanoFisico(float fuerza, float defensa, float potencia, float valorFijo)
        {
            float bruto = fuerza * 2f * potencia + valorFijo;
            return AplicarDefensa(bruto, defensa);
        }

        /// <summary>El daño de energía ignora la mitad de la defensa.</summary>
        public static int DanoEnergia(float magia, float defensa, float potencia, float valorFijo)
        {
            float bruto = magia * 2f * potencia + valorFijo;
            return AplicarDefensa(bruto, defensa * 0.5f);
        }

        static int AplicarDefensa(float bruto, float defensa)
        {
            float reduccion = 100f / (100f + Mathf.Max(0f, defensa) * FactorDefensa);
            float variacion = Random.Range(0.9f, 1.1f);
            return Mathf.Max(1, Mathf.RoundToInt(bruto * reduccion * variacion));
        }

        public static int Curacion(float magia, float potencia, float valorFijo)
        {
            return Mathf.Max(1, Mathf.RoundToInt(magia * 2f * potencia + valorFijo));
        }

        // ---------- Overdrive ----------
        /// <summary>Carga por recibir daño (proporcional al % de vida perdido).</summary>
        public static float CargaOverdrivePorDanoRecibido(int dano, int vidaMax) => (float)dano / vidaMax * 0.9f;

        /// <summary>Carga por causar daño.</summary>
        public static float CargaOverdrivePorDanoCausado(int dano, int vidaMaxObjetivo) => (float)dano / vidaMaxObjetivo * 0.35f;
    }
}
