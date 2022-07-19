using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatTile : MonoBehaviour
{
    public int x;
    public int y;

    public void OnMouseUpAsButton()
    {
        GameObject.Find("Scripts").GetComponent<CombatInteraction>().Move(x, y);
    }
}
