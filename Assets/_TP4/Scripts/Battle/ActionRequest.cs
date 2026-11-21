using System.Collections.Generic;

namespace TP4
{
    public class ActionRequest
    {
        public BattleUnit actor;
        public ActionData accion;
        public List<BattleUnit> objetivos = new List<BattleUnit>();

        public ItemData Item => accion as ItemData;
        public AbilityData Habilidad => accion as AbilityData;

        public ActionRequest(BattleUnit actor, ActionData accion, List<BattleUnit> objetivos)
        {
            this.actor = actor;
            this.accion = accion;
            this.objetivos = objetivos ?? new List<BattleUnit>();
        }
    }
}
