using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TP4
{
    /// <summary>
    /// Menú de pausa del combate: Continuar / Opciones / Abandonar torneo.
    /// Se abre con Esc o P (teclado) o Start (joystick). Detiene el tiempo (Time.timeScale = 0).
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static bool Abierta { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarEstado() => Abierta = false;

        enum Pantalla { Principal, Opciones, Confirmar }

        BattleManager bm;
        BattleVisuals vis;
        Font fuente;
        RectTransform contenido;
        RectTransform ventana;
        Text titulo;
        Text ayuda;
        MenuLista lista;
        Pantalla pantalla;
        Idioma idiomaMostrado;
        bool estabaEnSubmenu;
        int frameCambio = -1;

        public static PauseMenu Crear(BattleManager bm, BattleVisuals vis)
        {
            // Si existe el prefab editable (TP4 > Generar UI editable) se usa ese; si no, se arma por código.
            GameObject go = vis != null && vis.prefabUIPausa != null ? Instantiate(vis.prefabUIPausa) : new GameObject("PauseMenu");
            go.name = "PauseMenu";
            var raiz = UIFactory.AsegurarCanvas(go, 30);

            var p = UIBuilder.Componente<PauseMenu>(go);
            p.bm = bm;
            p.vis = vis;
            p.fuente = vis != null && vis.fuente != null ? vis.fuente : UIFactory.FuentePorDefecto;
            p.Construir(raiz, true);
            return p;
        }

        /// <summary>Editor: arma el menú de pausa (visible) para guardarlo como prefab editable.</summary>
        public static GameObject GenerarPlantilla(BattleVisuals vis, GameObject existente = null)
        {
            var go = existente != null ? existente : new GameObject("UI_Pausa");
            var raiz = UIFactory.AsegurarCanvas(go, 30);
            var p = go.AddComponent<PauseMenu>();
            p.vis = vis;
            p.fuente = vis != null && vis.fuente != null ? vis.fuente : UIFactory.FuentePorDefecto;
            p.Construir(raiz, false);
            DestroyImmediate(p);
            return go;
        }

        void Construir(RectTransform raiz, bool enJuego)
        {
            var b = new UIBuilder(vis);
            contenido = b.Rect(raiz, "Contenido", rt =>
            {
                UIFactory.Estirar(rt);
                rt.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0.05f, 0.7f);
            });

            titulo = b.Texto(contenido, "Titulo", "PAUSA", 64, TextAnchor.MiddleCenter, Color.white, rt =>
            {
                UIFactory.Ubicar(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0, 170), new Vector2(1200, 90));
                rt.GetComponent<Text>().fontStyle = FontStyle.Bold;
            });

            ventana = b.Ventana(contenido, "Ventana", rt =>
                UIFactory.Ubicar(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, 150), new Vector2(600, 200)));

            ayuda = b.Texto(contenido, "Ayuda", "Descripción de la opción seleccionada.", 28, TextAnchor.MiddleCenter, new Color(0.85f, 0.9f, 1f),
                rt => UIFactory.Ubicar(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(1600, 50)));

            if (!enJuego) return;
            lista = MenuLista.Crear(ventana, fuente);
            lista.alMostrarAyuda = t => ayuda.text = t ?? "";
            ayuda.text = "";
            contenido.gameObject.SetActive(false);
            Ajustes.Cambio += AlCambiarAjustes;
        }

        void OnDestroy()
        {
            Ajustes.Cambio -= AlCambiarAjustes;
            if (Abierta)
            {
                Abierta = false;
                Time.timeScale = 1f;
            }
        }

        static bool TeclaPausa(bool incluirEsc)
        {
            var kb = Keyboard.current;
            var gp = Gamepad.current;
            if (gp != null && gp.startButton.wasPressedThisFrame) return true;
            if (kb == null) return false;
            return kb.pKey.wasPressedThisFrame || (incluirEsc && kb.escapeKey.wasPressedThisFrame);
        }

        void Update()
        {
            if (frameCambio == Time.frameCount) return;

            if (!Abierta)
            {
                bool puede = bm != null && (bm.Fase == BattlePhase.Intro || bm.Fase == BattlePhase.EnCurso);
                // Esc solo pausa si no se estaba usando para volver de un submenú.
                bool enSubmenu = estabaEnSubmenu || (bm != null && bm.UI != null && bm.UI.EnSubmenu);
                if (puede && TeclaPausa(!enSubmenu)) Abrir();
            }
            else if (TeclaPausa(false))
            {
                Cerrar();
            }
        }

        void LateUpdate()
        {
            estabaEnSubmenu = bm != null && bm.UI != null && bm.UI.EnSubmenu;
        }

        void Abrir()
        {
            Abierta = true;
            frameCambio = Time.frameCount;
            Time.timeScale = 0f;
            contenido.gameObject.SetActive(true);
            Sfx.Play(SfxId.Confirmar, 0.7f);
            MostrarPrincipal(0);
        }

        void Cerrar()
        {
            Abierta = false;
            frameCambio = Time.frameCount;
            Time.timeScale = 1f;
            contenido.gameObject.SetActive(false);
            Sfx.Play(SfxId.Cancelar, 0.6f);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>Cancelar (Esc / B) se ignora en el mismo frame en que se abrió el menú.</summary>
        System.Action Cancelar(System.Action accion) => () =>
        {
            if (frameCambio == Time.frameCount) return;
            accion();
        };

        void MostrarPrincipal(int indice)
        {
            pantalla = Pantalla.Principal;
            idiomaMostrado = Ajustes.Idioma;
            titulo.text = Loc.T("pausa.titulo");
            lista.Mostrar(new List<OpcionLista>
            {
                new OpcionLista { texto = Loc.T("pausa.continuar"), alConfirmar = Cerrar },
                new OpcionLista { texto = Loc.T("menu.opciones"), ayuda = Loc.T("menu.opciones.ayuda"), alConfirmar = () => MostrarOpciones(0) },
                new OpcionLista { texto = Loc.T("pausa.abandonar"), ayuda = Loc.T("pausa.abandonar.ayuda"), color = new Color(1f, 0.6f, 0.55f),
                    alConfirmar = () => MostrarConfirmar() },
            }, Cancelar(Cerrar), indice);
        }

        void MostrarOpciones(int indice)
        {
            pantalla = Pantalla.Opciones;
            idiomaMostrado = Ajustes.Idioma;
            titulo.text = Loc.T("op.titulo");
            lista.Mostrar(PanelOpciones.Items(() => MostrarPrincipal(1)), Cancelar(() => MostrarPrincipal(1)), indice, 700f);
        }

        void MostrarConfirmar()
        {
            pantalla = Pantalla.Confirmar;
            idiomaMostrado = Ajustes.Idioma;
            titulo.text = Loc.T("pausa.confirmar");
            lista.Mostrar(new List<OpcionLista>
            {
                new OpcionLista { texto = Loc.T("pausa.no"), alConfirmar = () => MostrarPrincipal(2) },
                new OpcionLista { texto = Loc.T("pausa.si"), color = new Color(1f, 0.6f, 0.55f), alConfirmar = Abandonar },
            }, Cancelar(() => MostrarPrincipal(2)), 0);
        }

        void Abandonar()
        {
            Abierta = false;
            Time.timeScale = 1f;
            bm.VolverAlMenu();
        }

        void AlCambiarAjustes()
        {
            // Si cambió el idioma, se rearma la pantalla actual con los textos nuevos.
            if (!Abierta || Ajustes.Idioma == idiomaMostrado) return;
            if (pantalla == Pantalla.Opciones) MostrarOpciones(lista.IndiceActual);
        }
    }
}
