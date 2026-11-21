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
    public class BattleUI : MonoBehaviour
    {
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

        class Fila
        {
            public BattleUnit u;
            public Text nombre, estados, vida, energia;
            public Image resaltado;
            public UIFactory.Barra barraVida, barraEnergia, barraOD, barraATB;
            public Color cNombre, cVida, cOD, cATB;
        }
        readonly List<Fila> filas = new List<Fila>();

        class Etiqueta
        {
            public BattleUnit u;
            public RectTransform rt;
            public CanvasGroup grupo;
            public Text nombre, estados;
            public UIFactory.Barra vida;
        }
        readonly List<Etiqueta> etiquetas = new List<Etiqueta>();

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
        Text tituloFin, subtituloFin;
        RectTransform opcionesFinRect;
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

        public static BattleUI Crear(BattleManager bm, BattleVisuals vis)
        {
            AsegurarEventSystem();

            GameObject go = vis != null && vis.prefabUICombate != null
                ? UnityEngine.Object.Instantiate(vis.prefabUICombate)
                : new GameObject("BattleUI");
            go.name = "BattleUI";
            UIFactory.AsegurarCanvas(go, 10);

            var ui = UIBuilder.Componente<BattleUI>(go);
            ui.bm = bm;
            ui.vis = vis;
            ui.fuente = vis != null && vis.fuente != null ? vis.fuente : UIFactory.FuentePorDefecto;
            ui.cam = Camera.main;
            ui.raiz = (RectTransform)go.transform;
            ui.Construir();
            return ui;
        }

        public static GameObject GenerarPlantilla(BattleVisuals vis, GameObject existente = null)
        {
            var go = existente != null ? existente : new GameObject("UI_Combate");
            UIFactory.AsegurarCanvas(go, 10);
            var ui = go.AddComponent<BattleUI>();
            ui.vis = vis;
            ui.fuente = vis != null && vis.fuente != null ? vis.fuente : UIFactory.FuentePorDefecto;
            ui.raiz = (RectTransform)go.transform;
            ui.Construir();
            UnityEngine.Object.DestroyImmediate(ui);
            return go;
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

        void AlCambiarAjustes()
        {
            if (bm != null) bm.RefrescarTextos();
        }

        void OnDestroy()
        {
            Ajustes.Cambio -= AlCambiarAjustes;
        }

        static Action<RectTransform> ArribaIzq(float x, float y, float w, float h)
            => rt => UIFactory.Ubicar(rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h));

        bool EnJuego => bm != null && Application.isPlaying;

        void Construir()
        {
            var b = new UIBuilder(vis);
            if (EnJuego) Ajustes.Cambio += AlCambiarAjustes;

            principal = b.Rect(raiz, "Principal", rt => UIFactory.Estirar(rt));
            grupoPrincipal = UIBuilder.Componente<CanvasGroup>(principal.gameObject);
            capaMundo = b.Rect(principal, "Mundo", rt => UIFactory.Estirar(rt));

            ConstruirPanelInferior(b);
            if (EnJuego) ConstruirEtiquetasEnemigos();
            ConstruirVentanas(b);

            cursorActivo = b.Texto(capaMundo, "CursorActivo", "▼", 44, TextAnchor.MiddleCenter, new Color(0.4f, 1f, 1f),
                rt => UIFactory.Ubicar(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(60, 60)));

            capaPopups = b.Rect(raiz, "Popups", rt => UIFactory.Estirar(rt));

            letterArriba = b.Rect(raiz, "LetterboxArriba", rt =>
            {
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1); rt.sizeDelta = new Vector2(0, 0);
                rt.gameObject.AddComponent<Image>().color = Color.black;
            });
            letterAbajo = b.Rect(raiz, "LetterboxAbajo", rt =>
            {
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(0.5f, 0); rt.sizeDelta = new Vector2(0, 0);
                rt.gameObject.AddComponent<Image>().color = Color.black;
            });

            banner = b.Rect(raiz, "Banner", rt =>
            {
                rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = new Vector2(0, 120); rt.sizeDelta = new Vector2(0, 170);
                var img = rt.gameObject.AddComponent<Image>();
                img.color = new Color(0, 0, 0.1f, 0.65f);
                img.raycastTarget = false;
            });
            grupoBanner = UIBuilder.Componente<CanvasGroup>(banner.gameObject);
            grupoBanner.blocksRaycasts = false;
            textoBanner = b.Texto(banner, "TextoBanner", "¡VICTORIA!", 64, TextAnchor.MiddleCenter, ColorTexto, rt =>
            {
                UIFactory.Estirar(rt);
                rt.GetComponent<Text>().fontStyle = FontStyle.Bold;
            });

            panelFin = b.Ventana(raiz, "PanelFin", rt =>
                UIFactory.Ubicar(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(720, 360)));
            tituloFin = b.Texto(panelFin, "Titulo", "¡VICTORIA!", 54, TextAnchor.MiddleCenter, ColorActivo,
                rt =>
                {
                    UIFactory.Ubicar(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(680, 80));
                    rt.GetComponent<Text>().fontStyle = FontStyle.Bold;
                });
            subtituloFin = b.Texto(panelFin, "Subtitulo", "", 24, TextAnchor.MiddleCenter, new Color(0.85f, 0.88f, 1f),
                rt => UIFactory.Ubicar(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(680, 40)));
            opcionesFinRect = b.Rect(panelFin, "Opciones", rt =>
                UIFactory.Ubicar(rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(420, 200)));

            if (EnJuego)
            {
                cursorActivo.enabled = false;
                grupoBanner.alpha = 0f;
                panelFin.gameObject.SetActive(false);
                ventanaComandos.gameObject.SetActive(false);
                ventanaLista.gameObject.SetActive(false);
                ventanaObjetivos.gameObject.SetActive(false);
                barraAyuda.gameObject.SetActive(false);
                barraAccion.gameObject.SetActive(false);
                letterArriba.sizeDelta = new Vector2(letterArriba.sizeDelta.x, 0);
                letterAbajo.sizeDelta = new Vector2(letterAbajo.sizeDelta.x, 0);
                textoBanner.text = "";
            }
        }

        void ConstruirPanelInferior(UIBuilder b)
        {
            var nombres = b.Ventana(principal, "PanelNombres", rt =>
                UIFactory.Ubicar(rt, Vector2.zero, Vector2.zero, new Vector2(Margen, Margen), new Vector2(600, AltoPanel)));
            var estado = b.Ventana(principal, "PanelEstado", rt =>
                UIFactory.Ubicar(rt, Vector2.zero, Vector2.zero, new Vector2(Margen * 2 + 600, Margen), new Vector2(1920 - Margen * 3 - 600, AltoPanel)));

            Color cab = new Color(0.75f, 0.8f, 0.95f);
            b.TextoTraducible(nombres, "Enc_Nombre", "ui.nombre", 22, TextAnchor.UpperLeft, cab, ArribaIzq(24, 12, 300, 30));
            b.TextoTraducible(estado, "Enc_Vida", "ui.vida", 22, TextAnchor.UpperLeft, cab, ArribaIzq(30, 12, 200, 30));
            b.TextoTraducible(estado, "Enc_Energia", "ui.energia", 22, TextAnchor.UpperLeft, cab, ArribaIzq(380, 12, 200, 30));
            b.TextoTraducible(estado, "Enc_Overdrive", "ui.overdrive", 22, TextAnchor.UpperLeft, cab, ArribaIzq(640, 12, 260, 30));
            var encATB = b.TextoTraducible(estado, "Enc_ATB", "ui.atb", 22, TextAnchor.UpperLeft, cab, ArribaIzq(950, 12, 260, 30));
            var encTurno = b.TextoTraducible(estado, "Enc_Turno", "ui.turno", 22, TextAnchor.UpperLeft, cab, ArribaIzq(950, 12, 260, 30));
            if (EnJuego)
            {
                encATB.gameObject.SetActive(!bm.ModoTurnos);
                encTurno.gameObject.SetActive(bm.ModoTurnos);
            }
            else encTurno.gameObject.SetActive(false);

            int cantidad = EnJuego ? bm.Jugadores.Count : 3;
            for (int i = 0; i < Mathf.Max(cantidad, 3); i++)
            {
                float y = 50 + i * AltoFila;
                bool usar = i < cantidad;
                if (!usar && nombres.Find("Nombre" + i) == null) continue;

                var f = new Fila { u = EnJuego && usar ? bm.Jugadores[i] : null };

                var res = b.Rect(nombres, "Resaltado" + i, rt =>
                {
                    ArribaIzq(8, y - 4, 584, AltoFila - 6)(rt);
                    var img = rt.gameObject.AddComponent<Image>();
                    img.color = new Color(1f, 1f, 1f, 0.12f);
                    img.raycastTarget = false;
                });
                f.resaltado = res.GetComponent<Image>();
                f.nombre = b.Texto(nombres, "Nombre" + i, "Robot " + (i + 1), 34, TextAnchor.MiddleLeft, ColorTexto, ArribaIzq(24, y, 300, AltoFila - 14));
                f.estados = b.Texto(nombres, "Estados" + i, "", 22, TextAnchor.MiddleRight, ColorTexto, ArribaIzq(300, y, 280, AltoFila - 14));
                f.vida = b.Texto(estado, "Vida" + i, "350/350", 32, TextAnchor.UpperRight, ColorTexto, ArribaIzq(30, y, 300, 40));
                f.barraVida = b.Barra(estado, "BarraVida" + i, ColorVida, ArribaIzq(30, y + 42, 300, 12));
                f.energia = b.Texto(estado, "Energia" + i, "50/50", 32, TextAnchor.UpperRight, ColorTexto, ArribaIzq(380, y, 200, 40));
                f.barraEnergia = b.Barra(estado, "BarraEnergia" + i, ColorEnergia, ArribaIzq(380, y + 42, 200, 12));
                f.barraOD = b.Barra(estado, "BarraOD" + i, ColorOD, ArribaIzq(640, y + 16, 260, 24));
                f.barraATB = b.Barra(estado, "BarraATB" + i, ColorATB, ArribaIzq(950, y + 16, 260, 24));

                if (!EnJuego) continue;
                if (!usar)
                {
                    foreach (var g in new Component[] { f.resaltado, f.nombre, f.estados, f.vida, f.energia })
                        if (g != null) g.gameObject.SetActive(false);
                    foreach (var barra in new[] { f.barraVida, f.barraEnergia, f.barraOD, f.barraATB })
                        if (barra != null && barra.raiz != null) barra.raiz.gameObject.SetActive(false);
                    continue;
                }
                f.resaltado.enabled = false;
                f.nombre.text = f.u.Nombre;
                f.cNombre = f.nombre.color;
                f.cVida = f.vida.color;
                f.cOD = f.barraOD.imagenRelleno != null ? f.barraOD.imagenRelleno.color : ColorOD;
                f.cATB = f.barraATB.imagenRelleno != null ? f.barraATB.imagenRelleno.color : ColorATB;
                filas.Add(f);
                RegistrarUnidad(f.u);
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
                e.vida = UIFactory.CrearBarra(e.rt, new Color(1f, 0.35f, 0.3f), vis);
                UIFactory.Ubicar(e.vida.raiz, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(ancho - 20, jefe ? 14 : 10));
                e.estados = T(e.rt, "", 18, TextAnchor.UpperCenter);
                UIFactory.Ubicar(e.estados.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -12), new Vector2(ancho, 22));
                etiquetas.Add(e);
                RegistrarUnidad(u);
            }
        }

        void ConstruirVentanas(UIBuilder b)
        {
            float baseY = Margen + AltoPanel + 8;

            ventanaComandos = b.Ventana(principal, "VentanaComandos", rt =>
                UIFactory.Ubicar(rt, Vector2.zero, Vector2.zero, new Vector2(Margen * 2 + 600, baseY), new Vector2(300, 200)));
            ventanaLista = b.Ventana(principal, "VentanaLista", rt =>
                UIFactory.Ubicar(rt, Vector2.zero, Vector2.zero, new Vector2(Margen * 3 + 900, baseY), new Vector2(620, 200)));
            ventanaObjetivos = b.Ventana(principal, "VentanaObjetivos", rt =>
                UIFactory.Ubicar(rt, Vector2.zero, Vector2.zero, new Vector2(Margen, baseY), new Vector2(600, 200)));

            barraAyuda = b.Ventana(principal, "Ayuda", rt =>
                UIFactory.Ubicar(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -Margen), new Vector2(1000, 64)));
            textoAyuda = b.Texto(barraAyuda, "TextoAyuda", "Descripción de la acción seleccionada.", 26, TextAnchor.MiddleCenter, ColorTexto,
                rt =>
                {
                    UIFactory.Estirar(rt, 20, 20, 0, 0);
                    rt.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;
                });

            barraAccion = b.Ventana(principal, "Accion", rt =>
                UIFactory.Ubicar(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -Margen - 76), new Vector2(640, 60)));
            textoAccion = b.Texto(barraAccion, "TextoAccion", "Nombre de la acción", 30, TextAnchor.MiddleCenter, ColorTexto, rt => UIFactory.Estirar(rt));

            textoRonda = b.TextoPlantilla(principal, "TextoRonda", "ui.ronda", "Ronda 1/3  ·  vs Rival  ·  Normal", 24, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 1f),
                rt => UIFactory.Ubicar(rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -20), new Vector2(500, 40)));
            textoModo = b.TextoPlantilla(principal, "TextoModo", "ui.turnoDe", "Turno: Vanguardia", 24, TextAnchor.UpperRight, new Color(0.85f, 0.9f, 1f),
                rt => UIFactory.Ubicar(rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -20), new Vector2(430, 70)));
        }

        void RegistrarUnidad(BattleUnit u)
        {
            u.Damaged += (x, d) => Popup(x, d.ToString(), Color.white, 46);
            u.Healed += (x, d) => Popup(x, "+" + d, new Color(0.4f, 1f, 0.5f), 46);
            u.EnergyRestored += (x, d) => Popup(x, "+" + d + " EN", ColorEnergia, 38);
            u.Message += (x, txt, c) => Popup(x, txt, c, 30);
        }

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
                f.nombre.color = !u.IsAlive ? new Color(1f, 0.35f, 0.35f) : esActivo ? ColorActivo : f.cNombre;
                f.vida.text = $"{u.Vida}/{u.VidaMax}";
                f.vida.color = !u.IsAlive ? new Color(1f, 0.35f, 0.35f) : u.VidaPct < 0.25f ? new Color(1f, 0.85f, 0.3f) : f.cVida;
                f.barraVida.Set(u.VidaPct);
                f.energia.text = $"{u.Energia}/{u.EnergiaMax}";
                f.barraEnergia.Set(u.EnergiaMax > 0 ? (float)u.Energia / u.EnergiaMax : 0f);

                f.barraOD.Set(u.Overdrive);
                f.barraOD.imagenRelleno.color = u.Overdrive >= 1f
                    ? Color.Lerp(ColorODLleno, Color.white, (Mathf.Sin(Time.time * 8f) + 1f) * 0.25f)
                    : f.cOD;

                f.barraATB.Set(Mathf.Max(0f, u.ATB));
                f.barraATB.imagenRelleno.color = u.TieneEstadoLiberable ? new Color(0.4f, 0.95f, 1f)
                    : u.ATBCongelada ? ColorATBCongelada
                    : u.ATB >= 1f ? Color.Lerp(ColorATBLleno, Color.white, (Mathf.Sin(Time.time * 6f) + 1f) * 0.2f)
                    : f.cATB;

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
                e.rt.anchoredPosition = LimitarEtiqueta(local);
                float objetivoAlpha = visible && e.u.IsAlive ? 1f : 0f;
                e.grupo.alpha = Mathf.MoveTowards(e.grupo.alpha, objetivoAlpha, Time.deltaTime * 2f);
                e.vida.Set(e.u.VidaPct);
                e.estados.text = TextoEstados(e.u);
            }
        }

        Vector2 LimitarEtiqueta(Vector2 local)
        {
            float maxY = raiz.rect.height * 0.5f - 240f;
            float maxX = raiz.rect.width * 0.5f - 230f;
            return new Vector2(Mathf.Clamp(local.x, -maxX, maxX), Mathf.Min(local.y, maxY));
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
                    bool enemigo = objetivosResaltados[i].Team == Team.Enemigo;
                    if (enemigo) p = LimitarEtiqueta(p);
                    cursores[i].rectTransform.anchoredPosition = p + new Vector2(0, (enemigo ? 70f : 10f) + bob);
                }
                else cursores[i].enabled = false;
            }
        }

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
            var liberar = GameSession.Torneo != null ? GameSession.Torneo.accionLiberar : null;
            if (liberar != null && bm.Vivos(u.Team).Exists(a => a != u && a.TieneEstadoLiberable))
            {
                ops.Add(new Opcion
                {
                    texto = Loc.T("cmd.liberar"),
                    color = new Color(0.5f, 1f, 1f),
                    ayuda = Loc.T("cmd.liberar.ayuda"),
                    alConfirmar = () => Elegir(liberar, () => AbrirComandos(u, 0))
                });
            }
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
                {
                    var aliados = bm.Vivos(activo.Team);
                    if (accion.efectos.Exists(e => e.tipo == EffectType.Liberar))
                        aliados = aliados.FindAll(a => a.TieneEstadoLiberable);
                    foreach (var v in aliados) AgregarOpcionObjetivo(ops, v, $"{v.Vida}/{v.VidaMax}");
                    break;
                }

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

        void MostrarOpciones(RectTransform ventana, float ancho, List<Opcion> ops, Action alCancelar, int indiceInicial, List<GameObject> destino = null)
        {
            if (destino == null) destino = opcionesActuales;
            destino.Clear();

            var contenido = ventana.Find("Contenido") as RectTransform;
            if (contenido != null) { contenido.SetParent(null); Destroy(contenido.gameObject); }
            contenido = UIFactory.Estirar(UIFactory.Rect("Contenido", ventana));

            float alto = 24 + ops.Count * AltoOpcion;
            float anchoReal = ventana.sizeDelta.x > 1f ? ventana.sizeDelta.x : ancho;
            ventana.sizeDelta = new Vector2(anchoReal, alto);
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

            for (int i = 0; i < botones.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = SiguienteHabilitado(botones, i, -1);
                nav.selectOnDown = SiguienteHabilitado(botones, i, +1);
                botones[i].navigation = nav;
            }

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

        public void SetRonda(params object[] valores)
        {
            var loc = textoRonda.GetComponent<TextoLoc>();
            if (loc != null && loc.esPlantilla) loc.Formatear(valores);
            else textoRonda.text = Loc.T("ui.ronda", valores);
        }

        public bool EnSubmenu => modo == Modo.Lista || modo == Modo.Objetivo;

        public bool EsperandoOrden => modo != Modo.Nada;

        void ActualizarModoATB()
        {
            var u = bm.UnidadEnTurno;
            string nombre = u == null ? "—"
                : $"<color=#{(u.EsJugador ? "8FE8FF" : "FF8A80")}>{u.Nombre}</color>";
            var loc = textoModo.GetComponent<TextoLoc>();
            string plantilla = loc != null && loc.esPlantilla ? loc.Plantilla : Loc.T("ui.turnoDe");
            string texto;
            try { texto = string.Format(plantilla, nombre); }
            catch (FormatException) { texto = plantilla; }
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

            tituloFin.text = victoria ? (ultimaPelea ? Loc.T("msg.campeonTorneo") : Loc.T("msg.victoria")) : Loc.T("msg.derrota");
            tituloFin.color = victoria ? ColorActivo : new Color(1f, 0.4f, 0.4f);
            subtituloFin.text = victoria ? "" : Loc.T("msg.derrota.sub");

            var ops = new List<Opcion>();
            if (victoria && !ultimaPelea)
                ops.Add(new Opcion { texto = Loc.T("fin.siguiente"), alConfirmar = bm.Continuar });
            else
                ops.Add(new Opcion { texto = Loc.T("fin.menu"), alConfirmar = bm.VolverAlMenu });

            var cont = opcionesFinRect;
            Vector2 posOpciones = cont.anchoredPosition;
            MostrarOpciones(cont, 420, ops, null, 0, opcionesFin);
            cont.anchoredPosition = posOpciones;
        }
    }
}
