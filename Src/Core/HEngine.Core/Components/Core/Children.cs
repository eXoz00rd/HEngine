using HEngine.Core.Contracts;
using HEngine.Core.Primitives;

namespace HEngine.Core.Components.Core;

public struct Children : IComponent {
    private Entity _child1, _child2, _child3, _child4;
    private List<Entity>? _additionalChildren;
    private byte _count;

    public Children()
    {
        _child1 = Entity.Null;
        _child2 = Entity.Null;
        _child3 = Entity.Null;
        _child4 = Entity.Null;
        _additionalChildren = null;
        _count = 0;
    }

    public int Count => _count;

    public void Add(Entity child)
    {
        if (child == Entity.Null)
            return;

        switch (_count)
        {
            case 0:
                _child1 = child;
                break;
            case 1:
                _child2 = child;
                break;
            case 2:
                _child3 = child;
                break;
            case 3:
                _child4 = child;
                break;
            default:
                _additionalChildren ??= new List<Entity>();
                _additionalChildren.Add(child);
                break;
        }

        _count++;
    }

    public bool Remove(Entity child)
    {
        if (child == Entity.Null)
            return false;

        // Sprawdź pierwsze 4 sloty
        if (_child1 == child)
        {
            RemoveAtIndex(0);
            return true;
        }

        if (_child2 == child)
        {
            RemoveAtIndex(1);
            return true;
        }

        if (_child3 == child)
        {
            RemoveAtIndex(2);
            return true;
        }

        if (_child4 == child)
        {
            RemoveAtIndex(3);
            return true;
        }
        
        if (_additionalChildren != null && _additionalChildren.Remove(child))
        {
            _count--;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        _child1 = _child2 = _child3 = _child4 = Entity.Null;
        _additionalChildren?.Clear();
        _count = 0;
    }

    public Entity GetChild(int index)
    {
        if (index < 0 || index >= _count)
            return Entity.Null;

        return index switch
        {
            0 => _child1,
            1 => _child2,
            2 => _child3,
            3 => _child4,
            _ => _additionalChildren?[index - 4] ?? Entity.Null
        };
    }

    private void RemoveAtIndex(int index)
    {
        // Przesuń elementy w lewo
        switch (index)
        {
            case 0:
                _child1 = _child2;
                _child2 = _child3;
                _child3 = _child4;
                _child4 = _additionalChildren?.Count > 0 ?
                    _additionalChildren[0] :
                    Entity.Null;
                break;
            case 1:
                _child2 = _child3;
                _child3 = _child4;
                _child4 = _additionalChildren?.Count > 0 ?
                    _additionalChildren[0] :
                    Entity.Null;
                break;
            case 2:
                _child3 = _child4;
                _child4 = _additionalChildren?.Count > 0 ?
                    _additionalChildren[0] :
                    Entity.Null;
                break;
            case 3:
                _child4 = _additionalChildren?.Count > 0 ?
                    _additionalChildren[0] :
                    Entity.Null;
                break;
        }

        if (_additionalChildren?.Count > 0)
            _additionalChildren.RemoveAt(0);

        _count--;
    }
}