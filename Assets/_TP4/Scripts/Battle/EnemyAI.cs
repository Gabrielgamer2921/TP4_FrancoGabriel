using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    /// <summary>
    /// IA de los enemigos. El CUÁNDO lo decide la ATB; acá se decide QUÉ hacer y A QUIÉN.
    /// Se puntúa cada combinación (acción, objetivo) y se elige la de mayor puntaje,
    /// con un poco de azar para no ser predecible.
    /// </summary>
    public static class EnemyAI
    {
        public static ActionRequest Decidir(BattleUnit yo, BattleManager bm)
        {
            var acciones = new List<AbilityData>();
            if (yo.Data.ataqueBasico != null) acciones.Add(yo.Data.ataqueBasico);
            foreach (var h in yo.Habilidades) if (yo.PuedeUsar(h)) acciones.Add(h);
            if (yo.OverdriveListo) acciones.Add(yo.Data.overdrive);

            ActionRequest mejor = null;
            float mejorPuntaje = float.MinValue;

            foreach (var accion in acciones)
            {
                foreach (var objetivos in OpcionesDeObjetivo(yo, accion, bm))
                {
                    if (objetivos.Count == 0) continue;
                    float p = Puntuar(yo, accion, objetivos);
                    p *= 1f + Random.Range(-yo.Data.azarIA, yo.Data.azarIA);
                    if (p > mejorPuntaje)
                    {
                        mejorPuntaje = p;
                        mejor = new ActionRequest(yo, accion, objetivos);
                    }
                }
            }

            // Respaldo: ataque básico a cualquiera.
            if (mejor == null && yo.Data.ataqueBasico != null)
            {
                var rivales = bm.Vivos(OtroEquipo(yo.Team));
                if (rivales.Count > 0)
                    mejor = new ActionRequest(yo, yo.Data.ataqueBasico, new List<BattleUnit> { rivales[Random.Range(0, rivales.Count)] });
            }
            return mejor;
        }

        static Team OtroEquipo(Team t) => t == Team.Jugador ? Team.Enemigo : Team.Jugador;

        public static IEnumerable<List<BattleUnit>> OpcionesDeObjetivo(BattleUnit yo, ActionData accion, BattleManager bm)
        {
            var aliados = bm.Vivos(yo.Team);
            var rivales = bm.Vivos(OtroEquipo(yo.Team));

            switch (accion.objetivo)
            {
                case TargetMode.EnemigoUnico:
                    // Si alguien provoca, los ataques individuales van a él.
                    var provocadores = rivales.FindAll(u => u.EstaProvocando);
                    foreach (var r in provocadores.Count > 0 ? provocadores : rivales)
                        yield return new List<BattleUnit> { r };
                    break;
                case TargetMode.AliadoUnico:
                    foreach (var a in aliados) yield return new List<BattleUnit> { a };
                    break;
                case TargetMode.TodosEnemigos:
                    yield return rivales;
                    break;
                case TargetMode.TodosAliados:
                    yield return aliados;
                    break;
                case TargetMode.Propio:
                    yield return new List<BattleUnit> { yo };
                    break;
            }
        }

        static float Puntuar(BattleUnit yo, ActionData accion, List<BattleUnit> objetivos)
        {
            float puntaje = 0f;

            foreach (var t in objetivos)
            {
                foreach (var e in accion.efectos)
                {
                    switch (e.tipo)
                    {
                        case EffectType.DanoFisico:
                        case EffectType.DanoEnergia:
                        case EffectType.DanoFijo:
                        {
                            float est = EstimarDano(yo, t, e);
                            // Preferir el daño relativo a la vida (rematar al débil).
                            puntaje += Mathf.Min(est, t.Vida) / t.VidaMax * 100f;
                            if (est >= t.Vida) puntaje += 40f; // bonus por eliminar
                            break;
                        }
                        case EffectType.Curar:
                        {
                            float falta = 1f - t.VidaPct;
                            puntaje += falta > 0.4f ? falta * 130f : -15f;
                            break;
                        }
                        case EffectType.RestaurarEnergia:
                            puntaje += (1f - (float)t.Energia / Mathf.Max(1, t.EnergiaMax)) * 30f;
                            break;
                        case EffectType.ModificarStat:
                        {
                            bool positivo = e.modificador.porcentaje >= 0f;
                            puntaje += t.TieneBuff(e.modificador.stat, positivo) ? -5f : 22f;
                            break;
                        }
                        case EffectType.AplicarEstado:
                            puntaje += e.estado != null && t.TieneEstado(e.estado) ? -5f : 30f * e.probabilidad;
                            break;
                        case EffectType.LimpiarEstados:
                            puntaje += t.Estados.Count * 28f - 10f;
                            break;
                        case EffectType.ModificarATB:
                            if (e.potencia > 0f) puntaje += (t != yo && t.State == UnitState.Cargando && t.ATB < 0.6f) ? 22f : -5f;
                            else puntaje += 18f;
                            break;
                        case EffectType.Provocar:
                        {
                            if (yo.EstaProvocando) { puntaje -= 20f; break; }
                            int heridos = 0;
                            foreach (var a in BattleManager.Instance.Vivos(yo.Team))
                                if (a != yo && a.VidaPct < 0.5f) heridos++;
                            puntaje += heridos * 30f + 5f;
                            break;
                        }
                    }
                }
            }

            if (accion is AbilityData hab)
            {
                puntaje -= hab.costoEnergia * 0.15f;
                if (hab.esOverdrive) puntaje += 35f;
            }
            // Acciones lentas valen un poco menos.
            puntaje /= Mathf.Lerp(1f, accion.recuperacion, 0.35f);
            return puntaje;
        }

        static float EstimarDano(BattleUnit yo, BattleUnit t, ActionEffect e)
        {
            float def = t.GetStat(StatType.Defensa);
            switch (e.tipo)
            {
                case EffectType.DanoFisico:
                    return yo.GetStat(StatType.Fuerza) * 2f * e.potencia * 100f / (100f + def * 4f) + e.valorFijo;
                case EffectType.DanoEnergia:
                    return yo.GetStat(StatType.Magia) * 2f * e.potencia * 100f / (100f + def * 2f) + e.valorFijo;
                default:
                    return e.valorFijo;
            }
        }
    }
}
