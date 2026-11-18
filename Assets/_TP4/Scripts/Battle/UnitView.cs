using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    /// <summary>
    /// Parte visual de un robot: modelo (o cápsula placeholder), animaciones, flash al recibir daño,
    /// proyectiles y efectos de estado.
    /// Triggers de Animator que usa (todos opcionales): Attack, Shoot, Skill, Item, Hit, Die, Victory.
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        BattleUnit unit;
        BattleVisuals vis;
        Animator animator;
        Transform modelo;
        Renderer[] renderers;
        readonly HashSet<string> parametros = new HashSet<string>();
        MaterialPropertyBlock mpb;
        Vector3 posInicial;
        Quaternion rotInicial;
        Color? tinte;

        public float Altura { get; private set; } = 2f;
        public Vector3 Centro => transform.position + Vector3.up * Altura * 0.5f;
        public Vector3 Arriba => transform.position + Vector3.up * (Altura + 0.3f);

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public void Construir(BattleUnit unit, BattleVisuals vis)
        {
            this.unit = unit;
            this.vis = vis;
            var data = unit.Data;
            posInicial = transform.position;
            rotInicial = transform.rotation;
            mpb = new MaterialPropertyBlock();

            modelo = CrearModelo(data, transform, vis, out animator).transform;

            renderers = modelo.GetComponentsInChildren<Renderer>();
            CalcularAltura();

            if (animator != null && animator.runtimeAnimatorController != null)
                foreach (var p in animator.parameters) parametros.Add(p.name);

            unit.Damaged += (u, d) => StartCoroutine(Golpeado());
            unit.Died += u => StartCoroutine(Morir());
            unit.PhaseTriggered += (u, fase) => { tinte = fase.colorTinte; AplicarTinte(); StartCoroutine(Pulso(1.15f)); };
        }

        /// <summary>
        /// Instancia el modelo de un robot (o el placeholder de cubos si no tiene modelo).
        /// Lo usan el combate, el menú principal y la selección de equipo.
        /// </summary>
        public static GameObject CrearModelo(RobotData data, Transform padre, BattleVisuals vis, out Animator animator)
        {
            GameObject go;
            animator = null;
            if (data.modelo != null)
            {
                go = Instantiate(data.modelo, padre);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * data.escala;
                animator = go.GetComponentInChildren<Animator>();
                if (animator != null && data.animatorController != null)
                    animator.runtimeAnimatorController = data.animatorController;
            }
            else
            {
                go = CrearPlaceholder(data, padre, vis);
            }
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            return go;
        }

        static GameObject CrearPlaceholder(RobotData data, Transform padre, BattleVisuals vis)
        {
            var raiz = new GameObject("Placeholder");
            raiz.transform.SetParent(padre, false);
            raiz.transform.localScale = Vector3.one * data.escala;

            Material mat = CrearMaterial(vis != null ? vis.materialPlaceholder : null, data.colorPrincipal, false);
            Material oscuro = CrearMaterial(vis != null ? vis.materialPlaceholder : null, data.colorPrincipal * 0.45f, false);
            Material visor = CrearMaterial(vis != null ? vis.materialEmisivo : null, new Color(0.3f, 1f, 1f), true);

            // Un "mecha" simple: torso, piernas, hombros y visor.
            Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0, 1.35f, 0), new Vector3(0.9f, 0.8f, 0.7f), mat);
            Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(-0.25f, 0.5f, 0), new Vector3(0.28f, 1f, 0.35f), oscuro);
            Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0.25f, 0.5f, 0), new Vector3(0.28f, 1f, 0.35f), oscuro);
            Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(-0.62f, 1.55f, 0), new Vector3(0.35f, 0.35f, 0.5f), oscuro);
            Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0.62f, 1.55f, 0), new Vector3(0.35f, 0.35f, 0.5f), oscuro);
            Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0, 1.95f, 0.05f), new Vector3(0.5f, 0.35f, 0.45f), mat);
            Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0, 1.97f, 0.28f), new Vector3(0.4f, 0.1f, 0.05f), visor);
            // Cañón en el brazo derecho
            Pieza(raiz.transform, PrimitiveType.Cube, new Vector3(0.62f, 1.25f, 0.35f), new Vector3(0.16f, 0.16f, 0.7f), oscuro);
            return raiz;
        }

        static GameObject Pieza(Transform padre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static readonly Dictionary<string, Material> cacheMateriales = new Dictionary<string, Material>();

        /// <summary>Crea (o reutiliza) un material de color a partir de un material base.</summary>
        public static Material CrearMaterial(Material baseMat, Color color, bool emisivo)
        {
            string clave = (baseMat != null ? baseMat.GetHashCode().ToString() : "null") + "|" + emisivo + "|" + ColorUtility.ToHtmlStringRGBA(color);
            if (cacheMateriales.TryGetValue(clave, out var cacheado) && cacheado != null) return cacheado;
            Material m;
            if (baseMat != null) m = new Material(baseMat);
            else
            {
                var sh = Shader.Find(emisivo ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                m = new Material(sh);
            }
            m.color = color;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, color);
            if (m.HasProperty("_EmissionColor") && emisivo)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 2f);
            }
            cacheMateriales[clave] = m;
            return m;
        }

        void CalcularAltura()
        {
            if (renderers == null || renderers.Length == 0) return;
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            Altura = Mathf.Max(0.5f, b.max.y - transform.position.y);
        }

        // ---------- Animaciones ----------
        public bool Trigger(string nombre)
        {
            if (animator == null || !parametros.Contains(nombre)) return false;
            animator.SetTrigger(nombre);
            return true;
        }

        static string TriggerDe(ActionAnim a)
        {
            switch (a)
            {
                case ActionAnim.Disparo: return "Shoot";
                case ActionAnim.Habilidad: return "Skill";
                case ActionAnim.Item: return "Item";
                default: return "Attack";
            }
        }

        /// <summary>Todo lo que pasa antes del impacto: avanzar, animar, disparar.</summary>
        public IEnumerator Preparar(ActionData accion, List<BattleUnit> objetivos)
        {
            BattleUnit primero = objetivos.Count > 0 ? objetivos[0] : null;
            bool avanzar = accion.cuerpoACuerpo && primero != null && primero != unit && primero.Team != unit.Team;

            if (primero != null && primero != unit) MirarA(primero.transform.position);

            if (avanzar)
            {
                Vector3 dir = (transform.position - primero.transform.position);
                dir.y = 0;
                Vector3 destino = primero.transform.position + dir.normalized * (1.4f + 0.3f * primero.View.Altura);
                yield return Mover(destino, 0.22f);
            }

            bool animo = Trigger(TriggerDe(accion.animacion)) || Trigger("Attack");
            if (!animo) StartCoroutine(Pulso(1.08f));

            if (accion.vfxLanzamiento != null)
                Destroy(Instantiate(accion.vfxLanzamiento, Centro, transform.rotation), 3f);

            bool esDisparo = !accion.cuerpoACuerpo && accion.HaceDano && accion.ApuntaAEnemigos;
            Sfx.Play(accion.sfxLanzamiento, esDisparo ? SfxId.Disparo : SfxId.Confirmar, 0.8f);

            yield return new WaitForSeconds(avanzar ? 0.25f : 0.35f);

            if (esDisparo)
            {
                float dur = 0f;
                foreach (var t in objetivos)
                {
                    float d = Vector3.Distance(Centro, t.View.Centro) / 28f;
                    dur = Mathf.Max(dur, d);
                    StartCoroutine(Proyectil(t.View.Centro, accion.colorEfecto, d));
                }
                yield return new WaitForSeconds(dur);
            }
        }

        public IEnumerator Volver()
        {
            if ((transform.position - posInicial).sqrMagnitude > 0.01f)
                yield return Mover(posInicial, 0.25f);
            transform.rotation = rotInicial;
        }

        void MirarA(Vector3 p)
        {
            Vector3 d = p - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }

        IEnumerator Mover(Vector3 destino, float dur)
        {
            Vector3 desde = transform.position;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(desde, destino, Mathf.SmoothStep(0, 1, t / dur));
                yield return null;
            }
            transform.position = destino;
        }

        IEnumerator Proyectil(Vector3 destino, Color color, float dur)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(p.GetComponent<Collider>());
            p.transform.position = Centro + transform.forward * 0.6f;
            p.transform.localScale = Vector3.one * 0.3f;
            p.GetComponent<Renderer>().sharedMaterial = CrearMaterial(vis != null ? vis.materialEmisivo : null, color, true);
            var trail = p.AddComponent<TrailRenderer>();
            trail.time = 0.15f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0f;
            trail.sharedMaterial = CrearMaterial(vis != null ? vis.materialTrail : null, color, true);
            Vector3 desde = p.transform.position;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                p.transform.position = Vector3.Lerp(desde, destino, t / dur);
                yield return null;
            }
            Destroy(p);
        }

        /// <summary>Destello de impacto por defecto cuando la acción no tiene VFX propio.</summary>
        public void Impacto(ActionData accion)
        {
            if (accion != null && accion.vfxImpacto != null)
            {
                Destroy(Instantiate(accion.vfxImpacto, Centro, Quaternion.identity), 3f);
                return;
            }
            StartCoroutine(Destello(accion != null ? accion.colorEfecto : Color.white));
        }

        IEnumerator Destello(Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.transform.position = Centro;
            go.GetComponent<Renderer>().sharedMaterial = CrearMaterial(vis != null ? vis.materialEmisivo : null, color, true);
            const float dur = 0.25f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.6f, k) * (1f - k * 0.7f);
                yield return null;
            }
            Destroy(go);
        }

        // ---------- Reacciones ----------
        IEnumerator Golpeado()
        {
            if (!unit.IsAlive) yield break;
            Trigger("Hit");
            SetColorFlash(new Color(1f, 0.25f, 0.25f));
            Vector3 basePos = modelo.localPosition;
            for (float t = 0; t < 0.18f; t += Time.deltaTime)
            {
                modelo.localPosition = basePos + Random.insideUnitSphere * 0.08f;
                yield return null;
            }
            modelo.localPosition = basePos;
            ClearFlash();
        }

        IEnumerator Morir()
        {
            Sfx.Play(SfxId.Muerte, 0.8f);
            if (Trigger("Die")) yield break;
            // Sin animación: se cae y se oscurece.
            SetColorFlash(new Color(0.15f, 0.15f, 0.15f));
            Quaternion desde = modelo.localRotation;
            Quaternion hasta = desde * Quaternion.Euler(-80f, 0, 0);
            Vector3 p0 = modelo.localPosition;
            for (float t = 0; t < 0.6f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / 0.6f);
                modelo.localRotation = Quaternion.Slerp(desde, hasta, k);
                modelo.localPosition = p0 + Vector3.down * 0.3f * k;
                yield return null;
            }
        }

        IEnumerator Pulso(float escala)
        {
            Vector3 s0 = modelo.localScale;
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / 0.3f * Mathf.PI);
                modelo.localScale = s0 * Mathf.Lerp(1f, escala, k);
                yield return null;
            }
            modelo.localScale = s0;
        }

        public void Victoria()
        {
            if (!unit.IsAlive) return;
            if (!Trigger("Victory")) StartCoroutine(Pulso(1.12f));
        }

        void SetColorFlash(Color c)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, c);
                mpb.SetColor(ColorId, c);
                r.SetPropertyBlock(mpb);
            }
        }

        void ClearFlash()
        {
            if (tinte.HasValue) { AplicarTinte(); return; }
            foreach (var r in renderers)
            {
                if (r == null) continue;
                mpb.Clear();
                r.SetPropertyBlock(mpb);
            }
        }

        void AplicarTinte()
        {
            if (!tinte.HasValue) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var baseColor = r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId) ? r.sharedMaterial.GetColor(BaseColorId) : Color.white;
                var c = Color.Lerp(baseColor, tinte.Value, 0.45f);
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, c);
                mpb.SetColor(ColorId, c);
                r.SetPropertyBlock(mpb);
            }
        }

        // ---------- Estados ----------
        /// <summary>Jaula de barras eléctricas alrededor del robot (estados liberables).</summary>
        GameObject CrearJaula(StatusData estado)
        {
            var jaula = new GameObject("Jaula_" + estado.nombre);
            jaula.transform.SetParent(transform, false);
            var mat = CrearMaterial(vis != null ? vis.materialEmisivo : null, estado.color, true);
            float r = 0.85f * Mathf.Max(1f, unit.Data.escala);
            const int lados = 8;
            var puntos = new Vector3[lados];
            for (int i = 0; i < lados; i++)
            {
                float a = i * Mathf.PI * 2f / lados;
                puntos[i] = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                // Barras verticales
                var barra = Pieza(jaula.transform, PrimitiveType.Cube, puntos[i] + Vector3.up * Altura * 0.5f,
                    new Vector3(0.05f, Altura, 0.05f), mat);
                Destroy(barra.GetComponent<Collider>());
            }
            // Anillos horizontales
            foreach (float h in new[] { 0.15f, 0.55f, 0.95f })
            {
                for (int i = 0; i < lados; i++)
                {
                    Vector3 a = puntos[i] + Vector3.up * Altura * h;
                    Vector3 b = puntos[(i + 1) % lados] + Vector3.up * Altura * h;
                    var seg = Pieza(jaula.transform, PrimitiveType.Cube, (a + b) * 0.5f, new Vector3(0.04f, 0.04f, Vector3.Distance(a, b)), mat);
                    seg.transform.localRotation = Quaternion.LookRotation(b - a);
                    Destroy(seg.GetComponent<Collider>());
                }
            }
            jaula.AddComponent<Chisporroteo>();
            Sfx.Play(estado.sfxAplicar, SfxId.Estado, 0.9f);
            return jaula;
        }

        public GameObject CrearVfxEstado(StatusData estado)
        {
            if (estado.vfxLoop != null)
            {
                var v = Instantiate(estado.vfxLoop, transform);
                v.transform.localPosition = Vector3.up * Altura * 0.5f;
                return v;
            }
            if (estado.liberable) return CrearJaula(estado);

            // Anillo de color a los pies.
            var anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(anillo.GetComponent<Collider>());
            anillo.name = "Estado_" + estado.nombre;
            anillo.transform.SetParent(transform, false);
            anillo.transform.localPosition = Vector3.up * (0.03f + 0.02f * unit.Estados.Count);
            anillo.transform.localScale = new Vector3(1.6f, 0.01f, 1.6f) * Mathf.Max(1f, unit.Data.escala);
            anillo.GetComponent<Renderer>().sharedMaterial = CrearMaterial(vis != null ? vis.materialEmisivo : null, estado.color, true);
            anillo.AddComponent<Latido>();
            Sfx.Play(estado.sfxAplicar, SfxId.Estado, 0.7f);
            return anillo;
        }
    }

    /// <summary>Chisporroteo de la jaula de la Red eléctrica: gira y "parpadea" barras al azar.</summary>
    public class Chisporroteo : MonoBehaviour
    {
        float t;
        void Update()
        {
            transform.Rotate(0f, 40f * Time.deltaTime, 0f);
            t -= Time.deltaTime;
            if (t > 0f) return;
            t = 0.07f;
            foreach (Transform c in transform) c.gameObject.SetActive(Random.value > 0.18f);
        }
    }

    /// <summary>Escala que late suavemente (para los anillos de estado).</summary>
    public class Latido : MonoBehaviour
    {
        Vector3 s0;
        void Start() => s0 = transform.localScale;
        void Update()
        {
            float k = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
            transform.localScale = new Vector3(s0.x * k, s0.y, s0.z * k);
        }
    }
}
