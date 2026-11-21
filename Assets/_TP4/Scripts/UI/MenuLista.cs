using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TP4
{
    public class OpcionLista
    {
        public string texto;
        public Func<string> valor;
        public bool habilitado = true;
        public string ayuda;
        public Color? color;
        public Action alConfirmar;
        public Action<int> alCambiar;
        public Action alClick;
    }

    public class MenuLista : MonoBehaviour
    {
        RectTransform ventana;
        Font fuente;
        readonly List<GameObject> opciones = new List<GameObject>();
        readonly List<(Text texto, OpcionLista item)> valores = new List<(Text, OpcionLista)>();
        int ultimoIndice;

        public Action<string> alMostrarAyuda;
        public bool Activa { get; set; } = true;
        public int IndiceActual => ultimoIndice;

        public static MenuLista Crear(RectTransform ventana, Font fuente)
        {
            var m = ventana.gameObject.AddComponent<MenuLista>();
            m.ventana = ventana;
            m.fuente = fuente;
            return m;
        }

        public void Mostrar(List<OpcionLista> items, Action alCancelar, int indiceInicial = 0, float ancho = 560f, float altoOpcion = 60f, int tamTexto = 34)
        {
            opciones.Clear();
            valores.Clear();

            var viejo = ventana.Find("Contenido");
            if (viejo != null) { viejo.SetParent(null); Destroy(viejo.gameObject); }
            var contenido = UIFactory.Estirar(UIFactory.Rect("Contenido", ventana));

            ventana.sizeDelta = new Vector2(ancho, 28 + items.Count * altoOpcion);

            var botones = new List<Button>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int indice = i;
                var rt = UIFactory.Rect("Opcion_" + i, contenido);
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -14 - i * altoOpcion);
                rt.sizeDelta = new Vector2(-28, altoOpcion - 4);

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
                boton.interactable = item.habilitado;

                var puntero = UIFactory.Texto(rt, "►", tamTexto - 6, TextAnchor.MiddleLeft, Color.white, fuente);
                UIFactory.Estirar(puntero.rectTransform, 10, 0, 0, 0);
                puntero.enabled = false;

                Color colorTexto = item.habilitado ? (item.color ?? Color.white) : new Color(0.55f, 0.58f, 0.65f);
                var texto = UIFactory.Texto(rt, item.texto, tamTexto, TextAnchor.MiddleLeft, colorTexto, fuente);
                UIFactory.Estirar(texto.rectTransform, 46, 10, 0, 0);

                if (item.valor != null)
                {
                    var der = UIFactory.Texto(rt, "", tamTexto - 2, TextAnchor.MiddleRight, new Color(1f, 0.9f, 0.5f), fuente);
                    der.supportRichText = true;
                    UIFactory.Estirar(der.rectTransform, 46, 16, 0, 0);
                    valores.Add((der, item));
                }

                var om = rt.gameObject.AddComponent<OpcionMenu>();
                om.texto = texto;
                om.puntero = puntero;
                om.alCancelar = alCancelar;
                string ayuda = item.ayuda;
                om.alSeleccionar = () => { ultimoIndice = indice; alMostrarAyuda?.Invoke(ayuda); };
                if (item.alCambiar != null)
                {
                    om.alMoverHorizontal = d =>
                    {
                        item.alCambiar(d);
                        Sfx.Play(SfxId.Mover, 0.6f);
                        RefrescarValores();
                    };
                }

                boton.onClick.AddListener(() =>
                {
                    if (item.alConfirmar != null)
                    {
                        Sfx.Play(SfxId.Confirmar, 0.7f);
                        item.alConfirmar();
                    }
                    else if (item.alClick != null || item.alCambiar != null)
                    {
                        if (item.alClick != null) item.alClick();
                        else item.alCambiar(+1);
                        Sfx.Play(SfxId.Mover, 0.6f);
                        RefrescarValores();
                    }
                });

                botones.Add(boton);
                opciones.Add(rt.gameObject);
            }

            for (int i = 0; i < botones.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = Siguiente(botones, i, -1);
                nav.selectOnDown = Siguiente(botones, i, +1);
                botones[i].navigation = nav;
            }

            RefrescarValores();
            ultimoIndice = Mathf.Clamp(indiceInicial, 0, Mathf.Max(0, botones.Count - 1));
            Seleccionar(ultimoIndice);
        }

        public void RefrescarValores()
        {
            foreach (var (t, item) in valores)
                if (t != null) t.text = $"◄  {item.valor()}  ►";
        }

        static Button Siguiente(List<Button> botones, int desde, int dir)
        {
            int n = botones.Count;
            for (int k = 1; k < n; k++)
            {
                var b = botones[((desde + dir * k) % n + n) % n];
                if (b.interactable) return b;
            }
            return null;
        }

        void Seleccionar(int indice)
        {
            if (EventSystem.current == null || opciones.Count == 0) return;
            for (int k = 0; k < opciones.Count; k++)
            {
                var o = opciones[(indice + k) % opciones.Count];
                var b = o.GetComponent<Button>();
                if (b == null || !b.interactable) continue;
                o.GetComponent<OpcionMenu>().silenciosa = true;
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(o);
                return;
            }
        }

        void Update()
        {
            if (!Activa || EventSystem.current == null || opciones.Count == 0) return;
            var sel = EventSystem.current.currentSelectedGameObject;
            if (sel != null && sel.activeInHierarchy && opciones.Contains(sel)) return;
            Seleccionar(ultimoIndice);
        }
    }

    public static class PanelOpciones
    {
        public static List<OpcionLista> Items(Action alVolver)
        {
            return new List<OpcionLista>
            {
                new OpcionLista
                {
                    texto = Loc.T("op.atb"),
                    ayuda = Loc.T("op.atb.ayuda") + (BattleManager.Instance != null ? " " + Loc.T("op.atb.proxima") : ""),
                    valor = () => Ajustes.ATBActivo ? Loc.T("op.si") : Loc.T("op.no"),
                    alCambiar = d => Ajustes.ATBActivo = !Ajustes.ATBActivo
                },
                new OpcionLista
                {
                    texto = Loc.T("op.volumen"),
                    ayuda = Loc.T("op.volumen.ayuda"),
                    valor = () => Ajustes.Volumen + "%",
                    alCambiar = d =>
                    {
                        int v = Ajustes.Volumen;
                        Ajustes.Volumen = (v == 1 && d > 0) ? 5 : v + d * 5;
                    },
                    alClick = () => Ajustes.Volumen = Ajustes.Volumen >= 100 ? 10 : Mathf.Min(100, Ajustes.Volumen + 10)
                },
                new OpcionLista
                {
                    texto = Loc.T("op.idioma"),
                    ayuda = Loc.T("op.idioma.ayuda"),
                    valor = () => Loc.T("op.idioma.valor"),
                    alCambiar = d => Ajustes.Idioma = Ajustes.Idioma == Idioma.Espanol ? Idioma.Ingles : Idioma.Espanol
                },
                new OpcionLista
                {
                    texto = Loc.T("menu.volver"),
                    alConfirmar = alVolver
                }
            };
        }
    }
}
