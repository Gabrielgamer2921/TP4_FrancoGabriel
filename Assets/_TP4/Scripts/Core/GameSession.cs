using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    public static class GameSession
    {
        public static TournamentData Torneo { get; private set; }
        public static List<RobotData> Equipo { get; private set; } = new List<RobotData>();
        public static Dificultad Dificultad { get; private set; } = Dificultad.Normal;
        public static int PeleaActual { get; private set; }
        public static Inventory Inventario { get; private set; } = new Inventory();

        public static ModoATB ModoATB => Ajustes.ModoATB;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Terminar();

        public static bool Iniciada => Torneo != null && Equipo.Count > 0;

        public static EncounterData PeleaData =>
            Torneo != null && PeleaActual < Torneo.peleas.Count ? Torneo.peleas[PeleaActual] : null;

        public static bool EsUltimaPelea => Torneo != null && PeleaActual >= Torneo.peleas.Count - 1;

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

        public static List<RobotData> EquipoPorDefecto(TournamentData torneo)
        {
            var equipo = new List<RobotData>();
            for (int i = 0; i < torneo.plantel.Count && equipo.Count < torneo.tamanoEquipo; i++)
                if (torneo.plantel[i] != null) equipo.Add(torneo.plantel[i]);
            return equipo;
        }

        public static void AsegurarIniciada(TournamentData torneo)
        {
            if (Iniciada && Torneo == torneo) return;
            NuevoTorneo(torneo, EquipoPorDefecto(torneo), Dificultad.Normal);
        }

        public static void SaltarAPelea(int indice)
        {
            if (Torneo == null) return;
            PeleaActual = Mathf.Clamp(indice, 0, Torneo.peleas.Count - 1);
        }

        public static bool AvanzarPelea()
        {
            PeleaActual++;
            return Torneo != null && PeleaActual < Torneo.peleas.Count;
        }

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
