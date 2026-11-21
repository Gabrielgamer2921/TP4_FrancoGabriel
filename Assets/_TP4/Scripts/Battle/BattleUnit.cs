using System;
using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    public enum UnitState
    {
        Cargando,
        Listo,
        EnCola,
        Actuando,
        Muerto
    }

    public class StatusInstance
    {
        public StatusData data;
        public float restante;
        public float tickTimer;
        public GameObject vfx;
    }

    public class BuffInstance
    {
        public StatModifierDef mod;
        public float restante;
    }

    public class BattleUnit : MonoBehaviour
    {
        public RobotData Data { get; private set; }
        public Team Team { get; private set; }
        public int Slot { get; private set; }
        public UnitView View { get; private set; }

        public int Vida { get; private set; }
        public int VidaMax { get; private set; }
        public int Energia { get; private set; }
        public int EnergiaMax { get; private set; }

        public float ATB { get; set; }
        public float Overdrive { get; private set; }
        public UnitState State { get; set; }
        public float MultiplicadorVelocidad { get; set; } = 1f;
        public float ProvocarRestante { get; private set; }
        public bool AccionExtra { get; set; }
        public int AccionesPorTurno { get; private set; } = 1;

        public ActionData UltimaAccion { get; set; }
        public int RachaSoporte { get; set; }

        public readonly List<StatusInstance> Estados = new List<StatusInstance>();
        public readonly List<BuffInstance> Buffs = new List<BuffInstance>();
        public readonly List<AbilityData> Habilidades = new List<AbilityData>();
        readonly HashSet<int> fasesActivadas = new HashSet<int>();
        float multStats = 1f;

        public bool IsAlive => Vida > 0;
        public bool EsJugador => Team == Team.Jugador;
        public bool EstaProvocando => ProvocarRestante > 0f;
        public bool OverdriveListo => Overdrive >= 1f && Data.overdrive != null;
        public float VidaPct => VidaMax > 0 ? (float)Vida / VidaMax : 0f;
        public string Nombre => Data != null ? Data.nombre : name;

        public bool ATBCongelada
        {
            get
            {
                foreach (var s in Estados) if (s.data.congelaATB) return true;
                return false;
            }
        }

        public event Action<BattleUnit> Changed;
        public event Action<BattleUnit, int> Damaged;
        public event Action<BattleUnit, int> Healed;
        public event Action<BattleUnit, int> EnergyRestored;
        public event Action<BattleUnit, string, Color> Message;
        public event Action<BattleUnit> Died;
        public event Action<BattleUnit, BossPhase> PhaseTriggered;
        public event Action<BattleUnit, StatusData> EstadoAplicado;

        public void Init(RobotData data, Team team, int slot, UnitView view, float multiplicadorStats = 1f)
        {
            Data = data;
            Team = team;
            Slot = slot;
            View = view;
            multStats = Mathf.Max(0.1f, multiplicadorStats);
            VidaMax = Formulas.VidaMax(Mathf.RoundToInt(data.stats.vitalidad * multStats));
            Vida = VidaMax;
            EnergiaMax = data.energiaMax;
            Energia = EnergiaMax;
            Overdrive = 0f;
            State = UnitState.Cargando;
            ATB = UnityEngine.Random.Range(0f, 0.45f);
            Habilidades.Clear();
            Habilidades.AddRange(data.habilidades);
            AccionesPorTurno = Mathf.Max(1, data.accionesPorTurno);
            name = $"{team}_{slot}_{data.nombre}";
        }

        public int GetStat(StatType type)
        {
            float pct = 0f;
            foreach (var b in Buffs) if (b.mod.stat == type) pct += b.mod.porcentaje;
            foreach (var s in Estados)
                foreach (var m in s.data.modificadores)
                    if (m.stat == type) pct += m.porcentaje;
            pct = Mathf.Max(-0.9f, pct);
            float v = Data.stats.Get(type) * multStats * (1f + pct);
            return Mathf.Max(type == StatType.Defensa ? 0 : 1, Mathf.RoundToInt(v));
        }

        public float StatModPct(StatType type)
        {
            float pct = 0f;
            foreach (var b in Buffs) if (b.mod.stat == type) pct += b.mod.porcentaje;
            foreach (var s in Estados)
                foreach (var m in s.data.modificadores)
                    if (m.stat == type) pct += m.porcentaje;
            return pct;
        }

        public bool PuedeUsar(AbilityData a)
        {
            if (a == null || !IsAlive) return false;
            if (a.esOverdrive) return OverdriveListo;
            if (EnfriamientoRestante(a) > 0) return false;
            return Energia >= a.costoEnergia;
        }

        readonly Dictionary<AbilityData, int> enfriamientos = new Dictionary<AbilityData, int>();

        public int EnfriamientoRestante(AbilityData a) => a != null && enfriamientos.TryGetValue(a, out int n) ? n : 0;

        public void RegistrarUso(ActionData accion)
        {
            var claves = new List<AbilityData>(enfriamientos.Keys);
            foreach (var k in claves) enfriamientos[k] = Mathf.Max(0, enfriamientos[k] - 1);
            if (accion is AbilityData hab && hab.enfriamiento > 0) enfriamientos[hab] = hab.enfriamiento;
        }

        public bool TickTiempo(float dt, float velocidadGlobal)
        {
            if (!IsAlive) return false;

            TickEfectos(dt);
            if (!IsAlive) return false;

            if (State == UnitState.Cargando && !ATBCongelada)
            {
                ATB += Formulas.VelocidadATB(GetStat(StatType.Agilidad)) * MultiplicadorVelocidad * velocidadGlobal * dt;
                if (ATB >= 1f)
                {
                    ATB = 1f;
                    State = UnitState.Listo;
                    Changed?.Invoke(this);
                    return true;
                }
            }
            return false;
        }

        public void AvanzarTurno(float segundos)
        {
            if (!IsAlive) return;
            TickEfectos(segundos);
        }

        void TickEfectos(float dt)
        {
            TickEstados(dt);
            if (!IsAlive) return;

            for (int i = Buffs.Count - 1; i >= 0; i--)
            {
                Buffs[i].restante -= dt;
                if (Buffs[i].restante <= 0f) { Buffs.RemoveAt(i); Changed?.Invoke(this); }
            }

            if (ProvocarRestante > 0f)
            {
                ProvocarRestante -= dt;
                if (ProvocarRestante <= 0f) { ProvocarRestante = 0f; Changed?.Invoke(this); }
            }
        }

        void TickEstados(float dt)
        {
            for (int i = Estados.Count - 1; i >= 0; i--)
            {
                var s = Estados[i];
                s.restante -= dt;
                if (s.data.danoPorTickPct > 0f)
                {
                    s.tickTimer -= dt;
                    while (s.tickTimer <= 0f)
                    {
                        s.tickTimer += s.data.intervaloTick;
                        int dano = Mathf.Max(1, Mathf.RoundToInt(VidaMax * s.data.danoPorTickPct));
                        RecibirDano(dano, null, false);
                        if (!IsAlive) return;
                    }
                }
                if (s.restante <= 0f)
                {
                    QuitarEstado(i);
                }
            }
        }

        public int RecibirDano(int cantidad, BattleUnit atacante, bool cargaOverdrive = true)
        {
            if (!IsAlive) return 0;
            int real = Mathf.Min(cantidad, Vida);
            Vida -= real;
            if (cargaOverdrive) AgregarOverdrive(Formulas.CargaOverdrivePorDanoRecibido(cantidad, VidaMax));
            if (atacante != null && atacante.IsAlive)
                atacante.AgregarOverdrive(Formulas.CargaOverdrivePorDanoCausado(cantidad, VidaMax));

            Damaged?.Invoke(this, cantidad);
            if (Vida <= 0)
            {
                Vida = 0;
                Morir();
            }
            else
            {
                RevisarFases();
            }
            Changed?.Invoke(this);
            return real;
        }

        public void Curar(int cantidad)
        {
            if (!IsAlive) return;
            Vida = Mathf.Min(VidaMax, Vida + cantidad);
            Healed?.Invoke(this, cantidad);
            Changed?.Invoke(this);
        }

        public void RestaurarEnergia(int cantidad)
        {
            if (!IsAlive) return;
            Energia = Mathf.Min(EnergiaMax, Energia + cantidad);
            EnergyRestored?.Invoke(this, cantidad);
            Changed?.Invoke(this);
        }

        public void GastarEnergia(int cantidad)
        {
            Energia = Mathf.Max(0, Energia - cantidad);
            Changed?.Invoke(this);
        }

        public void AgregarOverdrive(float cantidad)
        {
            if (Data.overdrive == null || !IsAlive) return;
            bool estabaLleno = Overdrive >= 1f;
            Overdrive = Mathf.Clamp01(Overdrive + cantidad);
            if (!estabaLleno && Overdrive >= 1f)
                Message?.Invoke(this, Loc.T("msg.overdrive"), new Color(1f, 0.85f, 0.2f));
            Changed?.Invoke(this);
        }

        public void VaciarOverdrive()
        {
            Overdrive = 0f;
            Changed?.Invoke(this);
        }

        public void AplicarBuff(StatModifierDef mod, float duracion)
        {
            foreach (var b in Buffs)
            {
                if (b.mod.stat == mod.stat && Mathf.Sign(b.mod.porcentaje) == Mathf.Sign(mod.porcentaje))
                {
                    b.mod = mod;
                    b.restante = Mathf.Max(b.restante, duracion);
                    MensajeBuff(mod);
                    Changed?.Invoke(this);
                    return;
                }
            }
            Buffs.Add(new BuffInstance { mod = mod, restante = duracion });
            MensajeBuff(mod);
            Changed?.Invoke(this);
        }

        void MensajeBuff(StatModifierDef mod)
        {
            bool sube = mod.porcentaje >= 0f;
            string txt = $"{Loc.Stat(mod.stat)} {(sube ? "▲" : "▼")}";
            Message?.Invoke(this, txt, sube ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.5f, 0.3f));
        }

        public bool TieneEstadoLiberable
        {
            get
            {
                foreach (var s in Estados) if (s.data.liberable) return true;
                return false;
            }
        }

        public int Liberar()
        {
            int n = 0;
            for (int i = Estados.Count - 1; i >= 0; i--)
                if (Estados[i].data.liberable) { QuitarEstado(i); n++; }
            if (n > 0) Message?.Invoke(this, Loc.T("msg.liberado"), new Color(0.5f, 1f, 1f));
            Changed?.Invoke(this);
            return n;
        }

        public bool TieneEstado(StatusData data)
        {
            foreach (var s in Estados) if (s.data == data) return true;
            return false;
        }

        public bool TieneBuff(StatType stat, bool positivo)
        {
            foreach (var b in Buffs)
                if (b.mod.stat == stat && (b.mod.porcentaje >= 0f) == positivo) return true;
            return false;
        }

        public void AplicarEstado(StatusData data)
        {
            if (!IsAlive || data == null) return;
            foreach (var s in Estados)
            {
                if (s.data == data)
                {
                    s.restante = data.duracion;
                    Message?.Invoke(this, data.Nombre, data.color);
                    Changed?.Invoke(this);
                    return;
                }
            }

            var inst = new StatusInstance { data = data, restante = data.duracion, tickTimer = data.intervaloTick };
            if (View != null) inst.vfx = View.CrearVfxEstado(data);
            Estados.Add(inst);

            if (data.congelaATB && State == UnitState.Listo)
            {
                State = UnitState.Cargando;
                ATB = 0.9f;
            }

            Message?.Invoke(this, data.Nombre, data.color);
            EstadoAplicado?.Invoke(this, data);
            Changed?.Invoke(this);
        }

        void QuitarEstado(int index)
        {
            var s = Estados[index];
            if (s.vfx != null) Destroy(s.vfx);
            Estados.RemoveAt(index);
            Changed?.Invoke(this);
        }

        public int LimpiarEstados()
        {
            int n = Estados.Count;
            for (int i = Estados.Count - 1; i >= 0; i--) QuitarEstado(i);
            Buffs.RemoveAll(b => b.mod.porcentaje < 0f);
            if (n > 0) Message?.Invoke(this, Loc.T("msg.purgado"), Color.white);
            Changed?.Invoke(this);
            return n;
        }

        public void Provocar(float duracion)
        {
            ProvocarRestante = Mathf.Max(ProvocarRestante, duracion);
            Message?.Invoke(this, Loc.T("msg.provoca"), new Color(1f, 0.6f, 0.2f));
            Changed?.Invoke(this);
        }

        public void ModificarATB(float cantidad)
        {
            if (!IsAlive) return;

            var bm = BattleManager.Instance;
            if (bm != null && bm.ModoTurnos)
            {
                if (cantidad >= 0.5f)
                {
                    AccionExtra = true;
                    Message?.Invoke(this, Loc.T("msg.accionExtra"), new Color(0.4f, 0.9f, 1f));
                    Changed?.Invoke(this);
                }
                return;
            }

            if (State == UnitState.Cargando)
            {
                ATB = Mathf.Clamp(ATB + cantidad, -1f, 0.999f);
            }
            Message?.Invoke(this, cantidad >= 0f ? "ATB ▲" : "ATB ▼", cantidad >= 0f ? new Color(0.4f, 0.9f, 1f) : new Color(0.7f, 0.5f, 1f));
            Changed?.Invoke(this);
        }

        public void TerminarAccion(float recuperacion)
        {
            if (!IsAlive) return;
            ATB = 1f - recuperacion;
            State = UnitState.Cargando;
            Changed?.Invoke(this);
        }

        public void NotificarCambio() => Changed?.Invoke(this);

        void Morir()
        {
            State = UnitState.Muerto;
            ATB = 0f;
            Overdrive = 0f;
            ProvocarRestante = 0f;
            for (int i = Estados.Count - 1; i >= 0; i--) QuitarEstado(i);
            Buffs.Clear();
            Died?.Invoke(this);
        }

        void RevisarFases()
        {
            if (Data.fases == null) return;
            for (int i = 0; i < Data.fases.Count; i++)
            {
                if (fasesActivadas.Contains(i)) continue;
                var f = Data.fases[i];
                if (VidaPct <= f.umbralVida)
                {
                    fasesActivadas.Add(i);
                    MultiplicadorVelocidad *= f.multiplicadorVelocidad;
                    AccionesPorTurno += f.accionesExtra;
                    if (f.bonusStats != null)
                        foreach (var b in f.bonusStats) Buffs.Add(new BuffInstance { mod = b, restante = float.MaxValue });
                    foreach (var h in f.habilidadesNuevas)
                        if (h != null && !Habilidades.Contains(h)) Habilidades.Add(h);
                    PhaseTriggered?.Invoke(this, f);
                }
            }
        }
    }
}
