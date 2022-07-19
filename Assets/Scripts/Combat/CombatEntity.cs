using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatEntity : MonoBehaviour
{
    // Entites can be characters as well as miscellaneous things like fire
    public int id;

    public bool alive = true;
    public bool active = false; // false: spent its turn / not its turn / stunned
    public bool friendly; // helps build the round order cards

    // How thicc is the entity
    public int tilesWidth = 1;
    public int tilesHeight = 1;

    // Entity location on grid (top-left anchored)
    public int tileX;
    public int tileY;

    public enum EntityState
    {
        idle, standing, moving, attacking, defending, casting, dead
    }

    // Start is called before the first frame update
    void Start()
    {
        // TEMP
        CombatSession.entities.Add(id,this);
        if (friendly)
            CombatSession.playerGrid.entities.Add(id);
        else
            CombatSession.opponentGrid.entities.Add(id);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
