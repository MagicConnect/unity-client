using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatInteraction : MonoBehaviour
{
    public void Attack(CombatEntity entity)
    {
        CombatSession.ConfirmActionForCurrentEntity(1);
    }

    public void Defend(CombatEntity entity)
    {
        CombatSession.ConfirmActionForCurrentEntity(2);
    }

    public void Move(int toTileX, int toTileY)
    {
        CombatEntity entity = CombatSession.entities[CombatSession.currentEntityIDTurn];
        CombatAction action = new CombatAction();
        action.steps.Add(new CombatEffectMove(entity, toTileX, toTileY));
        CombatSession.queuedActions.Add(action);
        //CombatSession.ConfirmActionForCurrentEntity(3);
    }

    public void Update()
    {
        CombatSession.UpdateSession();


    }
}
