using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TP4
{
    /// <summary>
    /// Menú principal: Nueva partida (→ dificultad), Opciones (ATB, volumen, idioma) y Salir.
    /// No hay guardado: cada "Nueva partida" arranca el torneo desde cero.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] TournamentData torneo;
        [SerializeField] BattleVisuals visuales;
        [Tooltip("Lugares donde se muestran los robots del plantel de fondo.")]
        [SerializeField] Transform[] puntosRobots = new Transform[0];
        [SerializeField] string escenaSeleccion = "SeleccionEquipo";
        [SerializeField] string escenaCombate = "Combate";

        enum Pantalla { Principal, Dificultad, Opciones }

        Font fuente;
        Text encabezado, ayuda;
        RectTransform ventana;
        MenuLista lista;
        Pantalla pantalla;
        Idioma idiomaMostrado;

        void Start()
        {
#if UNITY_EDITOR
            if (torneo == null)
            {
                var g = UnityEditor.AssetDatabase.FindAssets("t:TournamentData");
                if (g.Length > 0) torneo = UnityEditor.AssetDatabase.LoadAssetAtPath<TournamentData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g[0]));
            }
            if (visuales == null)
            {
                var g = UnityEditor.AssetDatabase.FindAssets("t:BattleVisuals");
                if (g.Length > 0) visuales = UnityEditor.AssetDatabase.LoadAssetAtPath<BattleVisuals>(UnityEditor.AssetDatabase.GUIDToAssetPath(g[0]));
            }
#endif
            Time.timeScale = 1f;
            GameSession.Terminar();
            Sfx.Crear(visuales);
            if (visuales != null) Sfx.Musica(visuales.musicaMenu);
            BattleUI.AsegurarEventSystem();
            fuente = visuales != null && visuales.fuente != null ? visuales.fuente : UIFactory.FuentePorDefecto;

            MostrarRobots();
            ConstruirUI();
            Ajustes.Cambio += AlCambiarAjustes;
            MostrarPrincipal(0);
        }

        void OnDestroy()
        {
            Ajustes.Cambio -= AlCambiarAjustes;
        }

        // ---------------------------------------------------------------------
        // Fondo: los robots del plantel
        // ---------------------------------------------------------------------
        void MostrarRobots()
        {
            if (torneo == null) return;
            for (int i = 0; i < puntosRobots.Length && i < torneo.plantel.Count; i++)
            {
                if (puntosRobots[i] == null || torneo.plantel[i] == null) continue;
                UnitView.CrearModelo(torneo.plantel[i], puntosRobots[i], visuales, out var anim);
                var bob = puntosRobots[i].gameObject.AddComponent<Flotar>();
                bob.fase = i * 1.3f;
            }
        }

        // ---------------------------------------------------------------------
        // UI
        // ---------------------------------------------------------------------
        void ConstruirUI()
        {
            // Si existe el prefab editable (TP4 > Generar UI editable) se usa ese; si no, se arma por código.
            GameObject go = visuales != null && visuales.prefabUIMenu != null ? Instantiate(visuales.prefabUIMenu) : new GameObject("MenuUI");
            go.name = "MenuUI";
            var raiz = UIFactory.AsegurarCanvas(go, 0);
            Armar(raiz, new UIBuilder(visuales), this);
        }

        /// <summary>Editor: arma la UI del menú para guardarla como prefab editable.</summary>
        public static GameObject GenerarPlantilla(BattleVisuals vis, GameObject existente = null)
        {
            var go = existente != null ? existente : new GameObject("UI_Menu");
            var raiz = UIFactory.AsegurarCanvas(go, 0);
            Armar(raiz, new UIBuilder(vis), null);
            return go;
        }

        /// <summary>Busca o crea cada elemento (los que existen en el prefab se respetan como estén).</summary>
        static void Armar(RectTransform raiz, UIBuilder b, MainMenu m)
        {
            // Degradé oscuro a la izquierda para que se lea el menú
            b.Rect(raiz, "Sombra", rt =>
            {
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0.55f, 1);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = rt.gameObject.AddComponent<Image>();
                img.color = new Color(0f, 0f, 0.06f, 0.55f);
                img.raycastTarget = false;
            });

            var titulo = b.TextoTraducible(raiz, "Titulo", "titulo", 96, TextAnchor.UpperLeft, new Color(1f, 0.92f, 0.5f),
                rt =>
                {
                    UIFactory.Ubicar(rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -80), new Vector2(1100, 120));
                    rt.GetComponent<Text>().fontStyle = FontStyle.Bold;
                });
            b.TextoTraducible(raiz, "Subtitulo", "subtitulo", 34, TextAnchor.UpperLeft, new Color(0.6f, 0.9f, 1f),
                rt => UIFactory.Ubicar(rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(96, -200), new Vector2(1000, 50)));

            var encabezado = b.Texto(raiz, "Encabezado", "Elegí la dificultad", 34, TextAnchor.LowerLeft, Color.white,
                rt => UIFactory.Ubicar(rt, new Vector2(0, 1), new Vector2(0, 0), new Vector2(96, -360), new Vector2(900, 50)));

            var ventana = b.Ventana(raiz, "Menu", rt =>
                UIFactory.Ubicar(rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -370), new Vector2(560, 200)));

            var ayuda = b.Texto(raiz, "Ayuda", "Descripción de la opción seleccionada.", 28, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 1f),
                rt =>
                {
                    UIFactory.Ubicar(rt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(96, 110), new Vector2(900, 80));
                    rt.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;
                });

            b.TextoTraducible(raiz, "Controles", "menu.controles", 20, TextAnchor.LowerCenter, new Color(0.7f, 0.75f, 0.9f),
                rt => UIFactory.Ubicar(rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(1800, 30)));

            if (m == null) return;
            m.encabezado = encabezado;
            m.ayuda = ayuda;
            m.ventana = ventana;
            m.lista = MenuLista.Crear(ventana, m.fuente);
            m.lista.alMostrarAyuda = t => m.ayuda.text = t ?? "";
            m.encabezado.text = "";
            m.ayuda.text = "";
        }

        void AlCambiarAjustes()
        {
            if (Ajustes.Idioma == idiomaMostrado) return;
            if (pantalla == Pantalla.Opciones) MostrarOpciones(lista.IndiceActual);
        }

        // ---------------------------------------------------------------------
        // Pantallas
        // ---------------------------------------------------------------------
        void MostrarPrincipal(int indice)
        {
            pantalla = Pantalla.Principal;
            idiomaMostrado = Ajustes.Idioma;
            encabezado.text = "";
            lista.Mostrar(new List<OpcionLista>
            {
                new OpcionLista { texto = Loc.T("menu.nueva"), ayuda = Loc.T("menu.nueva.ayuda"), alConfirmar = () => MostrarDificultad(1) },
                new OpcionLista { texto = Loc.T("menu.opciones"), ayuda = Loc.T("menu.opciones.ayuda"), alConfirmar = () => MostrarOpciones(0) },
                new OpcionLista { texto = Loc.T("menu.salir"), ayuda = Loc.T("menu.salir.ayuda"), alConfirmar = Salir },
            }, null, indice);
        }

        void MostrarDificultad(int indice)
        {
            pantalla = Pantalla.Dificultad;
            idiomaMostrado = Ajustes.Idioma;
            encabezado.text = Loc.T("dif.titulo");
            var items = new List<OpcionLista>();
            foreach (Dificultad d in System.Enum.GetValues(typeof(Dificultad)))
            {
                var dif = d;
                items.Add(new OpcionLista
                {
                    texto = Loc.NombreDificultad(dif),
                    ayuda = Loc.T("dif." + dif + ".ayuda"),
                    color = dif == Dificultad.Mecha ? new Color(1f, 0.55f, 0.45f) : (Color?)null,
                    alConfirmar = () => NuevaPartida(dif)
                });
            }
            items.Add(new OpcionLista { texto = Loc.T("menu.volver"), alConfirmar = () => MostrarPrincipal(0) });
            lista.Mostrar(items, () => { Sfx.Play(SfxId.Cancelar, 0.6f); MostrarPrincipal(0); }, indice);
        }

        void MostrarOpciones(int indice)
        {
            pantalla = Pantalla.Opciones;
            idiomaMostrado = Ajustes.Idioma;
            encabezado.text = Loc.T("op.titulo");
            lista.Mostrar(PanelOpciones.Items(() => MostrarPrincipal(1)),
                () => { Sfx.Play(SfxId.Cancelar, 0.6f); MostrarPrincipal(1); }, indice, 700f);
        }

        // ---------------------------------------------------------------------
        // Acciones
        // ---------------------------------------------------------------------
        void NuevaPartida(Dificultad dificultad)
        {
            if (torneo == null)
            {
                Debug.LogError("[TP4] MainMenu no tiene TournamentData asignado.");
                return;
            }
            GameSession.NuevoTorneo(torneo, GameSession.EquipoPorDefecto(torneo), dificultad);
            lista.Activa = false;

            // Cuando exista la pantalla de selección de equipo, se pasa primero por ahí.
            string destino = !string.IsNullOrEmpty(escenaSeleccion) && Application.CanStreamedLevelBeLoaded(escenaSeleccion)
                ? escenaSeleccion
                : escenaCombate;
            SceneManager.LoadScene(destino);
        }

        void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>Movimiento suave de "respiración" para los robots del menú.</summary>
    public class Flotar : MonoBehaviour
    {
        public float fase;
        public float amplitud = 0.06f;
        public float giro = 6f;
        Vector3 pos0;
        Quaternion rot0;

        void Start()
        {
            pos0 = transform.localPosition;
            rot0 = transform.localRotation;
        }

        void Update()
        {
            float t = Time.time + fase;
            transform.localPosition = pos0 + Vector3.up * Mathf.Sin(t * 1.6f) * amplitud;
            transform.localRotation = rot0 * Quaternion.Euler(0, Mathf.Sin(t * 0.5f) * giro, 0);
        }
    }
}
