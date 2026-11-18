using System.Collections.Generic;
using UnityEngine;

namespace TP4
{
    /// <summary>
    /// Traducciones de la interfaz (español / inglés).
    /// Los textos de los datos (habilidades, ítems, etc.) tienen sus propios campos "EN" en cada ScriptableObject.
    /// </summary>
    public static class Loc
    {
        static readonly Dictionary<string, string[]> tabla = new Dictionary<string, string[]>
        {
            // ---------- Menú principal ----------
            { "titulo", new[] { "TORNEO DE MECHAS", "MECHA TOURNAMENT" } },
            { "subtitulo", new[] { "Combate por turnos ATB", "ATB turn-based combat" } },
            { "menu.nueva", new[] { "Nueva partida", "New game" } },
            { "menu.nueva.ayuda", new[] { "Empezar el torneo desde la primera ronda.", "Start the tournament from the first round." } },
            { "menu.opciones", new[] { "Opciones", "Options" } },
            { "menu.opciones.ayuda", new[] { "ATB, volumen e idioma.", "ATB, volume and language." } },
            { "menu.salir", new[] { "Salir", "Quit" } },
            { "menu.salir.ayuda", new[] { "Cerrar el juego.", "Close the game." } },
            { "menu.volver", new[] { "Volver", "Back" } },
            { "menu.controles", new[] { "Flechas/WASD o stick: mover   ·   Enter/Espacio o A: confirmar   ·   Esc o B: volver",
                                         "Arrows/WASD or stick: move   ·   Enter/Space or A: confirm   ·   Esc or B: back" } },

            // ---------- Dificultad ----------
            { "dif.titulo", new[] { "Elegí la dificultad", "Choose difficulty" } },
            { "dif.Facil", new[] { "Fácil", "Easy" } },
            { "dif.Normal", new[] { "Normal", "Normal" } },
            { "dif.Dificil", new[] { "Difícil", "Hard" } },
            { "dif.Mecha", new[] { "Mecha", "Mecha" } },
            { "dif.Facil.ayuda", new[] { "Para jugadores poco experimentados en combates por turnos.", "For players with little experience in turn-based combat." } },
            { "dif.Normal.ayuda", new[] { "Una experiencia balanceada para el combate por turnos.", "A balanced turn-based combat experience." } },
            { "dif.Dificil.ayuda", new[] { "Para aquellos jugadores que busquen un desafío más intenso.", "For players looking for a more intense challenge." } },
            { "dif.Mecha.ayuda", new[] { "El desafío final para aquellos que desean un verdadero reto.", "The ultimate challenge for those who want a true test." } },

            // ---------- Opciones ----------
            { "op.titulo", new[] { "Opciones", "Options" } },
            { "op.atb", new[] { "ATB activa", "Active ATB" } },
            { "op.atb.ayuda", new[] { "Activada: combate en tiempo real con barras ATB. Desactivada: por turnos clásicos (primero tu equipo, después los rivales).",
                                      "On: real-time combat with ATB gauges. Off: classic turns (your team first, then the rivals)." } },
            { "op.atb.proxima", new[] { "Se aplica desde la próxima pelea.", "Takes effect from the next battle." } },
            { "op.si", new[] { "Activada", "On" } },
            { "op.no", new[] { "Desactivada", "Off" } },
            { "op.volumen", new[] { "Volumen", "Volume" } },
            { "op.volumen.ayuda", new[] { "Volumen general del juego. Izquierda/derecha para ajustar.", "Master volume. Left/right to adjust." } },
            { "op.idioma", new[] { "Idioma", "Language" } },
            { "op.idioma.ayuda", new[] { "Idioma de los textos.", "Text language." } },
            { "op.idioma.valor", new[] { "Español", "English" } },

            // ---------- Pausa ----------
            { "pausa.titulo", new[] { "PAUSA", "PAUSED" } },
            { "pausa.continuar", new[] { "Continuar", "Resume" } },
            { "pausa.abandonar", new[] { "Abandonar torneo", "Quit tournament" } },
            { "pausa.abandonar.ayuda", new[] { "Volver al menú principal. Se pierde todo el progreso.", "Return to the main menu. All progress is lost." } },
            { "pausa.confirmar", new[] { "¿Abandonar? Se pierde el progreso.", "Quit? Progress will be lost." } },
            { "pausa.si", new[] { "Sí, abandonar", "Yes, quit" } },
            { "pausa.no", new[] { "No", "No" } },
            { "pausa.hint", new[] { "Pausa: Esc / P / Start", "Pause: Esc / P / Start" } },

            // ---------- Combate: paneles ----------
            { "ui.nombre", new[] { "NOMBRE", "NAME" } },
            { "ui.vida", new[] { "VIDA", "HP" } },
            { "ui.energia", new[] { "ENERGÍA", "ENERGY" } },
            { "ui.overdrive", new[] { "OVERDRIVE", "OVERDRIVE" } },
            { "ui.atb", new[] { "ATB", "ATB" } },
            { "ui.atb.activa", new[] { "ATB: ACTIVA", "ATB: ACTIVE" } },
            { "ui.atb.espera", new[] { "ATB: ESPERA", "ATB: WAIT" } },
            { "ui.turno", new[] { "TURNO", "TURN" } },
            { "ui.modoTurnos", new[] { "COMBATE POR TURNOS", "TURN-BASED COMBAT" } },
            { "ui.fase.jugador", new[] { "Fase: tu equipo", "Phase: your team" } },
            { "ui.fase.enemigo", new[] { "Fase: rivales", "Phase: rivals" } },
            { "turno.jugador", new[] { "TU TURNO", "YOUR TURN" } },
            { "turno.enemigo", new[] { "TURNO RIVAL", "ENEMY TURN" } },
            { "msg.pierdeTurno", new[] { "¡Paralizado!", "Paralyzed!" } },
            { "msg.accionExtra", new[] { "¡Acción extra!", "Extra action!" } },
            { "ui.ronda", new[] { "Ronda {0}/{1} - {3}", "Round {0}/{1} - {3}" } },
            { "ui.turnoDe", new[] { "Turno: {0}", "Turn: {0}" } },

            // ---------- Combate: comandos ----------
            { "cmd.atacar", new[] { "Atacar", "Attack" } },
            { "cmd.overdrive", new[] { "OVERDRIVE", "OVERDRIVE" } },
            { "cmd.modulos", new[] { "Módulos", "Modules" } },
            { "cmd.modulos.ayuda", new[] { "Habilidades que consumen energía.", "Skills that consume energy." } },
            { "cmd.items", new[] { "Ítems", "Items" } },
            { "cmd.items.ayuda", new[] { "Inventario compartido del equipo.", "Team's shared inventory." } },
            { "cmd.cambiar", new[] { "Cambiar ►", "Switch ►" } },
            { "cmd.cambiar.ayuda", new[] { "Pasar el turno a otro robot listo.", "Pass the turn to another ready robot." } },
            { "cmd.liberar", new[] { "Liberar", "Free" } },
            { "cmd.liberar.ayuda", new[] { "Liberar a un aliado atrapado en la red eléctrica. Gasta el turno.", "Free an ally caught in the electric net. Uses up the turn." } },
            { "msg.liberado", new[] { "¡Liberado!", "Freed!" } },
            { "msg.redBanner", new[] { "¡{0} quedó atrapado en la Red eléctrica!\n<size=32>No puede actuar. Otro robot puede usar \"Liberar\" (gasta su turno).</size>",
                                       "{0} is caught in the Electric net!\n<size=32>It can't act. Another robot can use \"Free\" (uses up its turn).</size>" } },
            { "msg.atrapado", new[] { "¡Atrapado!", "Trapped!" } },
            { "cmd.lento", new[] { "(lento)", "(slow)" } },
            { "cmd.rapido", new[] { "(rápido)", "(fast)" } },
            { "obj.enemigos", new[] { "Todos los enemigos", "All enemies" } },
            { "obj.equipo", new[] { "Todo el equipo", "Whole team" } },
            { "obj.provoca", new[] { "PROVOCA", "TAUNTING" } },
            { "est.prov", new[] { "PROV", "TAUNT" } },
            { "est.ko", new[] { "KO", "KO" } },

            // ---------- Combate: mensajes ----------
            { "msg.overdrive", new[] { "¡OVERDRIVE!", "OVERDRIVE!" } },
            { "msg.purgado", new[] { "Purgado", "Cleansed" } },
            { "msg.provoca", new[] { "¡Provoca!", "Taunt!" } },
            { "msg.peleaFinal", new[] { "¡PELEA FINAL!", "FINAL BATTLE!" } },
            { "msg.victoria", new[] { "¡VICTORIA!", "VICTORY!" } },
            { "msg.campeon", new[] { "¡CAMPEÓN!", "CHAMPION!" } },
            { "msg.campeonTorneo", new[] { "¡CAMPEÓN DEL TORNEO!", "TOURNAMENT CHAMPION!" } },
            { "msg.derrota", new[] { "DERROTA", "DEFEAT" } },
            { "msg.derrota.sub", new[] { "Tu equipo fue destruido. El torneo vuelve a empezar.", "Your team was destroyed. The tournament starts over." } },
            { "fin.siguiente", new[] { "Siguiente pelea", "Next battle" } },
            { "fin.menu", new[] { "Menú principal", "Main menu" } },

            // ---------- Stats (abreviaturas) ----------
            { "stat.Fuerza", new[] { "FUE", "STR" } },
            { "stat.Agilidad", new[] { "AGI", "AGI" } },
            { "stat.Vitalidad", new[] { "VIT", "VIT" } },
            { "stat.Magia", new[] { "MAG", "MAG" } },
            { "stat.Defensa", new[] { "DEF", "DEF" } },
        };

        public static string Es(string clave) => tabla.TryGetValue(clave, out var v) ? v[0] : clave;
        public static string En(string clave) => tabla.TryGetValue(clave, out var v) ? v[1] : clave;

        public static string T(string clave)
        {
            if (tabla.TryGetValue(clave, out var v)) return v[(int)Ajustes.Idioma];
            Debug.LogWarning("[TP4] Falta traducción: " + clave);
            return clave;
        }

        public static string T(string clave, params object[] args) => string.Format(T(clave), args);

        /// <summary>Devuelve el texto en inglés si corresponde y existe; si no, el español.</summary>
        public static string Elegir(string es, string en) => Ajustes.Ingles && !string.IsNullOrEmpty(en) ? en : es;

        public static string Stat(StatType s) => T("stat." + s);

        public static string NombreDificultad(Dificultad d) => T("dif." + d);
    }
}
