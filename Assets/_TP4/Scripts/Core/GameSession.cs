using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    /// <summary>
    /// Estado del torneo que sobrevive entre escenas: equipo elegido, pelea actual e inventario.
    /// </summary>
    public static class GameSession
    {
        public static TournamentData Torneo { get; private set; }
        public static List<RobotData> Equipo { get; private set; } = new List<RobotData>();
        public static int PeleaActual { get; private set; }
        public static Inventory Inventario { get; private set; } = new Inventory();

        static Dictionary<ItemData, int> snapshotInicioPelea;

        // Resetea al entrar a Play aunque el "Domain Reload" esté desactivado.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Torneo = null;
            Equipo = new List<RobotData>();
            PeleaActual = 0;
            Inventario = new Inventory();
            snapshotInicioPelea = null;
        }

        public static bool Iniciada => Torneo != null && Equipo.Count > 0;

        public static EncounterData PeleaData =>
            Torneo != null && PeleaActual < Torneo.peleas.Count ? Torneo.peleas[PeleaActual] : null;

        public static bool EsUltimaPelea => Torneo != null && PeleaActual >= Torneo.peleas.Count - 1;

        public static void NuevoTorneo(TournamentData torneo, List<RobotData> equipo)
        {
            Torneo = torneo;
            Equipo = new List<RobotData>(equipo);
            PeleaActual = 0;
            Inventario = new Inventory();
            foreach (var s in torneo.inventarioInicial)
                if (s.item != null) Inventario.Agregar(s.item, s.cantidad);
            snapshotInicioPelea = null;
        }

        /// <summary>Si se entra directo a la escena de combate (para testear), arma un equipo por defecto.</summary>
        public static void AsegurarIniciada(TournamentData torneo)
        {
            if (Iniciada && Torneo == torneo) return;
            var equipo = new List<RobotData>();
            for (int i = 0; i < torneo.plantel.Count && equipo.Count < torneo.tamanoEquipo; i++)
                if (torneo.plantel[i] != null) equipo.Add(torneo.plantel[i]);
            NuevoTorneo(torneo, equipo);
        }

        public static void GuardarInicioPelea() => snapshotInicioPelea = Inventario.Snapshot();

        /// <summary>Al reintentar una pelea se recuperan los ítems gastados en el intento fallido.</summary>
        public static void RestaurarInicioPelea()
        {
            if (snapshotInicioPelea != null) Inventario.Restaurar(snapshotInicioPelea);
        }

        /// <summary>Pasa a la siguiente pelea. Devuelve false si el torneo terminó.</summary>
        public static bool AvanzarPelea()
        {
            PeleaActual++;
            snapshotInicioPelea = null;
            return Torneo != null && PeleaActual < Torneo.peleas.Count;
        }
    }
}
