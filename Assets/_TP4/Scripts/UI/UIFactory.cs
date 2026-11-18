using UnityEngine;
using UnityEngine.UI;

namespace TP4
{
    /// <summary>Helpers para construir la UI por código (estilo ventanas azules de FF7).</summary>
    public static class UIFactory
    {
        static Sprite gradiente;
        static Font fuentePorDefecto;

        public static Font FuentePorDefecto
        {
            get
            {
                if (fuentePorDefecto == null)
                    fuentePorDefecto = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return fuentePorDefecto;
            }
        }

        public static RectTransform Rect(string nombre, Transform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);
            return rt;
        }

        /// <summary>Posiciona un rect anclado a una esquina. pos = esquina inferior izquierda relativa al ancla.</summary>
        public static RectTransform Ubicar(RectTransform rt, Vector2 ancla, Vector2 pivot, Vector2 pos, Vector2 tamano)
        {
            rt.anchorMin = rt.anchorMax = ancla;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamano;
            return rt;
        }

        public static RectTransform Estirar(RectTransform rt, float izq = 0, float der = 0, float abajo = 0, float arriba = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(izq, abajo);
            rt.offsetMax = new Vector2(-der, -arriba);
            return rt;
        }

        static Sprite Gradiente()
        {
            if (gradiente != null) return gradiente;
            gradiente = CrearGradiente(new Color(0.55f, 0.55f, 0.55f), Color.white);
            return gradiente;
        }

        static Sprite gradienteVentana;

        /// <summary>Textura vertical de degradé (la usa también el editor para guardarla como asset).</summary>
        public static Texture2D TexturaGradiente(Color abajo, Color arriba)
        {
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                tex.SetPixel(0, y, Color.Lerp(abajo, arriba, y / 63f));
            tex.Apply();
            return tex;
        }

        public static Color GradienteBarraAbajo => new Color(0.55f, 0.55f, 0.55f);

        static Sprite CrearGradiente(Color abajo, Color arriba)
        {
            var tex = TexturaGradiente(abajo, arriba);
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Agrega Canvas + CanvasScaler + GraphicRaycaster si el objeto no los tiene.</summary>
        public static RectTransform AsegurarCanvas(GameObject go, int orden)
        {
            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = orden;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                go.AddComponent<GraphicRaycaster>();
            }
            go.layer = 5;
            return (RectTransform)go.transform;
        }

        /// <summary>Ventana azul con degradé y borde claro.</summary>
        public static Image Ventana(string nombre, Transform padre, BattleVisuals vis)
        {
            var rt = Rect(nombre, padre);
            Color arriba = vis != null ? vis.colorPanelArriba : new Color(0.16f, 0.24f, 0.72f, 0.97f);
            Color abajo = vis != null ? vis.colorPanelAbajo : new Color(0.03f, 0.05f, 0.28f, 0.97f);
            Color borde = vis != null ? vis.colorBorde : new Color(0.85f, 0.88f, 0.95f);
            Sprite sprite = vis != null && vis.spriteVentana != null ? vis.spriteVentana : null;
            if (sprite == null && gradienteVentana == null) gradienteVentana = CrearGradiente(abajo, arriba);

            // Borde: imagen de fondo; el interior va 3px adentro.
            var bordeImg = rt.gameObject.AddComponent<Image>();
            bordeImg.color = borde;
            bordeImg.raycastTarget = false;

            var interior = Estirar(Rect("Interior", rt), 3, 3, 3, 3);
            var img = interior.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : gradienteVentana;
            img.color = Color.white;
            img.raycastTarget = false;
            return bordeImg;
        }

        public static Text Texto(Transform padre, string contenido, int tamano, TextAnchor alineacion, Color color, Font fuente = null)
        {
            var rt = Rect("Texto", padre);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = fuente != null ? fuente : FuentePorDefecto;
            t.text = contenido;
            t.fontSize = tamano;
            t.alignment = alineacion;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var sombra = rt.gameObject.AddComponent<Shadow>();
            sombra.effectColor = new Color(0, 0, 0, 0.8f);
            sombra.effectDistance = new Vector2(2, -2);
            return t;
        }

        public class Barra
        {
            public RectTransform raiz;
            public RectTransform relleno;
            public Image imagenRelleno;

            public void Set(float valor)
            {
                valor = Mathf.Clamp01(valor);
                relleno.anchorMax = new Vector2(valor, 1f);
            }
        }

        public static Barra CrearBarra(Transform padre, Color color, BattleVisuals vis = null)
        {
            var raiz = Rect("Barra", padre);
            var fondo = raiz.gameObject.AddComponent<Image>();
            fondo.color = new Color(0.05f, 0.05f, 0.12f, 0.9f);
            fondo.raycastTarget = false;
            var outline = raiz.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.7f, 0.72f, 0.8f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var area = Estirar(Rect("Area", raiz), 2, 2, 2, 2);
            var relleno = Rect("Relleno", area);
            relleno.anchorMin = Vector2.zero;
            relleno.anchorMax = new Vector2(1, 1);
            relleno.offsetMin = Vector2.zero;
            relleno.offsetMax = Vector2.zero;
            var img = relleno.gameObject.AddComponent<Image>();
            img.sprite = vis != null && vis.spriteRelleno != null ? vis.spriteRelleno : Gradiente();
            img.color = color;
            img.raycastTarget = false;
            return new Barra { raiz = raiz, relleno = relleno, imagenRelleno = img };
        }
    }
}
