using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TP4
{
    public enum BattlePhase
    {
        Intro,
        EnCurso,
        Victoria,
        Derrota
    }

    /// <summary>
    /// Orquesta el combate: crea los robots, avanza las barras ATB (modo activo),
    /// pide decisiones a la IA, ejecuta la cola de acciones y detecta victoria/derrota.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("Datos")]
        [SerializeField] TournamentData torneo;
        [SerializeField] BattleVisuals visuales;

        [Header("Escena")]
        [SerializeField] Transform[] spawnsJugador = new Transform[3];
        [SerializeField] Transform[] spawnsEnemigo = new Transform[3];
        [SerializeField] Transform spawnJefe;
        [SerializeField] BattleCamera camara;

        [Header("Flujo")]
        [Tooltip("Escena de selección de equipo (si no está en Build Settings, se reinicia el torneo con el equipo actual).")]
        [SerializeField] string escenaSeleccion = "SeleccionEquipo";

        public readonly List<BattleUnit> Jugadores = new List<BattleUnit>();
        public readonly List<BattleUnit> Enemigos = new List<BattleUnit>();
        /// <summary>Robots del jugador con la barra llena esperando una orden (en orden de llegada).</summary>
        public readonly List<BattleUnit> ListosJugador = new List<BattleUnit>();

        readonly Queue<ActionRequest> cola = new Queue<ActionRequest>();
        bool ejecutando;

        public BattlePhase Fase { get; private set; } = BattlePhase.Intro;
        public bool PausaCinematica { get; private set; }
        public BattleUI UI { get; private set; }
        public EncounterData Encuentro { get; private set; }
        public BattleVisuals Visuales => visuales;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (torneo == null)
            {
                Debug.LogError("[TP4] BattleManager no tiene TournamentData asignado. Usá el menú TP4 > Crear contenido y escena.");
                enabled = false;
                return;
            }
            if (camara == null && Camera.main != null) camara = Camera.main.GetComponent<BattleCamera>();

            GameSession.AsegurarIniciada(torneo);
            GameSession.GuardarInicioPelea();
            Encuentro = GameSession.PeleaData;
            if (Encuentro == null)
            {
                Debug.LogError("[TP4] El torneo no tiene peleas configuradas.");
                enabled = false;
                return;
            }

            Sfx.Crear(visuales);
            CrearUnidades();
            UI = BattleUI.Crear(this, visuales);
            UI.SetRonda($"Ronda {GameSession.PeleaActual + 1}/{torneo.peleas.Count}  ·  vs {Encuentro.nombreRival}");

            var musica = Encuentro.musica != null ? Encuentro.musica
                : visuales != null ? (Encuentro.esJefe ? visuales.musicaJefe : visuales.musicaCombate) : null;
            Sfx.Musica(musica);

            StartCoroutine(Intro());
        }

        IEnumerator Intro()
        {
            Fase = BattlePhase.Intro;
            yield return new WaitForSeconds(0.3f);
            string titulo = Encuentro.esJefe ? $"¡PELEA FINAL!\n<size=40>{Encuentro.nombreRival}</size>" : $"{Encuentro.nombre}\n<size=40>vs {Encuentro.nombreRival}</size>";
            UI.Banner(titulo, 1.6f, Encuentro.esJefe ? new Color(1f, 0.45f, 0.35f) : Color.white);
            yield return new WaitForSeconds(2.1f);
            Fase = BattlePhase.EnCurso;
        }

        // ---------------------------------------------------------------------
        // Creación
        // ---------------------------------------------------------------------
        void CrearUnidades()
        {
            var equipo = GameSession.Equipo;
            for (int i = 0; i < equipo.Count; i++)
            {
                var spawn = i < spawnsJugador.Length ? spawnsJugador[i] : null;
                Jugadores.Add(CrearUnidad(equipo[i], Team.Jugador, i, spawn));
            }

            var enemigos = Encuentro.enemigos;
            bool jefeSolo = Encuentro.esJefe && enemigos.Count == 1 && spawnJefe != null;
            for (int i = 0; i < enemigos.Count; i++)
            {
                var spawn = jefeSolo ? spawnJefe : (i < spawnsEnemigo.Length ? spawnsEnemigo[i] : null);
                Enemigos.Add(CrearUnidad(enemigos[i], Team.Enemigo, i, spawn));
            }
        }

        BattleUnit CrearUnidad(RobotData data, Team team, int slot, Transform spawn)
        {
            var go = new GameObject(data.nombre);
            if (spawn != null) go.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            else go.transform.position = new Vector3(team == Team.Jugador ? 4f : -4f, 0f, slot * 2.5f - 2.5f);

            var unit = go.AddComponent<BattleUnit>();
            var view = go.AddComponent<UnitView>();
            unit.Init(data, team, slot, view);
            view.Construir(unit, visuales);

            unit.Died += AlMorir;
            unit.PhaseTriggered += (u, fase) =>
            {
                UI.Banner(fase.mensaje, 1.4f, new Color(1f, 0.4f, 0.3f));
                if (camara != null) camara.Shake(0.3f, 0.5f);
                Sfx.Play(SfxId.Overdrive, 0.8f, 0.7f);
            };
            return unit;
        }

        // ---------------------------------------------------------------------
        // Bucle principal
        // ---------------------------------------------------------------------
        void Update()
        {
            if (Fase != BattlePhase.EnCurso) return;

            if (!PausaCinematica)
            {
                float dt = Time.deltaTime;
                float velocidad = torneo.velocidadBatalla;
                foreach (var u in Jugadores) if (u.TickTiempo(dt, velocidad)) AlQuedarListo(u);
                foreach (var u in Enemigos) if (u.TickTiempo(dt, velocidad)) AlQuedarListo(u);
            }

            // Los que murieron o fueron paralizados dejan de estar listos.
            ListosJugador.RemoveAll(u => !u.IsAlive || u.State != UnitState.Listo);

            if (!ejecutando && cola.Count > 0 && Fase == BattlePhase.EnCurso)
                StartCoroutine(Ejecutar(cola.Dequeue()));
        }

        void AlQuedarListo(BattleUnit u)
        {
            if (u.EsJugador)
            {
                if (!ListosJugador.Contains(u)) ListosJugador.Add(u);
            }
            else
            {
                var req = EnemyAI.Decidir(u, this);
                if (req != null) Encolar(req);
                else u.TerminarAccion(0.5f);
            }
        }

        /// <summary>Pasa el primer robot listo al final de la fila (comando "Cambiar").</summary>
        public void RotarListos()
        {
            if (ListosJugador.Count < 2) return;
            var primero = ListosJugador[0];
            ListosJugador.RemoveAt(0);
            ListosJugador.Add(primero);
        }

        public void Encolar(ActionRequest req)
        {
            if (req == null || req.actor == null || req.accion == null) return;
            req.actor.State = UnitState.EnCola;
            ListosJugador.Remove(req.actor);
            // El ítem se reserva al elegirlo para que otro robot no use el mismo.
            if (req.Item != null) GameSession.Inventario.Consumir(req.Item);
            cola.Enqueue(req);
        }

        // ---------------------------------------------------------------------
        // Ejecución de acciones
        // ---------------------------------------------------------------------
        IEnumerator Ejecutar(ActionRequest req)
        {
            ejecutando = true;
            var actor = req.actor;
            var accion = req.accion;

            if (!actor.IsAlive)
            {
                if (req.Item != null) GameSession.Inventario.Devolver(req.Item);
                ejecutando = false;
                yield break;
            }

            var objetivos = ResolverObjetivos(req);
            var hab = req.Habilidad;
            bool sinRecursos = hab != null && !actor.PuedeUsar(hab);
            if (objetivos.Count == 0 || sinRecursos)
            {
                if (req.Item != null) GameSession.Inventario.Devolver(req.Item);
                actor.TerminarAccion(0.3f);
                ejecutando = false;
                yield break;
            }

            actor.State = UnitState.Actuando;
            if (hab != null)
            {
                if (hab.esOverdrive) actor.VaciarOverdrive();
                else if (hab.costoEnergia > 0) actor.GastarEnergia(hab.costoEnergia);
            }

            Color colorNombre = actor.EsJugador ? Color.white : new Color(1f, 0.6f, 0.55f);
            if (hab != null && hab.esOverdrive) colorNombre = new Color(1f, 0.85f, 0.2f);
            UI.AnunciarAccion(req.Item != null ? $"{actor.Nombre}: {accion.nombre}" : accion.nombre, colorNombre);

            bool cine = accion.esCinematica;
            if (cine)
            {
                PausaCinematica = true;
                UI.Ocultar(true);
                Sfx.Play(SfxId.Overdrive, 1f);
                if (camara != null) yield return camara.Cinematica(actor, objetivos, accion.duracionCinematica);
            }

            yield return actor.View.Preparar(accion, objetivos);

            // ---- Impacto ----
            bool overdrive = hab != null && hab.esOverdrive;
            foreach (var t in objetivos)
            {
                if (!t.IsAlive) continue;
                t.View.Impacto(accion);
                AplicarEfectos(actor, accion, t, overdrive);
            }
            if (accion.HaceDano && camara != null) camara.Shake(cine ? 0.35f : 0.12f, cine ? 0.45f : 0.15f);

            yield return new WaitForSeconds(cine ? 1f : 0.45f);
            yield return actor.View.Volver();

            if (cine)
            {
                if (camara != null) yield return camara.Restaurar();
                UI.Ocultar(false);
                PausaCinematica = false;
            }

            actor.TerminarAccion(accion.recuperacion);
            ejecutando = false;
            RevisarFin();
        }

        List<BattleUnit> ResolverObjetivos(ActionRequest req)
        {
            var actor = req.actor;
            Team rival = actor.Team == Team.Jugador ? Team.Enemigo : Team.Jugador;
            switch (req.accion.objetivo)
            {
                case TargetMode.Propio:
                    return new List<BattleUnit> { actor };
                case TargetMode.TodosEnemigos:
                    return Vivos(rival);
                case TargetMode.TodosAliados:
                    return Vivos(actor.Team);
                default:
                {
                    // Objetivo único: si murió mientras esperaba en la cola, se elige otro del mismo equipo.
                    var original = req.objetivos.Count > 0 ? req.objetivos[0] : null;
                    if (original != null && original.IsAlive) return new List<BattleUnit> { original };
                    Team equipo = req.accion.objetivo == TargetMode.AliadoUnico ? actor.Team : rival;
                    var vivos = Vivos(equipo);
                    if (vivos.Count == 0) return vivos;
                    return new List<BattleUnit> { vivos[Random.Range(0, vivos.Count)] };
                }
            }
        }

        void AplicarEfectos(BattleUnit actor, ActionData accion, BattleUnit t, bool esOverdrive)
        {
            // Durante un Overdrive no se recarga la barra del que lo usa.
            BattleUnit cargador = esOverdrive ? null : actor;
            bool sonoGolpe = false;

            foreach (var e in accion.efectos)
            {
                if (!t.IsAlive) break;
                switch (e.tipo)
                {
                    case EffectType.DanoFisico:
                    {
                        int d = Formulas.DanoFisico(actor.GetStat(StatType.Fuerza), t.GetStat(StatType.Defensa), e.potencia, e.valorFijo);
                        t.RecibirDano(d, cargador);
                        sonoGolpe = true;
                        break;
                    }
                    case EffectType.DanoEnergia:
                    {
                        int d = Formulas.DanoEnergia(actor.GetStat(StatType.Magia), t.GetStat(StatType.Defensa), e.potencia, e.valorFijo);
                        t.RecibirDano(d, cargador);
                        sonoGolpe = true;
                        break;
                    }
                    case EffectType.DanoFijo:
                    {
                        int d = Mathf.Max(1, Mathf.RoundToInt(e.valorFijo * Random.Range(0.95f, 1.05f)));
                        t.RecibirDano(d, cargador);
                        sonoGolpe = true;
                        break;
                    }
                    case EffectType.Curar:
                        t.Curar(Formulas.Curacion(actor.GetStat(StatType.Magia), e.potencia, e.valorFijo));
                        Sfx.Play(accion.sfxImpacto, SfxId.Curar, 0.8f);
                        break;
                    case EffectType.RestaurarEnergia:
                        t.RestaurarEnergia(Mathf.Max(1, Mathf.RoundToInt(e.valorFijo + e.potencia * actor.GetStat(StatType.Magia))));
                        Sfx.Play(accion.sfxImpacto, SfxId.Curar, 0.8f);
                        break;
                    case EffectType.ModificarStat:
                        t.AplicarBuff(e.modificador, e.duracion);
                        break;
                    case EffectType.AplicarEstado:
                        if (e.estado != null && Random.value <= e.probabilidad) t.AplicarEstado(e.estado);
                        break;
                    case EffectType.LimpiarEstados:
                        t.LimpiarEstados();
                        break;
                    case EffectType.ModificarATB:
                        t.ModificarATB(e.potencia);
                        break;
                    case EffectType.Provocar:
                        t.Provocar(e.duracion);
                        break;
                }
            }

            if (sonoGolpe) Sfx.Play(accion.sfxImpacto, SfxId.Golpe, 0.9f);
        }

        // ---------------------------------------------------------------------
        // Fin del combate
        // ---------------------------------------------------------------------
        void AlMorir(BattleUnit u)
        {
            ListosJugador.Remove(u);
            RevisarFin();
        }

        void RevisarFin()
        {
            if (Fase != BattlePhase.EnCurso) return;
            if (Vivos(Team.Enemigo).Count == 0) StartCoroutine(Victoria());
            else if (Vivos(Team.Jugador).Count == 0) StartCoroutine(Derrota());
        }

        IEnumerator Victoria()
        {
            Fase = BattlePhase.Victoria;
            cola.Clear();
            ListosJugador.Clear();
            yield return new WaitForSeconds(0.8f);
            Sfx.PararMusica();
            Sfx.Play(SfxId.Victoria);
            foreach (var u in Jugadores) u.View.Victoria();
            UI.Banner(GameSession.EsUltimaPelea ? "¡CAMPEÓN!" : "¡VICTORIA!", 1.5f, new Color(1f, 0.9f, 0.4f));
            yield return new WaitForSeconds(2f);
            UI.MostrarFin(true, GameSession.EsUltimaPelea);
        }

        IEnumerator Derrota()
        {
            Fase = BattlePhase.Derrota;
            cola.Clear();
            ListosJugador.Clear();
            yield return new WaitForSeconds(0.8f);
            Sfx.PararMusica();
            Sfx.Play(SfxId.Derrota);
            UI.Banner("DERROTA", 1.5f, new Color(1f, 0.4f, 0.4f));
            yield return new WaitForSeconds(2f);
            UI.MostrarFin(false, false);
        }

        // Llamados desde los botones del panel de fin
        public void Continuar()
        {
            if (GameSession.AvanzarPelea()) RecargarEscena();
            else VolverAlInicio();
        }

        public void Reintentar()
        {
            GameSession.RestaurarInicioPelea();
            RecargarEscena();
        }

        public void VolverAlInicio()
        {
            if (!string.IsNullOrEmpty(escenaSeleccion) && Application.CanStreamedLevelBeLoaded(escenaSeleccion))
            {
                SceneManager.LoadScene(escenaSeleccion);
                return;
            }
            // Sin escena de selección: reiniciar el torneo con el mismo equipo.
            GameSession.NuevoTorneo(torneo, GameSession.Equipo);
            RecargarEscena();
        }

        static void RecargarEscena()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ---------------------------------------------------------------------
        // Consultas
        // ---------------------------------------------------------------------
        public List<BattleUnit> DeEquipo(Team t) => t == Team.Jugador ? Jugadores : Enemigos;

        public List<BattleUnit> Vivos(Team t)
        {
            var lista = new List<BattleUnit>();
            foreach (var u in DeEquipo(t)) if (u.IsAlive) lista.Add(u);
            return lista;
        }
    }
}
