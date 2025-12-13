using System.Collections.Generic;
using RedBjorn.ProtoTiles.Example;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDataSO", menuName = "TurnBased/PlayerDataSO", order = 1)]
public class PlayerDataSO :  ScriptableObject
{
    public List<UnitMove> AvalailableUnits = new List<UnitMove>();
}
