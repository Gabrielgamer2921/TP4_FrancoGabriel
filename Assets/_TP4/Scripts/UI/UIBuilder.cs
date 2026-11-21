using System;
using UnityEngine;
using UnityEngine.UI;

namespace TP4
{
    public class UIBuilder
    {
        public readonly BattleVisuals vis;
        public readonly Font fuente;

        public UIBuilder(BattleVisuals vis)
        {
            this.vis = vis;
            fuente = vis != null && vis.fuente != null ? vis.fuente : UIFactory.FuentePorDefecto;
        }

        static T Buscar<T>(Transform padre, string nombre) where T : Component
        {
            var t = padre.Find(nombre);
            return t != null ? t.GetComponent<T>() : null;
        }

        public RectTransform Rect(Transform padre, string nombre, Action<RectTransform> crear = null)
        {
            var rt = Buscar<RectTransform>(padre, nombre);
            if (rt != null) return rt;
            rt = UIFactory.Rect(nombre, padre);
            crear?.Invoke(rt);
            return rt;
        }

        public RectTransform Ventana(Transform padre, string nombre, Action<RectTransform> ubicar)
        {
            var rt = Buscar<RectTransform>(padre, nombre);
            if (rt != null) return rt;
            rt = (RectTransform)UIFactory.Ventana(nombre, padre, vis).transform;
            ubicar?.Invoke(rt);
            return rt;
        }

        public Text Texto(Transform padre, string nombre, string contenido, int tamano, TextAnchor alineacion, Color color,
            Action<RectTransform> ubicar)
        {
            var t = Buscar<Text>(padre, nombre);
            if (t != null) return t;
            t = UIFactory.Texto(padre, contenido, tamano, alineacion, color, fuente);
            t.name = nombre;
            t.supportRichText = true;
            ubicar?.Invoke(t.rectTransform);
            return t;
        }

        public Text TextoTraducible(Transform padre, string nombre, string clave, int tamano, TextAnchor alineacion, Color color,
            Action<RectTransform> ubicar)
        {
            var existente = Buscar<Text>(padre, nombre);
            if (existente != null) return existente;
            var t = Texto(padre, nombre, Loc.Es(clave), tamano, alineacion, color, ubicar);
            var loc = t.gameObject.AddComponent<TextoLoc>();
            loc.espanol = Loc.Es(clave);
            loc.ingles = Loc.En(clave);
            loc.Refrescar();
            return t;
        }

        public Text TextoPlantilla(Transform padre, string nombre, string clave, string ejemplo, int tamano, TextAnchor alineacion,
            Color color, Action<RectTransform> ubicar)
        {
            var t = Texto(padre, nombre, ejemplo, tamano, alineacion, color, ubicar);
            if (t.GetComponent<TextoLoc>() == null)
            {
                var loc = t.gameObject.AddComponent<TextoLoc>();
                loc.esPlantilla = true;
                loc.espanol = Loc.Es(clave);
                loc.ingles = Loc.En(clave);
            }
            return t;
        }

        public UIFactory.Barra Barra(Transform padre, string nombre, Color color, Action<RectTransform> ubicar)
        {
            var raiz = Buscar<RectTransform>(padre, nombre);
            if (raiz != null)
            {
                var relleno = raiz.Find("Area/Relleno") as RectTransform;
                return new UIFactory.Barra
                {
                    raiz = raiz,
                    relleno = relleno,
                    imagenRelleno = relleno != null ? relleno.GetComponent<Image>() : null
                };
            }
            var b = UIFactory.CrearBarra(padre, color, vis);
            b.raiz.name = nombre;
            ubicar?.Invoke(b.raiz);
            return b;
        }

        public static T Componente<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
