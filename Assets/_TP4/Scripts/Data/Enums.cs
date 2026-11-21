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

    public enum ModoATB
    {
        [InspectorName("Activo (el tiempo nunca se detiene)")] Activo,
        [InspectorName("Espera (se detiene en submenús y animaciones)")] Espera,
        [InspectorName("Clásico (se detiene mientras tengas un robot listo)")] Clasico
    }

    public enum Dificultad
    {
        Facil,
        Normal,
        Dificil,
        Mecha
    }

    public enum ActionAnim
    {
        Ataque,
        Disparo,
        Habilidad,
        Item
    }
}
