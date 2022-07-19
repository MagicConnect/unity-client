using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatGrid : MonoBehaviour
{
    [System.Serializable]
    public struct TileCoord
    {
        public TileCoord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public int x;
        public int y;
    }

    public List<int> entities = new List<int>();
    public List<CombatTile> tiles = new List<CombatTile>();
    public Dictionary<TileCoord, Vector2> tilePositions = new Dictionary<TileCoord, Vector2>();

    public void Awake()
    {
        foreach (CombatTile t in tiles)
        {
            Vector2 pos = t.transform.position;
            TileCoord tc = new TileCoord(t.x, t.y);
            tilePositions[tc] = pos;
        }
    }

    public Vector2 GetTilePosition(int x, int y)
    {
        return tilePositions[new TileCoord(x, y)];
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
