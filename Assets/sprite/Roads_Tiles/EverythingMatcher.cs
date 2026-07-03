using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "2D/Tiles/EverythingMatcher")]
public class EverythingMatcher : RuleTile<EverythingMatcher.Neighbor>
{ // Указываем свой вложенный класс
    public class Neighbor : RuleTile.TilingRule.Neighbor { } // Наследуем от стандартного

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        switch (neighbor)
        {
            case Neighbor.This: return tile != null;     // Соединяем с чем угодно
            case Neighbor.NotThis: return tile == null; // Пустота — это пустота
        }
        return base.RuleMatch(neighbor, tile);
    }
}