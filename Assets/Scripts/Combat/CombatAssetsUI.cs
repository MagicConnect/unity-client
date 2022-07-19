using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatAssetsUI : MonoBehaviour
{
    public static CombatAssetsUI instance;

    // References
    public GameObject refPartyList;
    public GameObject refRoundEntryList;
    public CombatGrid playerGrid;
    public CombatGrid opponentGrid;
    
    // Prefabs
    public GameObject prefabRoundEntryFriendly;
    public GameObject prefabRoundEntryOpponent;

    public void Awake()
    {
        instance = this;

        CombatSession.playerGrid = playerGrid;
        CombatSession.opponentGrid = opponentGrid;
    }
}
