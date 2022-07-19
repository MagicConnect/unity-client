using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatSession
{
    // Handles the combat session
    public static CombatGrid playerGrid;
    public static CombatGrid opponentGrid;
    public static Dictionary<int, CombatEntity> entities = new Dictionary<int, CombatEntity>();

    public static List<int> turnOrder = new List<int>(); // list of entity IDs
    public static int currentEntityIDTurn; // who is going next
    public static List<GameObject> roundEntries = new List<GameObject>(); // mirror of turnOrder, referencing the ui element

    public static List<CombatAction> queuedActions = new List<CombatAction>();
    
    public static void UpdateSession()
    {
        if(queuedActions.Count > 0)
        {
            if(!queuedActions[0].started)
            {
                queuedActions[0].StartEffects();
            }
            else
            {
                queuedActions[0].UpdateAction();
            }
        }
    }

    public static void SetCurrentEntity(CombatEntity entity)
    {
        // highlight the current entity

        // update the ability images
        /*GameObject abilityIcon1 = CombatAssetsUI.instance.ability1;
        GameObject abilityIcon2 = CombatAssetsUI.instance.ability2;
        GameObject abilityIcon3 = CombatAssetsUI.instance.ability3;
        GameObject abilityIconSpecial = CombatAssetsUI.instance.abilitySpecial;*/
        // highlight the round entry?
        GameObject roundEntry = roundEntries[0];
    }
    
    public static bool IsEntityLocallyControlled(int id)
    {
        if (false) // TODO is multiplayer?
        {

        }
        else
        {
            return playerGrid.entities.Contains(id);
        }
        return false;
    }

    public static void ConfirmActionForCurrentEntity(int action)
    {
        CombatEntity entity = entities[currentEntityIDTurn];
        if (!IsEntityLocallyControlled(currentEntityIDTurn))
            return;
        // transform action into api-input
        SendInputToAPI(new string[] { }); // TODO
    }

    public static void ActionDone(CombatAction action)
    {
        queuedActions.Remove(action);
    }

    public static void UpdateRoundOrder()
    {
        for (int i = 0; i < turnOrder.Count; i++)
        {
            // TODO
        }
    }

    public static void BuildEntityOrder()
    {
        List<CombatEntity> eligibleEntities = new List<CombatEntity>();
        foreach (CombatEntity ce in CombatSession.entities.Values)
        {
            if (true) // alive & actionable
                eligibleEntities.Add(ce);
        }

        // Order them based on rules (initiative, but API determines it)
        List<CombatEntity> entityOrder = new List<CombatEntity>();
        for (int i = 0; i < eligibleEntities.Count; i++)
        {
            for (int j = 0; j < eligibleEntities.Count; j++)
            {
                //sorting algo
            }
        }

        // purge the existing list
        for(int i = roundEntries.Count - 1; i >= 0; i--)
        {
            GameObject go = roundEntries[i];
            roundEntries.Remove(go);
            GameObject.Destroy(go);
        }

        // create/delete order cards as necessary
        for (int i = 0; i < turnOrder.Count; i++)
        {
            GameObject go;
            if (entities[turnOrder[i]].friendly)
                go = GameObject.Instantiate(CombatAssetsUI.instance.prefabRoundEntryFriendly);
            else
                go = GameObject.Instantiate(CombatAssetsUI.instance.prefabRoundEntryOpponent);

            // Get the portrait of the entity
            // TODO go.GetComponent<Image>().image = 

            go.transform.SetParent(CombatAssetsUI.instance.refPartyList.transform);
        }
    }

    public static void InterpretAPI(string[] data)
    {
        // TODO enqueue combat actions etc
    }

    public static void SendInputToAPI(string[] data)
    {
        // TODO send input
    }
}
