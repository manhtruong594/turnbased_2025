using System.Collections.Generic;
using RedBjorn.ProtoTiles;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Command
{
    public class MoveCommand : ICommand
    {
        private readonly UnitController _unit;
        private Vector3Int _previousPosition;

        public MoveCommand(UnitController unit)
        {
            _unit = unit;
        }

        public void Execute()
        {
            _previousPosition = _unit.currentGridPosition;
            _unit.MoveCommand();
        }
        
        public void Undo()
        {
            _unit.UndoMoveAction(_previousPosition);
        }
    }
}
