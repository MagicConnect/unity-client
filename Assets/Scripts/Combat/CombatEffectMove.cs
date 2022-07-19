using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatEffectMove : CombatEffect
{
    public CombatEntity movingEntity;
    public int fromX, fromY;
    public int toX, toY;
    public float progress;

    private float speed = 2f;

    public CombatEffectMove(CombatEntity ce, int tx, int ty)
    {
        movingEntity = ce;
        toX = tx;
        toY = ty;
    }

    public override void StartEffect(CombatAction source)
    {
        base.StartEffect(source);
        fromX = movingEntity.tileX;
        fromY = movingEntity.tileY;
    }

    public override void UpdateEffect()
    {
        CombatGrid grid = movingEntity.friendly ? CombatSession.playerGrid : CombatSession.opponentGrid;
        Vector2 fromPos = grid.GetTilePosition(fromX, fromY);
        Vector2 toPos = grid.GetTilePosition(toX, toY);
        float distance = Vector2.Distance(fromPos, toPos);

        progress += (speed * Time.deltaTime) / distance;

        movingEntity.transform.position = Vector2.Lerp(fromPos, toPos, progress);

        if (progress >= 1f)
        {
            movingEntity.tileX = toX;
            movingEntity.tileY = toY;
            EndEffect();
        }
    }
}
