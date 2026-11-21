using UnityEngine;

namespace TP4
{
    public static class Formulas
    {
        public static int VidaMax(int vitalidad) => 50 + vitalidad * 10;

        public static float VelocidadATB(int agilidad) => 0.07f + agilidad * 0.006f;

        const float FactorDefensa = 4f;

        public static int DanoFisico(float fuerza, float defensa, float potencia, float valorFijo)
        {
            float bruto = fuerza * 2f * potencia + valorFijo;
            return AplicarDefensa(bruto, defensa);
        }

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

        public static float CargaOverdrivePorDanoRecibido(int dano, int vidaMax) => (float)dano / vidaMax * 0.9f;

        public static float CargaOverdrivePorDanoCausado(int dano, int vidaMaxObjetivo) => (float)dano / vidaMaxObjetivo * 0.35f;
    }
}
