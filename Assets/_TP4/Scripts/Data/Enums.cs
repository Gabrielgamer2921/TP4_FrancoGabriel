using UnityEngine;

namespace TP4
{
    public enum StatType
    {
        Fuerza,
        Agilidad,
        Vitalidad,
        Magia,
        Defensa
    }

    public enum Team
    {
        Jugador,
        Enemigo
    }

    public enum RobotRole
    {
        Tanque,
        Asalto,
        Artillero,
        Mecanico,
        Jefe
    }

    /// <summary>A quién puede apuntar una acción, relativo a quien la usa.</summary>
    public enum TargetMode
    {
        [InspectorName("Enemigo único")] EnemigoUnico,
        [InspectorName("Aliado único")] AliadoUnico,
        [InspectorName("Todos los enemigos")] TodosEnemigos,
        [InspectorName("Todos los aliados")] TodosAliados,
        [InspectorName("Uno mismo")] Propio
    }

    public enum EffectType
    {
        [InspectorName("Daño físico (Fuerza vs Defensa)")] DanoFisico,
        [InspectorName("Daño de energía (Magia vs ½ Defensa)")] DanoEnergia,
        [InspectorName("Daño fijo (ignora stats)")] DanoFijo,
        [InspectorName("Curar vida")] Curar,
        [InspectorName("Restaurar energía")] RestaurarEnergia,
        [InspectorName("Modificar stat (buff/debuff)")] ModificarStat,
        [InspectorName("Aplicar estado")] AplicarEstado,
        [InspectorName("Limpiar estados")] LimpiarEstados,
        [InspectorName("Modificar ATB")] ModificarATB,
        [InspectorName("Provocar")] Provocar,
        [InspectorName("Liberar (quita estados liberables)")] Liberar
    }

    /// <summary>
    /// Cómo corre el tiempo de la ATB (inspirado en las opciones de FF4–FF9).
    /// En todos los modos la barra avanza según la Agilidad; cambia cuándo se detiene.
    /// </summary>
    public enum ModoATB
    {
        [InspectorName("Activo (el tiempo nunca se detiene)")] Activo,
        [InspectorName("Espera (se detiene en submenús y animaciones)")] Espera,
        [InspectorName("Clásico (se detiene mientras tengas un robot listo)")] Clasico
    }

    /// <summary>Dificultad: escala las stats de los rivales.</summary>
    public enum Dificultad
    {
        Facil,
        Normal,
        Dificil,
        Mecha
    }

    /// <summary>Animación genérica que dispara una acción (triggers del Animator).</summary>
    public enum ActionAnim
    {
        Ataque,
        Disparo,
        Habilidad,
        Item
    }
}
