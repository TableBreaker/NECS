using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Entity
{
    public Entity(int id, EntityWorldBase world, GameObject link = null)
    {
        EntityID = id;
        EntityWorld = world;
        _componentDic = new Dictionary<Type, ComponentBase>();
    }

    public void Dispose()
    {
        if (_destroyed) return;

        if(EntityLink)
        {
            UnityEngine.Object.Destroy(EntityLink);
            EntityLink = null;
        }

        foreach(var pair in _componentDic)
        {
            pair.Value.Dispose();
            EntityWorld.OnEntityRemoveComponent(pair.Value);
        }

        _componentDic.Clear();

        EntityWorld = null;

        _destroyed = true;
    }

    public T AddEntityComponent<T>() where T : ComponentBase, new()
    {
        var comp = new T();
        if (_componentDic.ContainsKey(typeof(T)))
        {
            Debug.LogError("Only one component can be added to a entity for a certain Type!");
            return null;
        }
        _componentDic[typeof(T)] = comp;
        EntityWorld.OnEntityAddComponent(comp);
        return comp;
    }

    public void RemoveEntityComponent(ComponentBase component)
    {
        if (!_componentDic.ContainsValue(component))
        {
            Debug.LogErrorFormat("Component of type [{0}] does not exist!", component.GetType());
            return;
        }
        _componentDic.Remove(component.GetType());
        EntityWorld.OnEntityRemoveComponent(component);
    }
    
    public static implicit operator bool(Entity entity)
    {
        return entity != null && !entity._destroyed;
    }

    public int EntityID { get; private set; }
    public EntityWorldBase EntityWorld { get; private set; }
    public GameObject EntityLink { get; private set; }

    private bool _destroyed;
    private Dictionary<Type, ComponentBase> _componentDic;
}
