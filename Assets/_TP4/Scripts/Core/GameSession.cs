using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    /// <summary>
    /// Estado del torneo en curso: equipo, dificultad, pelea actual e inventario.
    /// Vive solo en memoria: no hay guardado. Si el jugador pierde o abandona, se reinicia todo.
    /// </summary>
    public static class GameSession
    {
        public static TournamentData Torneo { get; private set; }
        public static List<RobotData> Equipo { get; private set; } = new List<RobotData>();
        public static Dificultad Dificultad { get; private set; } = Dificultad.Normal;
        public static int PeleaActual { get; private set; }
        public static Inventory Inventario { get; private set; } = new Inventory();

        /// <summary>El modo de ATB sale de las Opciones (ATB activa / desactivada).</summary>
        public static ModoATB ModoATB => Ajustes.ModoATB;

        // Resetea al entrar a Play aunque el "Domain Reload" esté desactivado.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Terminar();

        public static bool Iniciada => Torneo != null && Equipo.Count > 0;

        public static EncounterData PeleaData =>
            Torneo != null && PeleaActual < Torneo.peleas.Count ? Torneo.peleas[PeleaActual] : null;

        public static bool EsUltimaPelea => Torneo != null && PeleaActual >= Torneo.peleas.Count - 1;

        /// <summary>Multiplicador de stats de los rivales según la dificultad.</summary>
        public static float MultiplicadorRivales
        {
            get
            {
                switch (Dificultad)
                {
                    case Dificultad.Facil: return 0.75f;
                    case Dificultad.Dificil: return 1.25f;
                    case Dificultad.Mecha: return 1.5f;
                    default: return 1f;
                }
            }
        }

        public static void NuevoTorneo(TournamentData torneo, List<RobotData> equipo, Dificultad dificultad)
        {
            Torneo = torneo;
            Equipo = new List<RobotData>(equipo);
            Dificultad = dificultad;
            PeleaActual = 0;
            Inventario = new Inventory();
            foreach (var s in torneo.inventarioInicial)
                if (s.item != null) Inventario.Agregar(s.item, s.cantidad);
        }

        /// <summary>Equipo por defecto: los primeros N del plantel.</summary>
        public static List<RobotData> EquipoPorDefecto(TournamentData torneo)
        {
            var equipo = new List<RobotData>();
            for (int i = 0; i < torneo.plantel.Count && equipo.Count < torneo.tamanoEquipo; i++)
                if (torneo.plantel[i] != null) equipo.Add(torneo.plantel[i]);
            return equipo;
        }

        /// <summary>Si se entra directo a la escena de combate (para testear), arma una partida Normal por defecto.</summary>
        public static void AsegurarIniciada(TournamentData torneo)
        {
            if (Iniciada && Torneo == torneo) return;
            NuevoTorneo(torneo, EquipoPorDefecto(torneo), Dificultad.Normal);
        }

        /// <summary>Pasa a la siguiente pelea. Devuelve false si el torneo terminó.</summary>
        public static bool AvanzarPelea()
        {
            PeleaActual++;
            return Torneo != null && PeleaActual < Torneo.peleas.Count;
        }

        /// <summary>Borra todo el progreso (derrota, abandono o fin del torneo).</summary>
        public static void Terminar()
        {
            Torneo = null;
            Equipo = new List<RobotData>();
            Dificultad = Dificultad.Normal;
            PeleaActual = 0;
            Inventario = new Inventory();
        }
    }
}
