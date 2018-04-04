using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public abstract class EntityWorldBase
{
    public virtual void Initialize()
    {
        EntityDic = new Dictionary<int, Entity>();
        SingletonComponentDic = new Dictionary<Type, ComponentBase>();
        ComponentDic = new Dictionary<Type, HashSet<ComponentBase>>();
        SystemDic = new Dictionary<Type, SystemBase>();

        _entityIndexer = 0;
    }

    public virtual void Shutdown()
    {
        if(SystemDic != null)
        {
            SystemDic.Clear();
            SystemDic = null;
        }

        if(SingletonComponentDic != null)
        {
            SingletonComponentDic.Clear();
            SingletonComponentDic = null;
        }

        if(ComponentDic != null)
        {
            ComponentDic.Clear();
            ComponentDic = null;
        }

        if (EntityDic != null)
        {
            foreach (var v in EntityDic)
            {
                if (v.Value != null)
                {
                    v.Value.Dispose();
                }
            }
            EntityDic.Clear();
            EntityDic = null;
        }
    }

    public virtual void Update()
    {
        foreach(var sysPair in SystemDic)
        {
            sysPair.Value.Update(this);
        }
    }

    #region Entity
    public Entity CreateEntity(GameObject g = null)
    {
        var index = GetIndex();
        var entity = new Entity(index, this, g);
        EntityDic[index] = entity;
        return entity;
    }

    public T CreateSingletonComponent<T>() where T : ComponentBase, new()
    {
        ComponentBase singleton;
        if(!SingletonComponentDic.TryGetValue(typeof(T), out singleton))
        {
            singleton = new T();
            SingletonComponentDic[typeof(T)] = singleton;
        }

        return singleton as T;
    }

    public void OnEntityAddComponent(ComponentBase component)
    {
        if (component == null)
        {
            return;
        }

        HashSet<ComponentBase> coll = null;
        if(ComponentDic.TryGetValue(component.GetType(), out coll))
        {
            coll.Add(component);
        }
        else
        {
            coll = new HashSet<ComponentBase> { component };
            ComponentDic[component.GetType()] = coll;
        }
    }

    public void OnEntityRemoveComponent(ComponentBase component)
    {
        if (!component)
        {
            return;
        }

        HashSet<ComponentBase> coll = null;
        if (ComponentDic.TryGetValue(component.GetType(), out coll))
        {
            try
            {
                coll.Remove(component);
            }
            catch(System.Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    public HashSet<T> AllEntityComponents<T>() where T : ComponentBase
    {
        HashSet<ComponentBase> coll = null;
        if(ComponentDic.TryGetValue(typeof(T), out coll))
        {
            return new HashSet<T>(coll.Cast<T>());
        }

        return null;
    }

    public T AnyEntityComponent<T>() where T : ComponentBase
    {
        HashSet<ComponentBase> coll = null;
        if(ComponentDic.TryGetValue(typeof(T), out coll))
        {
            return coll.First() as T;
        }

        return null;
    }

    public T GetSingletonEntityComponent<T>() where T : ComponentBase
    {
        ComponentBase comp = null;
        if(SingletonComponentDic.TryGetValue(typeof(T), out comp))
        {
            return comp as T;
        }

        return null;
    }

    private int GetIndex()
    {
        return _entityIndexer++;
    }
    #endregion

    #region System
    public void RegisterSystem<T>() where T : SystemBase, new()
    {
        var system = new T();
        var type = typeof(T);
        if(SystemDic.ContainsKey(type))
        {
            Debug.LogErrorFormat("System : [{0}] has already registered", type);
            return;
        }
        SystemDic[typeof(T)] = system;
    }

    public void UnregisterSystem<T>() where T : SystemBase
    {
        var type = typeof(T);
        if (!SystemDic.ContainsKey(type)) return;
        SystemDic.Remove(type);
    }
    #endregion

    public bool                                             Initialized { get; protected set; }

    protected Dictionary<Type, SystemBase>                  SystemDic;
    protected Dictionary<int, Entity>                       EntityDic;
    protected Dictionary<Type, HashSet<ComponentBase>>      ComponentDic;
    protected Dictionary<Type, ComponentBase>               SingletonComponentDic;

    private int                                             _entityIndexer;
}

public abstract class SystemBase
{
    public abstract void Update(EntityWorldBase world);
}

public abstract class ComponentBase
{
    public virtual void Initialize(Entity entity)
    {
        ContainerEntity = entity;
    }

    public virtual void Dispose()
    {
        if (_disposed) return;

        ContainerEntity.RemoveEntityComponent(this);
        ContainerEntity = null;
        _disposed = true;
    }

    public static implicit operator bool(ComponentBase component)
    {
        return component != null && !component._disposed;
    }

    public Entity   ContainerEntity;

    private bool    _disposed;
}