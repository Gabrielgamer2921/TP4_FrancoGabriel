using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TP4
{
    /// <summary>
    /// UI de combate estilo FF7 construida por código.
    /// Toda la navegación usa el EventSystem (InputSystemUIInputModule), así que funciona
    /// igual con teclado (flechas/WASD + Enter/Espacio + Esc) y con joystick (stick/cruceta + A + B).
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        // ---------- Config de layout (canvas de referencia 1920x1080) ----------
        const float AltoPanel = 290f;
        const float Margen = 16f;
        const float AltoFila = 72f;
        const float AltoOpcion = 52f;

        static readonly Color ColorTexto = Color.white;
        static readonly Color ColorDeshabilitado = new Color(0.55f, 0.58f, 0.65f);
        static readonly Color ColorActivo = new Color(1f, 0.92f, 0.45f);
        static readonly Color ColorVida = new Color(0.35f, 0.85f, 1f);
        static readonly Color ColorEnergia = new Color(0.65f, 0.5f, 1f);
        static readonly Color ColorOD = new Color(0.95f, 0.35f, 0.75f);
        static readonly Color ColorODLleno = new Color(1f, 0.85f, 0.2f);
        static readonly Color ColorATB = new Color(0.65f, 0.8f, 1f);
        static readonly Color ColorATBLleno = new Color(1f, 0.85f, 0.25f);
        static readonly Color ColorATBCongelada = new Color(0.6f, 0.4f, 0.95f);

        BattleManager bm;
        BattleVisuals vis;
        Font fuente;
        Camera cam;

        RectTransform raiz;
        RectTransform principal;
        CanvasGroup grupoPrincipal;
        RectTransform capaMundo;
        RectTransform capaPopups;

        // ---------- Panel inferior ----------
        class Fila
        {
            public BattleUnit u;
            public Text nombre, estados, vida, energia;
            public Image resaltado;
            public UIFactory.Barra barraVida, barraEnergia, barraOD, barraATB;
        }
        readonly List<Fila> filas = new List<Fila>();

        // ---------- Etiquetas de enemigos ----------
        class Etiqueta
        {
            public BattleUnit u;
            public RectTransform rt;
            public CanvasGroup grupo;
            public Text nombre, estados;
            public UIFactory.Barra vida;
        }
        readonly List<Etiqueta> etiquetas = new List<Etiqueta>();

        // ---------- Menús ----------
        RectTransform ventanaComandos, ventanaLista, ventanaObjetivos;
        RectTransform barraAyuda;
        Text textoAyuda;
        RectTransform barraAccion;
        Text textoAccion;
        float accionRestante;
        Text textoRonda;
        Text textoModo;
        RectTransform banner;
        Text textoBanner;
        CanvasGroup grupoBanner;
        Coroutine rutinaBanner;
        RectTransform panelFin;
        RectTransform letterArriba, letterAbajo;
        readonly List<Text> cursores = new List<Text>();
        Text cursorActivo;

        enum Modo { Nada, Comandos, Lista, Objetivo }
        Modo modo;
        BattleUnit activo;
        ActionData accionElegida;
        Action volverDesdeObjetivo;
        readonly List<GameObject> opcionesActuales = new List<GameObject>();
        readonly List<GameObject> opcionesFin = new List<GameObject>();
        readonly List<BattleUnit> objetivosResaltados = new List<BattleUnit>();
        readonly List<BattleUnit> candidatosObjetivo = new List<BattleUnit>();
        readonly Dictionary<BattleUnit, float> ultimoPopup = new Dictionary<BattleUnit, float>();
        readonly Dictionary<BattleUnit, int> pilaPopup = new Dictionary<BattleUnit, int>();
        readonly List<(Text, string)> textosTraducibles = new List<(Text, string)>();

        class Opcion
        {
            public string texto;
            public string derecha;
            public bool habilitada = true;
            public Color? color;
            public string ayuda;
            public Action alConfirmar;
            public Action alSeleccionar;
        }

        // =====================================================================
        // Construcción
        // =====================================================================
        public static BattleUI Crear(BattleManager bm, BattleVisuals vis)
        {
            AsegurarEventSystem();

            var go = new GameObject("BattleUI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var ui = go.AddComponent<BattleUI>();
            ui.bm = bm;
            ui.vis = vis;
            ui.fuente = vis != null && vis.fuente != null ? vis.fuente : UIFactory.FuentePorDefecto;
            ui.cam = Camera.main;
            ui.raiz = (RectTransform)go.transform;
            ui.Construir();
            return ui;
        }

        public static void AsegurarEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (es != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var modulo = go.AddComponent<InputSystemUIInputModule>();
            modulo.AssignDefaultActions();
        }

        Text T(Transform padre, string s, int tam, TextAnchor al, Color? c = null)
        {
            var t = UIFactory.Texto(padre, s, tam, al, c ?? ColorTexto, fuente);
            t.supportRichText = true;
            return t;
        }

        /// <summary>Texto con clave de traducción: se actualiza solo si cambia el idioma.</summary>
        Text TL(Transform padre, string clave, int tam, TextAnchor al, Color? c = null)
        {
            var t = T(padre, Loc.T(clave), tam, al, c);
            textosTraducibles.Add((t, clave));
            return t;
        }

        void RefrescarIdioma()
        {
            foreach (var (t, clave) in textosTraducibles)
                if (t != null) t.text = Loc.T(clave);
            if (bm != null) bm.RefrescarTextos();
        }

        void OnDestroy()
        {
            Ajustes.Cambio -= RefrescarIdioma;
        }

        static RectTransform ArribaIzq(RectTransform rt, float x, float y, float w, float h)
            => UIFactory.Ubicar(rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h));

        void Construir()
        {
            Ajustes.Cambio += RefrescarIdioma;
            principal = UIFactory.Estirar(UIFactory.Rect("Principal", raiz));
            grupoPrincipal = principal.gameObject.AddComponent<CanvasGroup>();
            capaMundo = UIFactory.Estirar(UIFactory.Rect("Mundo", principal));

            ConstruirPanelInferior();
            ConstruirEtiquetasEnemigos();
            ConstruirVentanas();

            // Cursor de la unidad activa
            cursorActivo = T(capaMundo, "▼", 44, TextAnchor.MiddleCenter, new Color(0.4f, 1f, 1f));
            UIFactory.Ubicar(cursorActivo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(60, 60));
            cursorActivo.enabled = false;

            // Capas que siguen visibles durante cinemáticas
            capaPopups = UIFactory.Estirar(UIFactory.Rect("Popups", raiz));

            letterArriba = UIFactory.Rect("LetterboxArriba", raiz);
            letterArriba.anchorMin = new Vector2(0, 1); letterArriba.anchorMax = new Vector2(1, 1);
            letterArriba.pivot = new Vector2(0.5f, 1); letterArriba.sizeDelta = new Vector2(0, 0);
            letterArriba.gameObject.AddComponent<Image>().color = Color.black;
            letterAbajo = UIFactory.Rect("LetterboxAbajo", raiz);
            letterAbajo.anchorMin = new Vector2(0, 0); letterAbajo.anchorMax = new Vector2(1, 0);
            letterAbajo.pivot = new Vector2(0.5f, 0); letterAbajo.sizeDelta = new Vector2(0, 0);
            letterAbajo.gameObject.AddComponent<Image>().color = Color.black;

            // Banner central
            banner = UIFactory.Rect("Banner", raiz);
            banner.anchorMin = new Vector2(0, 0.5f); banner.anchorMax = new Vector2(1, 0.5f);
            banner.pivot = new Vector2(0.5f, 0.5f); banner.anchoredPosition = new Vector2(0, 120); banner.sizeDelta = new Vector2(0, 170);
            var bannerImg = banner.gameObject.AddComponent<Image>();
            bannerImg.color = new Color(0, 0, 0.1f, 0.65f);
            bannerImg.raycastTarget = false;
            grupoBanner = banner.gameObject.AddComponent<CanvasGroup>();
            grupoBanner.alpha = 0f;
            grupoBanner.blocksRaycasts = false;
            textoBanner = T(banner, "", 64, TextAnchor.MiddleCenter);
            UIFactory.Estirar(textoBanner.rectTransform);
            textoBanner.fontStyle = FontStyle.Bold;

            // Panel de fin
            panelFin = (RectTransform)UIFactory.Ventana("PanelFin", raiz, vis).transform;
            UIFactory.Ubicar(panelFin, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(720, 360));
            panelFin.gameObject.SetActive(false);
        }

        void ConstruirPanelInferior()
        {
            var nombres = (RectTransform)UIFactory.Ventana("PanelNombres", principal, vis).transform;
            UIFactory.Ubicar(nombres, Vector2.zero, Vector2.zero, new Vector2(Margen, Margen), new Vector2(600, AltoPanel));
            var estado = (RectTransform)UIFactory.Ventana("PanelEstado", principal, vis).transform;
            UIFactory.Ubicar(estado, Vector2.zero, Vector2.zero, new Vector2(Margen * 2 + 600, Margen), new Vector2(1920 - Margen * 3 - 600, AltoPanel));

            Color cab = new Color(0.75f, 0.8f, 0.95f);
            ArribaIzq(TL(nombres, "ui.nombre", 22, TextAnchor.UpperLeft, cab).rectTransform, 24, 12, 300, 30);
            ArribaIzq(TL(estado, "ui.vida", 22, TextAnchor.UpperLeft, cab).rectTransform, 30, 12, 200, 30);
            ArribaIzq(TL(estado, "ui.energia", 22, TextAnchor.UpperLeft, cab).rectTransform, 380, 12, 200, 30);
            ArribaIzq(TL(estado, "ui.overdrive", 22, TextAnchor.UpperLeft, cab).rectTransform, 640, 12, 260, 30);
            ArribaIzq(TL(estado, bm.ModoTurnos ? "ui.turno" : "ui.atb", 22, TextAnchor.UpperLeft, cab).rectTransform, 950, 12, 260, 30);

            int i = 0;
            foreach (var u in bm.Jugadores)
            {
                float y = 50 + i * AltoFila;
                var f = new Fila { u = u };

                var res = ArribaIzq(UIFactory.Rect("Resaltado", nombres), 8, y - 4, 584, AltoFila - 6);
                f.resaltado = res.gameObject.AddComponent<Image>();
                f.resaltado.color = new Color(1f, 1f, 1f, 0.12f);
                f.resaltado.raycastTarget = false;
                f.resaltado.enabled = false;

                f.nombre = T(nombres, u.Nombre, 34, TextAnchor.MiddleLeft);
                ArribaIzq(f.nombre.rectTransform, 24, y, 300, AltoFila - 14);
                f.estados = T(nombres, "", 22, TextAnchor.MiddleRight);
                ArribaIzq(f.estados.rectTransform, 300, y, 280, AltoFila - 14);

                f.vida = T(estado, "", 32, TextAnchor.UpperRight);
                ArribaIzq(f.vida.rectTransform, 30, y, 300, 40);
                f.barraVida = UIFactory.CrearBarra(estado, ColorVida);
                ArribaIzq(f.barraVida.raiz, 30, y + 42, 300, 12);

                f.energia = T(estado, "", 32, TextAnchor.UpperRight);
                ArribaIzq(f.energia.rectTransform, 380, y, 200, 40);
                f.barraEnergia = UIFactory.CrearBarra(estado, ColorEnergia);
                ArribaIzq(f.barraEnergia.raiz, 380, y + 42, 200, 12);

                f.barraOD = UIFactory.CrearBarra(estado, ColorOD);
                ArribaIzq(f.barraOD.raiz, 640, y + 16, 260, 24);
                f.barraATB = UIFactory.CrearBarra(estado, ColorATB);
                ArribaIzq(f.barraATB.raiz, 950, y + 16, 260, 24);

                filas.Add(f);
                RegistrarUnidad(u);
                i++;
            }
        }

        void ConstruirEtiquetasEnemigos()
        {
            foreach (var u in bm.Enemigos)
            {
                bool jefe = u.Data.rol == RobotRole.Jefe;
                float ancho = jefe ? 420 : 230;
                var e = new Etiqueta { u = u };
                e.rt = UIFactory.Rect("Etiqueta_" + u.Nombre, capaMundo);
                UIFactory.Ubicar(e.rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(ancho, 70));
                e.grupo = e.rt.gameObject.AddComponent<CanvasGroup>();
                e.nombre = T(e.rt, u.Nombre, jefe ? 32 : 24, TextAnchor.LowerCenter);
                UIFactory.Ubicar(e.nombre.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(ancho, 34));
                e.vida = UIFactory.CrearBarra(e.rt, new Color(1f, 0.35f, 0.3f));
                UIFactory.Ubicar(e.vida.raiz, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(ancho - 20, jefe ? 14 : 10));
                e.estados = T(e.rt, "", 18, TextAnchor.UpperCenter);
                UIFactory.Ubicar(e.estados.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -12), new Vector2(ancho, 22));
                etiquetas.Add(e);
                RegistrarUnidad(u);
            }
        }

        void ConstruirVentanas()
        {
            float baseY = Margen + AltoPanel + 8;

            ventanaComandos = (RectTransform)UIFactory.Ventana("VentanaComandos", principal, vis).transform;
            UIFactory.Ubicar(ventanaComandos, Vector2.zero, Vector2.zero, new Vector2(Margen * 2 + 600, baseY), new Vector2(300, 200));
            ventanaComandos.gameObject.SetActive(false);

            ventanaLista = (RectTransform)UIFactory.Ventana("VentanaLista", principal, vis).transform;
            UIFactory.Ubicar(ventanaLista, Vector2.zero, Vector2.zero, new Vector2(Margen * 3 + 900, baseY), new Vector2(620, 200));
            ventanaLista.gameObject.SetActive(false);

            ventanaObjetivos = (RectTransform)UIFactory.Ventana("VentanaObjetivos", principal, vis).transform;
            UIFactory.Ubicar(ventanaObjetivos, Vector2.zero, Vector2.zero, new Vector2(Margen, baseY), new Vector2(600, 200));
            ventanaObjetivos.gameObject.SetActive(false);

            barraAyuda = (RectTransform)UIFactory.Ventana("Ayuda", principal, vis).transform;
            UIFactory.Ubicar(barraAyuda, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -Margen), new Vector2(1500, 64));
            textoAyuda = T(barraAyuda, "", 28, TextAnchor.MiddleCenter);
            UIFactory.Estirar(textoAyuda.rectTransform, 20, 20, 0, 0);
            barraAyuda.gameObject.SetActive(false);

            barraAccion = (RectTransform)UIFactory.Ventana("Accion", principal, vis).transform;
            UIFactory.Ubicar(barraAccion, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -Margen - 76), new Vector2(640, 60));
            textoAccion = T(barraAccion, "", 30, TextAnchor.MiddleCenter);
            UIFactory.Estirar(textoAccion.rectTransform);
            barraAccion.gameObject.SetActive(false);

            textoRonda = T(principal, "", 24, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 1f));
            UIFactory.Ubicar(textoRonda.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -20), new Vector2(500, 40));
            textoModo = T(principal, "", 22, TextAnchor.UpperRight, new Color(0.85f, 0.9f, 1f));
            UIFactory.Ubicar(textoModo.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -20), new Vector2(600, 70));
        }

        void RegistrarUnidad(BattleUnit u)
        {
            u.Damaged += (x, d) => Popup(x, d.ToString(), Color.white, 46);
            u.Healed += (x, d) => Popup(x, "+" + d, new Color(0.4f, 1f, 0.5f), 46);
            u.EnergyRestored += (x, d) => Popup(x, "+" + d + " EN", ColorEnergia, 38);
            u.Message += (x, txt, c) => Popup(x, txt, c, 30);
        }

        // =====================================================================
        // Actualización
        // =====================================================================
        void Update()
        {
            if (bm == null) return;
            if (cam == null) cam = Camera.main;
            ActualizarModoATB();
            if (PauseMenu.Abierta) return;

            ActualizarFilas();
            ActualizarEtiquetas();
            ActualizarCursores();

            if (accionRestante > 0f)
            {
                accionRestante -= Time.deltaTime;
                if (accionRestante <= 0f) barraAccion.gameObject.SetActive(false);
            }

            ManejarTurno();

            if (panelFin.gameObject.activeSelf) AsegurarSeleccion(opcionesFin);
        }

        void ActualizarFilas()
        {
            foreach (var f in filas)
            {
                var u = f.u;
                bool esActivo = u == activo;
                f.resaltado.enabled = esActivo;
                f.nombre.color = !u.IsAlive ? new Color(1f, 0.35f, 0.35f) : esActivo ? ColorActivo : ColorTexto;
                f.vida.text = $"{u.Vida}/{u.VidaMax}";
                f.vida.color = !u.IsAlive ? new Color(1f, 0.35f, 0.35f) : u.VidaPct < 0.25f ? new Color(1f, 0.85f, 0.3f) : ColorTexto;
                f.barraVida.Set(u.VidaPct);
                f.energia.text = $"{u.Energia}/{u.EnergiaMax}";
                f.barraEnergia.Set(u.EnergiaMax > 0 ? (float)u.Energia / u.EnergiaMax : 0f);

                f.barraOD.Set(u.Overdrive);
                f.barraOD.imagenRelleno.color = u.Overdrive >= 1f
                    ? Color.Lerp(ColorODLleno, Color.white, (Mathf.Sin(Time.time * 8f) + 1f) * 0.25f)
                    : ColorOD;

                f.barraATB.Set(Mathf.Max(0f, u.ATB));
                f.barraATB.imagenRelleno.color = u.ATBCongelada ? ColorATBCongelada
                    : u.ATB >= 1f ? Color.Lerp(ColorATBLleno, Color.white, (Mathf.Sin(Time.time * 6f) + 1f) * 0.2f)
                    : ColorATB;

                f.estados.text = TextoEstados(u);
            }
        }

        static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        string TextoEstados(BattleUnit u)
        {
            if (!u.IsAlive) return $"<color=#FF6060>{Loc.T("est.ko")}</color>";
            var sb = new StringBuilder();
            foreach (var s in u.Estados)
                sb.Append($"<color=#{Hex(s.data.color)}>{s.data.Abreviatura}</color> ");
            if (u.EstaProvocando) sb.Append($"<color=#FFA040>{Loc.T("est.prov")}</color> ");
            foreach (StatType st in Enum.GetValues(typeof(StatType)))
            {
                float pct = u.StatModPct(st);
                if (Mathf.Abs(pct) < 0.01f) continue;
                string abrev = Loc.Stat(st);
                sb.Append(pct > 0 ? $"<color=#70FF80>{abrev}▲</color> " : $"<color=#FF9050>{abrev}▼</color> ");
            }
            return sb.ToString().TrimEnd();
        }

        bool APantalla(Vector3 mundo, out Vector2 local)
        {
            local = Vector2.zero;
            if (cam == null) return false;
            Vector3 sp = cam.WorldToScreenPoint(mundo);
            if (sp.z < 0f) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, sp, null, out local);
            return true;
        }

        void ActualizarEtiquetas()
        {
            foreach (var e in etiquetas)
            {
                bool visible = APantalla(e.u.View.Arriba, out var local);
                e.rt.anchoredPosition = local;
                float objetivoAlpha = visible && e.u.IsAlive ? 1f : 0f;
                e.grupo.alpha = Mathf.MoveTowards(e.grupo.alpha, objetivoAlpha, Time.deltaTime * 2f);
                e.vida.Set(e.u.VidaPct);
                e.estados.text = TextoEstados(e.u);
            }
        }

        void ActualizarCursores()
        {
            float bob = Mathf.Sin(Time.time * 8f) * 8f;

            if (activo != null && activo.IsAlive && APantalla(activo.View.Arriba, out var pa))
            {
                cursorActivo.enabled = true;
                cursorActivo.rectTransform.anchoredPosition = pa + new Vector2(0, 10 + bob);
            }
            else cursorActivo.enabled = false;

            while (cursores.Count < objetivosResaltados.Count)
            {
                var c = T(capaMundo, "▼", 56, TextAnchor.MiddleCenter, ColorActivo);
                UIFactory.Ubicar(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(70, 70));
                cursores.Add(c);
            }
            for (int i = 0; i < cursores.Count; i++)
            {
                bool usar = i < objetivosResaltados.Count && objetivosResaltados[i].IsAlive;
                if (usar && APantalla(objetivosResaltados[i].View.Arriba, out var p))
                {
                    cursores[i].enabled = true;
                    // Si el objetivo es enemigo, el cursor va arriba de su etiqueta.
                    float extra = objetivosResaltados[i].Team == Team.Enemigo ? 70f : 10f;
                    cursores[i].rectTransform.anchoredPosition = p + new Vector2(0, extra + bob);
                }
                else cursores[i].enabled = false;
            }
        }

        // =====================================================================
        // Turnos y menús
        // =====================================================================
        void ManejarTurno()
        {
            bool puede = bm.Fase == BattlePhase.EnCurso && !bm.PausaCinematica;

            if (modo != Modo.Nada)
            {
                if (!puede || activo == null || !activo.IsAlive || activo.State != UnitState.Listo)
                {
                    CerrarMenus();
                }
                else if (modo == Modo.Objetivo && candidatosObjetivo.Exists(c => !c.IsAlive))
                {
                    // Un candidato murió mientras elegíamos: reconstruir la lista.
                    Elegir(accionElegida, volverDesdeObjetivo);
                }
                else
                {
                    AsegurarSeleccion(opcionesActuales);
                }
            }

            if (modo == Modo.Nada && puede && bm.ListosJugador.Count > 0)
                AbrirComandos(bm.ListosJugador[0]);
        }

        void AbrirComandos(BattleUnit u, int indice = 0)
        {
            activo = u;
            modo = Modo.Comandos;
            objetivosResaltados.Clear();
            ventanaLista.gameObject.SetActive(false);
            ventanaObjetivos.gameObject.SetActive(false);

            var ops = new List<Opcion>();
            var d = u.Data;
            ops.Add(new Opcion
            {
                texto = Loc.T("cmd.atacar"),
                habilitada = d.ataqueBasico != null,
                ayuda = d.ataqueBasico != null ? d.ataqueBasico.Descripcion : "",
                alConfirmar = () => Elegir(d.ataqueBasico, () => AbrirComandos(u, 0))
            });
            if (u.OverdriveListo)
            {
                ops.Add(new Opcion
                {
                    texto = Loc.T("cmd.overdrive"),
                    color = ColorODLleno,
                    ayuda = $"{d.overdrive.Nombre}: {d.overdrive.Descripcion}",
                    alConfirmar = () => Elegir(d.overdrive, () => AbrirComandos(u, 1))
                });
            }
            int idxModulos = ops.Count;
            ops.Add(new Opcion
            {
                texto = Loc.T("cmd.modulos"),
                habilitada = u.Habilidades.Count > 0,
                ayuda = Loc.T("cmd.modulos.ayuda"),
                alConfirmar = () => AbrirModulos(u, idxModulos)
            });
            int idxItems = ops.Count;
            ops.Add(new Opcion
            {
                texto = Loc.T("cmd.items"),
                habilitada = GameSession.Inventario.TieneAlgo,
                ayuda = Loc.T("cmd.items.ayuda"),
                alConfirmar = () => AbrirItems(u, idxItems)
            });
            if (bm.ListosJugador.Count > 1)
            {
                ops.Add(new Opcion
                {
                    texto = Loc.T("cmd.cambiar"),
                    ayuda = Loc.T("cmd.cambiar.ayuda"),
                    alConfirmar = () => { bm.RotarListos(); Sfx.Play(SfxId.Confirmar, 0.6f); CerrarMenus(); }
                });
            }

            MostrarOpciones(ventanaComandos, 300, ops, null, indice);
            Sfx.Play(SfxId.Listo, 0.6f);
        }

        void AbrirModulos(BattleUnit u, int indiceVolver)
        {
            modo = Modo.Lista;
            var ops = new List<Opcion>();
            foreach (var h in u.Habilidades)
            {
                var hab = h;
                ops.Add(new Opcion
                {
                    texto = hab.Nombre,
                    derecha = $"{hab.costoEnergia} EN",
                    habilitada = u.PuedeUsar(hab),
                    ayuda = hab.Descripcion + (hab.recuperacion > 1.05f ? $"  <color=#FFB060>{Loc.T("cmd.lento")}</color>" : hab.recuperacion < 0.95f ? $"  <color=#70FFB0>{Loc.T("cmd.rapido")}</color>" : ""),
                    alConfirmar = () => Elegir(hab, () => AbrirModulos(u, indiceVolver))
                });
            }
            MostrarOpciones(ventanaLista, 620, ops, () => { Sfx.Play(SfxId.Cancelar, 0.6f); AbrirComandos(u, indiceVolver); }, 0);
        }

        void AbrirItems(BattleUnit u, int indiceVolver)
        {
            modo = Modo.Lista;
            var ops = new List<Opcion>();
            foreach (var it in GameSession.Inventario.Items)
            {
                var item = it;
                int n = GameSession.Inventario.Cantidad(item);
                if (n <= 0) continue;
                ops.Add(new Opcion
                {
                    texto = item.Nombre,
                    derecha = "x" + n,
                    ayuda = item.Descripcion,
                    alConfirmar = () => Elegir(item, () => AbrirItems(u, indiceVolver))
                });
            }
            MostrarOpciones(ventanaLista, 620, ops, () => { Sfx.Play(SfxId.Cancelar, 0.6f); AbrirComandos(u, indiceVolver); }, 0);
        }

        void Elegir(ActionData accion, Action volver)
        {
            if (accion == null || activo == null) return;
            accionElegida = accion;
            volverDesdeObjetivo = volver;
            candidatosObjetivo.Clear();

            Team rival = activo.Team == Team.Jugador ? Team.Enemigo : Team.Jugador;
            var ops = new List<Opcion>();

            switch (accion.objetivo)
            {
                case TargetMode.Propio:
                    Confirmar(new List<BattleUnit> { activo });
                    return;

                case TargetMode.EnemigoUnico:
                {
                    var vivos = bm.Vivos(rival);
                    var prov = vivos.FindAll(x => x.EstaProvocando);
                    if (prov.Count > 0) vivos = prov;
                    foreach (var v in vivos) AgregarOpcionObjetivo(ops, v, v.EstaProvocando ? Loc.T("obj.provoca") : null);
                    break;
                }
                case TargetMode.AliadoUnico:
                    foreach (var v in bm.Vivos(activo.Team)) AgregarOpcionObjetivo(ops, v, $"{v.Vida}/{v.VidaMax}");
                    break;

                case TargetMode.TodosEnemigos:
                case TargetMode.TodosAliados:
                {
                    var todos = bm.Vivos(accion.objetivo == TargetMode.TodosEnemigos ? rival : activo.Team);
                    candidatosObjetivo.AddRange(todos);
                    ops.Add(new Opcion
                    {
                        texto = accion.objetivo == TargetMode.TodosEnemigos ? Loc.T("obj.enemigos") : Loc.T("obj.equipo"),
                        ayuda = accion.Descripcion,
                        alSeleccionar = () => { objetivosResaltados.Clear(); objetivosResaltados.AddRange(todos); },
                        alConfirmar = () => Confirmar(new List<BattleUnit>(todos))
                    });
                    break;
                }
            }

            if (ops.Count == 0) { volver?.Invoke(); return; }
            modo = Modo.Objetivo;
            MostrarOpciones(ventanaObjetivos, 600, ops, () =>
            {
                Sfx.Play(SfxId.Cancelar, 0.6f);
                objetivosResaltados.Clear();
                ventanaObjetivos.gameObject.SetActive(false);
                volverDesdeObjetivo?.Invoke();
            }, 0);
        }

        void AgregarOpcionObjetivo(List<Opcion> ops, BattleUnit v, string derecha)
        {
            candidatosObjetivo.Add(v);
            ops.Add(new Opcion
            {
                texto = v.Nombre,
                derecha = derecha,
                ayuda = accionElegida.Descripcion,
                alSeleccionar = () => { objetivosResaltados.Clear(); objetivosResaltados.Add(v); },
                alConfirmar = () => Confirmar(new List<BattleUnit> { v })
            });
        }

        void Confirmar(List<BattleUnit> objetivos)
        {
            if (activo == null || accionElegida == null) return;
            Sfx.Play(SfxId.Confirmar, 0.7f);
            bm.Encolar(new ActionRequest(activo, accionElegida, objetivos));
            CerrarMenus();
        }

        void CerrarMenus()
        {
            modo = Modo.Nada;
            activo = null;
            accionElegida = null;
            objetivosResaltados.Clear();
            candidatosObjetivo.Clear();
            ventanaComandos.gameObject.SetActive(false);
            ventanaLista.gameObject.SetActive(false);
            ventanaObjetivos.gameObject.SetActive(false);
            barraAyuda.gameObject.SetActive(false);
            opcionesActuales.Clear();
            if (EventSystem.current != null && !panelFin.gameObject.activeSelf)
                EventSystem.current.SetSelectedGameObject(null);
        }

        void MostrarAyuda(string texto)
        {
            bool hay = !string.IsNullOrEmpty(texto);
            barraAyuda.gameObject.SetActive(hay);
            textoAyuda.text = hay ? texto : "";
        }

        /// <summary>Llena una ventana con opciones navegables (navegación explícita arriba/abajo con wrap).</summary>
        void MostrarOpciones(RectTransform ventana, float ancho, List<Opcion> ops, Action alCancelar, int indiceInicial, List<GameObject> destino = null)
        {
            if (destino == null) destino = opcionesActuales;
            destino.Clear();

            var contenido = ventana.Find("Contenido") as RectTransform;
            if (contenido != null) { contenido.SetParent(null); Destroy(contenido.gameObject); }
            contenido = UIFactory.Estirar(UIFactory.Rect("Contenido", ventana));

            float alto = 24 + ops.Count * AltoOpcion;
            ventana.sizeDelta = new Vector2(ancho, alto);
            ventana.gameObject.SetActive(true);

            var botones = new List<Button>();
            for (int i = 0; i < ops.Count; i++)
            {
                var op = ops[i];
                var rt = UIFactory.Rect("Opcion_" + op.texto, contenido);
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -12 - i * AltoOpcion);
                rt.sizeDelta = new Vector2(-24, AltoOpcion - 2);

                var bg = rt.gameObject.AddComponent<Image>();
                bg.color = Color.white;
                var boton = rt.gameObject.AddComponent<Button>();
                boton.targetGraphic = bg;
                var cb = boton.colors;
                cb.normalColor = new Color(1, 1, 1, 0);
                cb.highlightedColor = new Color(1, 1, 1, 0.1f);
                cb.selectedColor = new Color(1, 1, 1, 0.18f);
                cb.pressedColor = new Color(1, 1, 1, 0.3f);
                cb.disabledColor = new Color(1, 1, 1, 0);
                cb.fadeDuration = 0.05f;
                boton.colors = cb;
                boton.interactable = op.habilitada;

                var puntero = T(rt, "►", 26, TextAnchor.MiddleLeft, Color.white);
                UIFactory.Estirar(puntero.rectTransform, 8, 0, 0, 0);
                puntero.enabled = false;

                var texto = T(rt, op.texto, 30, TextAnchor.MiddleLeft, op.habilitada ? (op.color ?? ColorTexto) : ColorDeshabilitado);
                UIFactory.Estirar(texto.rectTransform, 40, 10, 0, 0);
                if (!string.IsNullOrEmpty(op.derecha))
                {
                    var der = T(rt, op.derecha, 26, TextAnchor.MiddleRight, op.habilitada ? new Color(0.8f, 0.85f, 1f) : ColorDeshabilitado);
                    UIFactory.Estirar(der.rectTransform, 40, 14, 0, 0);
                }

                var om = rt.gameObject.AddComponent<OpcionMenu>();
                om.texto = texto;
                om.puntero = puntero;
                om.alCancelar = alCancelar;
                string ayuda = op.ayuda;
                Action sel = op.alSeleccionar;
                om.alSeleccionar = () => { MostrarAyuda(ayuda); sel?.Invoke(); };

                Action conf = op.alConfirmar;
                boton.onClick.AddListener(() => conf?.Invoke());

                botones.Add(boton);
                destino.Add(rt.gameObject);
            }

            // Navegación explícita para que no salte entre ventanas.
            for (int i = 0; i < botones.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = SiguienteHabilitado(botones, i, -1);
                nav.selectOnDown = SiguienteHabilitado(botones, i, +1);
                botones[i].navigation = nav;
            }

            // Selección inicial
            int idx = Mathf.Clamp(indiceInicial, 0, Mathf.Max(0, botones.Count - 1));
            Button inicial = null;
            for (int k = 0; k < botones.Count; k++)
            {
                var b = botones[(idx + k) % botones.Count];
                if (b.interactable) { inicial = b; break; }
            }
            if (inicial != null && EventSystem.current != null)
            {
                inicial.GetComponent<OpcionMenu>().silenciosa = true;
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(inicial.gameObject);
            }
        }

        static Button SiguienteHabilitado(List<Button> botones, int desde, int dir)
        {
            int n = botones.Count;
            for (int k = 1; k < n; k++)
            {
                var b = botones[((desde + dir * k) % n + n) % n];
                if (b.interactable) return b;
            }
            return null;
        }

        void AsegurarSeleccion(List<GameObject> opciones)
        {
            if (EventSystem.current == null || opciones.Count == 0) return;
            var sel = EventSystem.current.currentSelectedGameObject;
            if (sel != null && sel.activeInHierarchy && opciones.Contains(sel)) return;
            foreach (var o in opciones)
            {
                if (o == null) continue;
                var b = o.GetComponent<Button>();
                if (b != null && b.interactable)
                {
                    o.GetComponent<OpcionMenu>().silenciosa = true;
                    EventSystem.current.SetSelectedGameObject(o);
                    return;
                }
            }
        }

        // =====================================================================
        // API usada por el BattleManager
        // =====================================================================
        public void SetRonda(string texto) => textoRonda.text = texto;

        /// <summary>Hay un submenú (Módulos/Ítems) o una selección de objetivo abierta.</summary>
        public bool EnSubmenu => modo == Modo.Lista || modo == Modo.Objetivo;

        /// <summary>Hay algún menú de órdenes abierto.</summary>
        public bool EsperandoOrden => modo != Modo.Nada;

        void ActualizarModoATB()
        {
            string modo = bm.ModoTurnos
                ? $"{Loc.T("ui.modoTurnos")}  ·  {Loc.T(bm.FaseDe == Team.Jugador ? "ui.fase.jugador" : "ui.fase.enemigo")}"
                : Loc.T("ui.atb.activa");
            string texto = $"<color=#FFE070>{modo}</color>\n<size=18>{Loc.T("pausa.hint")}</size>";
            if (textoModo.text != texto) textoModo.text = texto;
        }

        public void AnunciarAccion(string nombre, Color color)
        {
            textoAccion.text = nombre;
            textoAccion.color = color;
            barraAccion.gameObject.SetActive(true);
            accionRestante = 1.4f;
        }

        public void Banner(string texto, float duracion, Color? color = null)
        {
            if (rutinaBanner != null) StopCoroutine(rutinaBanner);
            rutinaBanner = StartCoroutine(RutinaBanner(texto, duracion, color ?? Color.white));
        }

        IEnumerator RutinaBanner(string texto, float duracion, Color color)
        {
            textoBanner.text = texto;
            textoBanner.color = color;
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                grupoBanner.alpha = t / 0.25f;
                yield return null;
            }
            grupoBanner.alpha = 1f;
            yield return new WaitForSecondsRealtime(duracion);
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                grupoBanner.alpha = 1f - t / 0.35f;
                yield return null;
            }
            grupoBanner.alpha = 0f;
        }

        public void Popup(BattleUnit u, string texto, Color color, int tamano)
        {
            float ahora = Time.time;
            if (!ultimoPopup.TryGetValue(u, out float ultimo) || ahora - ultimo > 0.35f) pilaPopup[u] = 0;
            ultimoPopup[u] = ahora;
            int pila = pilaPopup.TryGetValue(u, out int p) ? p : 0;
            pilaPopup[u] = pila + 1;
            StartCoroutine(RutinaPopup(u, texto, color, tamano, pila));
        }

        IEnumerator RutinaPopup(BattleUnit u, string texto, Color color, int tamano, int pila)
        {
            var t = T(capaPopups, texto, tamano, TextAnchor.MiddleCenter, color);
            t.fontStyle = FontStyle.Bold;
            var rt = t.rectTransform;
            UIFactory.Ubicar(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 80));
            Vector3 ancla = u.View.Centro;
            Vector2 jitter = new Vector2(UnityEngine.Random.Range(-30f, 30f), pila * 42f);
            const float dur = 1.1f;
            for (float k = 0; k < dur; k += Time.deltaTime)
            {
                float n = k / dur;
                if (APantalla(ancla, out var local))
                    rt.anchoredPosition = local + jitter + new Vector2(0, 40f + n * 70f);
                float escala = n < 0.12f ? Mathf.Lerp(1.6f, 1f, n / 0.12f) : 1f;
                rt.localScale = Vector3.one * escala;
                var c = t.color;
                c.a = n > 0.7f ? 1f - (n - 0.7f) / 0.3f : 1f;
                t.color = c;
                yield return null;
            }
            Destroy(t.gameObject);
        }

        /// <summary>Oculta la UI (cinemáticas) mostrando barras negras tipo película.</summary>
        public void Ocultar(bool ocultar)
        {
            StartCoroutine(RutinaOcultar(ocultar));
        }

        IEnumerator RutinaOcultar(bool ocultar)
        {
            float a0 = grupoPrincipal.alpha;
            float a1 = ocultar ? 0f : 1f;
            float h0 = letterArriba.sizeDelta.y;
            float h1 = ocultar ? 130f : 0f;
            const float dur = 0.3f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                grupoPrincipal.alpha = Mathf.Lerp(a0, a1, k);
                float h = Mathf.Lerp(h0, h1, k);
                letterArriba.sizeDelta = new Vector2(0, h);
                letterAbajo.sizeDelta = new Vector2(0, h);
                yield return null;
            }
            grupoPrincipal.alpha = a1;
            letterArriba.sizeDelta = new Vector2(0, h1);
            letterAbajo.sizeDelta = new Vector2(0, h1);
            grupoPrincipal.interactable = !ocultar;
            grupoPrincipal.blocksRaycasts = !ocultar;
        }

        public void MostrarFin(bool victoria, bool ultimaPelea)
        {
            CerrarMenus();
            panelFin.gameObject.SetActive(true);

            var titulo = panelFin.Find("Titulo");
            if (titulo != null) Destroy(titulo.gameObject);
            string texto = victoria ? (ultimaPelea ? Loc.T("msg.campeonTorneo") : Loc.T("msg.victoria")) : Loc.T("msg.derrota");
            var t = T(panelFin, texto, 54, TextAnchor.MiddleCenter, victoria ? ColorActivo : new Color(1f, 0.4f, 0.4f));
            t.name = "Titulo";
            t.fontStyle = FontStyle.Bold;
            UIFactory.Ubicar(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(680, 80));

            var ops = new List<Opcion>();
            if (victoria && !ultimaPelea)
                ops.Add(new Opcion { texto = Loc.T("fin.siguiente"), alConfirmar = bm.Continuar });
            else
                ops.Add(new Opcion { texto = Loc.T("fin.menu"), alConfirmar = bm.VolverAlMenu });

            var sub = panelFin.Find("Subtitulo");
            if (sub != null) Destroy(sub.gameObject);
            if (!victoria)
            {
                var st = T(panelFin, Loc.T("msg.derrota.sub"), 24, TextAnchor.MiddleCenter, new Color(0.85f, 0.88f, 1f));
                st.name = "Subtitulo";
                UIFactory.Ubicar(st.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(680, 40));
            }

            // Contenedor de opciones debajo del título
            var cont = panelFin.Find("Opciones") as RectTransform;
            if (cont == null)
            {
                cont = UIFactory.Rect("Opciones", panelFin);
                UIFactory.Ubicar(cont, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(420, 200));
            }
            MostrarOpciones(cont, 420, ops, null, 0, opcionesFin);
            // MostrarOpciones redimensiona el contenedor; lo re-anclamos abajo.
            cont.anchoredPosition = new Vector2(0, 30);
        }
    }
}
