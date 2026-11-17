using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    /// <summary>Cámara de combate: leve balanceo, sacudidas al recibir golpes y tomas cinemáticas.</summary>
    [RequireComponent(typeof(Camera))]
    public class BattleCamera : MonoBehaviour
    {
        [SerializeField] float balanceo = 0.08f;

        Vector3 posBase;
        Quaternion rotBase;
        Vector3 pos;
        Quaternion rot;
        float fovBase;
        Camera cam;

        float shakeRestante;
        float shakeMagnitud;
        bool enCinematica;

        public bool EnCinematica => enCinematica;

        void Awake()
        {
            cam = GetComponent<Camera>();
            posBase = pos = transform.position;
            rotBase = rot = transform.rotation;
            fovBase = cam.fieldOfView;
        }

        public void Shake(float magnitud = 0.15f, float duracion = 0.2f)
        {
            shakeMagnitud = Mathf.Max(shakeMagnitud, magnitud);
            shakeRestante = Mathf.Max(shakeRestante, duracion);
        }

        void LateUpdate()
        {
            Vector3 offset = Vector3.zero;
            if (!enCinematica)
            {
                pos = posBase;
                rot = rotBase;
                offset += new Vector3(Mathf.Sin(Time.time * 0.4f), Mathf.Sin(Time.time * 0.27f) * 0.5f, 0f) * balanceo;
            }
            if (shakeRestante > 0f)
            {
                shakeRestante -= Time.deltaTime;
                offset += Random.insideUnitSphere * shakeMagnitud;
                if (shakeRestante <= 0f) shakeMagnitud = 0f;
            }
            transform.SetPositionAndRotation(pos + offset, rot);
        }

        /// <summary>
        /// Toma cinemática: órbita cerca del que actúa y luego plano amplio hacia los objetivos.
        /// </summary>
        public IEnumerator Cinematica(BattleUnit actor, List<BattleUnit> objetivos, float duracion)
        {
            enCinematica = true;
            Vector3 centro = actor.View.Centro;
            float radio = 2.2f + actor.View.Altura * 0.6f;
            Vector3 frente = actor.transform.forward;

            // Toma 1: órbita alrededor del actor
            float t1 = duracion * 0.6f;
            float anguloInicio = -70f, anguloFin = 50f;
            for (float t = 0; t < t1; t += Time.deltaTime)
            {
                float k = t / t1;
                float ang = Mathf.Lerp(anguloInicio, anguloFin, Mathf.SmoothStep(0, 1, k));
                Vector3 dir = Quaternion.Euler(0, ang, 0) * frente;
                pos = centro + dir * radio + Vector3.up * Mathf.Lerp(-0.2f, 0.6f, k);
                rot = Quaternion.LookRotation(centro - pos);
                cam.fieldOfView = Mathf.Lerp(fovBase, fovBase * 0.75f, k);
                yield return null;
            }

            // Toma 2: detrás del actor mirando a los objetivos
            Vector3 foco = centro;
            if (objetivos.Count > 0)
            {
                foco = Vector3.zero;
                foreach (var o in objetivos) foco += o.View.Centro;
                foco /= objetivos.Count;
            }
            Vector3 atras = actor.transform.position - (foco - actor.transform.position).normalized * 3.5f + Vector3.up * (actor.View.Altura + 1.2f);
            Vector3 p0 = pos;
            Quaternion r0 = rot;
            float t2 = duracion * 0.4f;
            for (float t = 0; t < t2; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / t2);
                pos = Vector3.Lerp(p0, atras, k);
                rot = Quaternion.Slerp(r0, Quaternion.LookRotation(foco - atras), k);
                cam.fieldOfView = Mathf.Lerp(fovBase * 0.75f, fovBase * 1.05f, k);
                yield return null;
            }
        }

        public IEnumerator Restaurar(float duracion = 0.45f)
        {
            Vector3 p0 = pos;
            Quaternion r0 = rot;
            float f0 = cam.fieldOfView;
            for (float t = 0; t < duracion; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / duracion);
                pos = Vector3.Lerp(p0, posBase, k);
                rot = Quaternion.Slerp(r0, rotBase, k);
                cam.fieldOfView = Mathf.Lerp(f0, fovBase, k);
                yield return null;
            }
            cam.fieldOfView = fovBase;
            enCinematica = false;
        }
    }
}
