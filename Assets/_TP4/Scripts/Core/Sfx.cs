using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    public enum SfxId
    {
        Mover, Confirmar, Cancelar, Listo,
        Golpe, Disparo, Curar, Estado, Muerte, Victoria, Derrota, Overdrive
    }

    /// <summary>
    /// Reproductor de efectos de sonido. Si un clip no está asignado en <see cref="BattleVisuals"/>,
    /// genera un sonido procedural para que el feedback sonoro exista desde el día 1.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }

        BattleVisuals visuals;
        AudioSource source;
        AudioSource musica;
        readonly Dictionary<SfxId, AudioClip> procedurales = new Dictionary<SfxId, AudioClip>();

        public static Sfx Crear(BattleVisuals visuals)
        {
            var go = new GameObject("SFX");
            var sfx = go.AddComponent<Sfx>();
            sfx.visuals = visuals;
            sfx.source = go.AddComponent<AudioSource>();
            sfx.source.playOnAwake = false;
            sfx.musica = go.AddComponent<AudioSource>();
            sfx.musica.loop = true;
            sfx.musica.playOnAwake = false;
            Instance = sfx;
            return sfx;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Play(SfxId id, float volumen = 1f, float pitch = 1f)
        {
            if (Instance == null) return;
            Instance.PlayInterno(Instance.Clip(id), volumen, pitch);
        }

        public static void Play(AudioClip clip, SfxId fallback, float volumen = 1f)
        {
            if (Instance == null) return;
            Instance.PlayInterno(clip != null ? clip : Instance.Clip(fallback), volumen, Random.Range(0.95f, 1.05f));
        }

        public static void Musica(AudioClip clip)
        {
            if (Instance == null || clip == null) return;
            Instance.musica.clip = clip;
            Instance.musica.volume = Instance.visuals != null ? Instance.visuals.volumenMusica : 0.5f;
            Instance.musica.Play();
        }

        public static void PararMusica()
        {
            if (Instance != null) Instance.musica.Stop();
        }

        void PlayInterno(AudioClip clip, float volumen, float pitch)
        {
            if (clip == null) return;
            source.pitch = pitch;
            source.PlayOneShot(clip, volumen);
        }

        AudioClip Clip(SfxId id)
        {
            AudioClip asignado = null;
            if (visuals != null)
            {
                switch (id)
                {
                    case SfxId.Mover: asignado = visuals.sfxMover; break;
                    case SfxId.Confirmar: asignado = visuals.sfxConfirmar; break;
                    case SfxId.Cancelar: asignado = visuals.sfxCancelar; break;
                    case SfxId.Listo: asignado = visuals.sfxListo; break;
                    case SfxId.Golpe: asignado = visuals.sfxGolpe; break;
                    case SfxId.Disparo: asignado = visuals.sfxDisparo; break;
                    case SfxId.Curar: asignado = visuals.sfxCurar; break;
                    case SfxId.Estado: asignado = visuals.sfxEstado; break;
                    case SfxId.Muerte: asignado = visuals.sfxMuerte; break;
                    case SfxId.Victoria: asignado = visuals.sfxVictoria; break;
                    case SfxId.Derrota: asignado = visuals.sfxDerrota; break;
                    case SfxId.Overdrive: asignado = visuals.sfxOverdrive; break;
                }
            }
            if (asignado != null) return asignado;
            if (!procedurales.TryGetValue(id, out var clip))
            {
                clip = Generar(id);
                procedurales[id] = clip;
            }
            return clip;
        }

        // ---------- Sonidos procedurales ----------
        const int SampleRate = 44100;

        static AudioClip Generar(SfxId id)
        {
            switch (id)
            {
                case SfxId.Mover: return Tono("mover", 0.04f, 880f, 880f, 0.25f, Onda.Cuadrada);
                case SfxId.Confirmar: return Tono("confirmar", 0.08f, 660f, 1320f, 0.3f, Onda.Cuadrada);
                case SfxId.Cancelar: return Tono("cancelar", 0.09f, 500f, 250f, 0.3f, Onda.Cuadrada);
                case SfxId.Listo: return Tono("listo", 0.12f, 1046f, 1568f, 0.25f, Onda.Seno);
                case SfxId.Golpe: return Ruido("golpe", 0.18f, 0.9f, 180f);
                case SfxId.Disparo: return Tono("disparo", 0.2f, 1400f, 200f, 0.4f, Onda.Sierra);
                case SfxId.Curar: return Tono("curar", 0.35f, 520f, 1040f, 0.3f, Onda.Seno);
                case SfxId.Estado: return Tono("estado", 0.3f, 300f, 150f, 0.35f, Onda.Cuadrada);
                case SfxId.Muerte: return Ruido("muerte", 0.7f, 0.8f, 80f);
                case SfxId.Victoria: return Arpegio("victoria", new[] { 523f, 659f, 784f, 1046f }, 0.13f);
                case SfxId.Derrota: return Arpegio("derrota", new[] { 392f, 330f, 262f, 196f }, 0.2f);
                case SfxId.Overdrive: return Tono("overdrive", 0.6f, 200f, 1600f, 0.4f, Onda.Sierra);
                default: return Tono("bip", 0.05f, 800f, 800f, 0.2f, Onda.Seno);
            }
        }

        enum Onda { Seno, Cuadrada, Sierra }

        static float Muestra(Onda onda, float fase)
        {
            switch (onda)
            {
                case Onda.Cuadrada: return Mathf.Sin(fase) >= 0f ? 1f : -1f;
                case Onda.Sierra: return (fase / Mathf.PI) % 2f - 1f;
                default: return Mathf.Sin(fase);
            }
        }

        static AudioClip Tono(string nombre, float dur, float f0, float f1, float vol, Onda onda)
        {
            int n = Mathf.CeilToInt(dur * SampleRate);
            var data = new float[n];
            float fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = Mathf.Lerp(f0, f1, t);
                fase += 2f * Mathf.PI * f / SampleRate;
                float env = Mathf.Min(1f, t * 20f) * (1f - t);
                data[i] = Muestra(onda, fase) * env * vol;
            }
            return Crear(nombre, data);
        }

        static AudioClip Ruido(string nombre, float dur, float vol, float graves)
        {
            int n = Mathf.CeilToInt(dur * SampleRate);
            var data = new float[n];
            float prev = 0f;
            float fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float env = Mathf.Pow(1f - t, 2.5f);
                // ruido filtrado + un golpe grave
                prev = Mathf.Lerp(prev, Random.Range(-1f, 1f), 0.35f);
                fase += 2f * Mathf.PI * Mathf.Lerp(graves, graves * 0.4f, t) / SampleRate;
                data[i] = (prev * 0.6f + Mathf.Sin(fase) * 0.6f) * env * vol;
            }
            return Crear(nombre, data);
        }

        static AudioClip Arpegio(string nombre, float[] notas, float durNota)
        {
            int porNota = Mathf.CeilToInt(durNota * SampleRate);
            var data = new float[porNota * notas.Length + porNota * 2];
            for (int k = 0; k < notas.Length; k++)
            {
                float fase = 0f;
                int largo = k == notas.Length - 1 ? porNota * 3 : porNota;
                for (int i = 0; i < largo && k * porNota + i < data.Length; i++)
                {
                    float t = (float)i / largo;
                    fase += 2f * Mathf.PI * notas[k] / SampleRate;
                    data[k * porNota + i] += Muestra(Onda.Cuadrada, fase) * (1f - t) * 0.22f;
                }
            }
            return Crear(nombre, data);
        }

        static AudioClip Crear(string nombre, float[] data)
        {
            var clip = AudioClip.Create("sfx_" + nombre, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
