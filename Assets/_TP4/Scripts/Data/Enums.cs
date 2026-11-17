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
        [InspectorName("Provocar")] Provocar
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
