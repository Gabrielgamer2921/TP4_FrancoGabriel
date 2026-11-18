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
        [Tooltip("Escena del menú principal (debe estar en Build Settings).")]
        [SerializeField] string escenaMenu = "MenuPrincipal";

        public readonly List<BattleUnit> Jugadores = new List<BattleUnit>();
        public readonly List<BattleUnit> Enemigos = new List<BattleUnit>();
        /// <summary>Robots del jugador con la barra llena esperando una orden (en orden de llegada).</summary>
        public readonly List<BattleUnit> ListosJugador = new List<BattleUnit>();

        readonly Queue<ActionRequest> cola = new Queue<ActionRequest>();
        bool ejecutando;

        public BattlePhase Fase { get; private set; } = BattlePhase.Intro;
        public bool PausaCinematica { get; private set; }

        /// <summary>
        /// true = ATB desactivada: combate por turnos clásico (estilo The 7th Saga).
        /// Primero actúa todo tu equipo y después todos los rivales, sin barras ni apuro.
        /// Se fija al empezar cada pelea.
        /// </summary>
        public bool ModoTurnos { get; private set; }

        BattleUnit turnoActual;
        BattleUnit actorEjecutando;

        /// <summary>
        /// El robot al que le toca ahora: por turnos, el que está jugando su turno;
        /// en ATB, el que está ejecutando una acción o, si no, el primero esperando una orden.
        /// </summary>
        public BattleUnit UnidadEnTurno
        {
            get
            {
                if (ModoTurnos) return turnoActual != null && turnoActual.IsAlive ? turnoActual : null;
                if (actorEjecutando != null) return actorEjecutando;
                return ListosJugador.Count > 0 ? ListosJugador[0] : null;
            }
        }

        /// <summary>Modo por turnos: de quién es la fase actual.</summary>
        public Team FaseDe { get; private set; } = Team.Jugador;

        /// <summary>Modo por turnos: cuántos "segundos" de estados/buffs pasan por cada turno.</summary>
        const float SegundosPorTurno = 3f;

        /// <summary>
        /// ¿Está detenido el tiempo de la ATB? Depende del modo elegido:
        /// Activo = solo en cinemáticas; Espera = además en submenús y durante las animaciones;
        /// Clásico = además siempre que haya un robot tuyo esperando una orden.
        /// </summary>
        public bool TiempoDetenido
        {
            get
            {
                if (PausaCinematica) return true;
                switch (GameSession.ModoATB)
                {
                    case ModoATB.Espera:
                        return ejecutando || (UI != null && UI.EnSubmenu);
                    case ModoATB.Clasico:
                        return ejecutando || ListosJugador.Count > 0 || (UI != null && UI.EsperandoOrden);
                    default:
                        return false;
                }
            }
        }
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
#if UNITY_EDITOR
            // Red de seguridad en el editor: si falta la referencia, buscar los assets en el proyecto.
            if (torneo == null) torneo = BuscarAsset<TournamentData>();
            if (visuales == null) visuales = BuscarAsset<BattleVisuals>();
#endif
            if (torneo == null)
            {
                Debug.LogError("[TP4] BattleManager no tiene TournamentData asignado. Usá el menú TP4 > Crear contenido y escena.");
                enabled = false;
                return;
            }
            if (camara == null && Camera.main != null) camara = Camera.main.GetComponent<BattleCamera>();

            GameSession.AsegurarIniciada(torneo);
#if UNITY_EDITOR
            // Menú TP4 > Probar pelea N: arranca directo en esa pelea.
            int prueba = UnityEditor.EditorPrefs.GetInt("tp4.peleaPrueba", -1);
            if (prueba >= 0)
            {
                UnityEditor.EditorPrefs.DeleteKey("tp4.peleaPrueba");
                GameSession.SaltarAPelea(prueba);
            }
#endif
            Encuentro = GameSession.PeleaData;
            if (Encuentro == null)
            {
                Debug.LogError("[TP4] El torneo no tiene peleas configuradas.");
                enabled = false;
                return;
            }

            Time.timeScale = 1f;
            ModoTurnos = !Ajustes.ATBActivo;
            Sfx.Crear(visuales);
            CrearUnidades();
            if (ModoTurnos)
                foreach (var u in Todas()) { u.ATB = 0f; u.State = UnitState.Cargando; }
            UI = BattleUI.Crear(this, visuales);
            PauseMenu.Crear(this, visuales);
            RefrescarTextos();

            var musica = Encuentro.musica != null ? Encuentro.musica
                : visuales != null ? (Encuentro.esJefe ? visuales.musicaJefe : visuales.musicaCombate) : null;
            Sfx.Musica(musica);

            StartCoroutine(Intro());
        }

        /// <summary>Textos que dependen del idioma (se llama también al cambiar de idioma).</summary>
        public void RefrescarTextos()
        {
            if (UI == null || Encuentro == null) return;
            UI.SetRonda(GameSession.PeleaActual + 1, torneo.peleas.Count, Encuentro.NombreRival,
                Loc.NombreDificultad(GameSession.Dificultad));
        }

        IEnumerator Intro()
        {
            Fase = BattlePhase.Intro;
            yield return new WaitForSeconds(0.3f);
            string titulo = Encuentro.esJefe
                ? $"{Loc.T("msg.peleaFinal")}\n<size=40>{Encuentro.NombreRival}</size>"
                : $"{Encuentro.Nombre}\n<size=40>vs {Encuentro.NombreRival}</size>";
            UI.Banner(titulo, 1.6f, Encuentro.esJefe ? new Color(1f, 0.45f, 0.35f) : Color.white);
            yield return new WaitForSeconds(2.1f);
            Fase = BattlePhase.EnCurso;
            if (ModoTurnos) StartCoroutine(BucleTurnos());
        }

        // ---------------------------------------------------------------------
        // Modo por turnos (ATB desactivada)
        // ---------------------------------------------------------------------
        IEnumerator BucleTurnos()
        {
            while (Fase == BattlePhase.EnCurso)
            {
                yield return FaseDeEquipo(Team.Jugador);
                if (Fase != BattlePhase.EnCurso) yield break;
                yield return FaseDeEquipo(Team.Enemigo);
            }
        }

        IEnumerator FaseDeEquipo(Team equipo)
        {
            FaseDe = equipo;
            var orden = Vivos(equipo);
            // Dentro de la fase, el más ágil actúa primero.
            orden.Sort((a, b) => b.GetStat(StatType.Agilidad).CompareTo(a.GetStat(StatType.Agilidad)));
            foreach (var u in orden) { u.ATB = 1f; u.NotificarCambio(); }

            UI.Banner(Loc.T(equipo == Team.Jugador ? "turno.jugador" : "turno.enemigo"), 0.7f,
                equipo == Team.Jugador ? new Color(0.5f, 0.9f, 1f) : new Color(1f, 0.55f, 0.5f));
            yield return new WaitForSeconds(1.1f);

            foreach (var u in orden)
            {
                if (Fase != BattlePhase.EnCurso) yield break;
                if (!u.IsAlive) continue;
                yield return TurnoDe(u);
                u.ATB = 0f;
                u.NotificarCambio();
                yield return new WaitForSeconds(equipo == Team.Enemigo ? 0.35f : 0.15f);
            }
            foreach (var u in DeEquipo(equipo)) u.ATB = 0f;
        }

        IEnumerator TurnoDe(BattleUnit u)
        {
            turnoActual = u;
            // Los estados avanzan al empezar el turno (la quemadura puede rematarlo).
            u.AvanzarTurno(SegundosPorTurno);
            if (!u.IsAlive || Fase != BattlePhase.EnCurso) yield break;

            // La parálisis le hace perder el turno.
            if (u.ATBCongelada)
            {
                UI.Popup(u, Loc.T(u.TieneEstadoLiberable ? "msg.atrapado" : "msg.pierdeTurno"), new Color(1f, 0.95f, 0.3f), 30);
                yield return new WaitForSeconds(0.7f);
                yield break;
            }

            // El jefe puede tener varias acciones por turno; Overclock suma una extra.
            int restantes = u.AccionesPorTurno;
            while (restantes > 0 && u.IsAlive && Fase == BattlePhase.EnCurso)
            {
                restantes--;
                // Si quedó atrapado/paralizado entre una acción y otra, pierde el resto del turno.
                if (u.ATBCongelada) yield break;
                if (u.EsJugador)
                {
                    u.State = UnitState.Listo;
                    if (!ListosJugador.Contains(u)) ListosJugador.Add(u);
                    // Esperar a que el jugador elija (la UI encola la acción).
                    while (u.IsAlive && u.State == UnitState.Listo && Fase == BattlePhase.EnCurso) yield return null;
                }
                else
                {
                    var req = EnemyAI.Decidir(u, this);
                    if (req == null) yield break;
                    Encolar(req);
                }

                // Esperar a que se ejecute.
                while (Fase == BattlePhase.EnCurso && (cola.Count > 0 || ejecutando)) yield return null;
                if (u.State != UnitState.Muerto) u.State = UnitState.Cargando;

                if (u.AccionExtra && u.IsAlive)
                {
                    u.AccionExtra = false;
                    restantes++;
                }
                if (restantes > 0) yield return new WaitForSeconds(0.3f);
            }
        }

#if UNITY_EDITOR
        static T BuscarAsset<T>() where T : Object
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids.Length == 0) return null;
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
        }
#endif

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
            // La dificultad escala las stats de los rivales.
            unit.Init(data, team, slot, view, team == Team.Enemigo ? GameSession.MultiplicadorRivales : 1f);
            view.Construir(unit, visuales);

            unit.Died += AlMorir;
            unit.EstadoAplicado += (u, estado) =>
            {
                // Feedback claro de la Red eléctrica: cartel + cómo salir.
                if (estado.liberable && u.EsJugador)
                {
                    UI.Banner(Loc.T("msg.redBanner", u.Nombre), 2f, new Color(0.5f, 0.95f, 1f));
                    if (camara != null) camara.Shake(0.15f, 0.3f);
                }
            };
            unit.PhaseTriggered += (u, fase) =>
            {
                UI.Banner(fase.Mensaje, 1.4f, new Color(1f, 0.4f, 0.3f));
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

            if (!ModoTurnos && !TiempoDetenido)
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
            actorEjecutando = actor;
            if (hab != null)
            {
                if (hab.esOverdrive) actor.VaciarOverdrive();
                else if (hab.costoEnergia > 0) actor.GastarEnergia(hab.costoEnergia);
            }

            Color colorNombre = actor.EsJugador ? Color.white : new Color(1f, 0.6f, 0.55f);
            if (hab != null && hab.esOverdrive) colorNombre = new Color(1f, 0.85f, 0.2f);
            UI.AnunciarAccion(req.Item != null ? $"{actor.Nombre}: {accion.Nombre}" : accion.Nombre, colorNombre);

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

            // Memoria para la IA (para que no repita siempre lo mismo).
            actor.RachaSoporte = accion.HaceDano ? 0 : actor.RachaSoporte + 1;
            actor.UltimaAccion = accion;
            actor.RegistrarUso(accion);

            actor.TerminarAccion(accion.recuperacion);
            actorEjecutando = null;
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
                    case EffectType.Liberar:
                        t.Liberar();
                        Sfx.Play(accion.sfxImpacto, SfxId.Curar, 0.8f);
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
            UI.Banner(GameSession.EsUltimaPelea ? Loc.T("msg.campeon") : Loc.T("msg.victoria"), 1.5f, new Color(1f, 0.9f, 0.4f));
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
            UI.Banner(Loc.T("msg.derrota"), 1.5f, new Color(1f, 0.4f, 0.4f));
            yield return new WaitForSeconds(2f);
            UI.MostrarFin(false, false);
        }

        // Llamados desde los botones del panel de fin / pausa
        public void Continuar()
        {
            if (GameSession.AvanzarPelea()) RecargarEscena();
            else VolverAlMenu();
        }

        /// <summary>Sin guardado: volver al menú borra todo el progreso del torneo.</summary>
        public void VolverAlMenu()
        {
            GameSession.Terminar();
            Time.timeScale = 1f;
            if (!string.IsNullOrEmpty(escenaMenu) && Application.CanStreamedLevelBeLoaded(escenaMenu))
            {
                SceneManager.LoadScene(escenaMenu);
                return;
            }
            Debug.LogWarning($"[TP4] La escena '{escenaMenu}' no está en Build Settings. Se reinicia el combate.");
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

        public IEnumerable<BattleUnit> Todas()
        {
            foreach (var u in Jugadores) yield return u;
            foreach (var u in Enemigos) yield return u;
        }

        public List<BattleUnit> Vivos(Team t)
        {
            var lista = new List<BattleUnit>();
            foreach (var u in DeEquipo(t)) if (u.IsAlive) lista.Add(u);
            return lista;
        }
    }
}
