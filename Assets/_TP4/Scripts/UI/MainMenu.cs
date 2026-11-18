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
        Text titulo, subtitulo, encabezado, ayuda, controles;
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
            var go = new GameObject("MenuUI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var raiz = (RectTransform)go.transform;

            // Degradé oscuro a la izquierda para que se lea el menú
            var sombra = UIFactory.Rect("Sombra", raiz);
            sombra.anchorMin = new Vector2(0, 0);
            sombra.anchorMax = new Vector2(0.55f, 1);
            sombra.offsetMin = sombra.offsetMax = Vector2.zero;
            var sombraImg = sombra.gameObject.AddComponent<Image>();
            sombraImg.color = new Color(0f, 0f, 0.06f, 0.55f);
            sombraImg.raycastTarget = false;

            titulo = UIFactory.Texto(raiz, "", 96, TextAnchor.UpperLeft, new Color(1f, 0.92f, 0.5f), fuente);
            titulo.fontStyle = FontStyle.Bold;
            UIFactory.Ubicar(titulo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -80), new Vector2(1100, 120));

            subtitulo = UIFactory.Texto(raiz, "", 34, TextAnchor.UpperLeft, new Color(0.6f, 0.9f, 1f), fuente);
            UIFactory.Ubicar(subtitulo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(96, -200), new Vector2(1000, 50));

            encabezado = UIFactory.Texto(raiz, "", 34, TextAnchor.LowerLeft, Color.white, fuente);
            UIFactory.Ubicar(encabezado.rectTransform, new Vector2(0, 1), new Vector2(0, 0), new Vector2(96, -360), new Vector2(900, 50));

            ventana = (RectTransform)UIFactory.Ventana("Menu", raiz, visuales).transform;
            UIFactory.Ubicar(ventana, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -370), new Vector2(560, 200));
            lista = MenuLista.Crear(ventana, fuente);
            lista.alMostrarAyuda = t => ayuda.text = t ?? "";

            ayuda = UIFactory.Texto(raiz, "", 28, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 1f), fuente);
            ayuda.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.Ubicar(ayuda.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(96, 110), new Vector2(900, 80));

            controles = UIFactory.Texto(raiz, "", 20, TextAnchor.LowerCenter, new Color(0.7f, 0.75f, 0.9f), fuente);
            UIFactory.Ubicar(controles.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(1800, 30));

            RefrescarTextosFijos();
        }

        void RefrescarTextosFijos()
        {
            titulo.text = Loc.T("titulo");
            subtitulo.text = Loc.T("subtitulo");
            controles.text = Loc.T("menu.controles");
        }

        void AlCambiarAjustes()
        {
            if (Ajustes.Idioma == idiomaMostrado) return;
            RefrescarTextosFijos();
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
