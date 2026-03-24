using RedBjorn.ProtoTiles;
using UnityEngine;

public class BaseManager : MonoBehaviour
{
    protected GameMediator _gameMediator;
    public virtual void SetMediator(GameMediator mediator)
    {
        _gameMediator = mediator;
    }

    public virtual void Initialize(GameMediator mediator)
    {
        SetMediator(mediator);
    }

    public virtual void SetCachedMap(MapEntity map)
    {
    }
}
