using System;
using System.Collections.Generic;
using UnityEngine;

public interface IUpdater
{
    void update(float delta);
}

public class GlobalTimer : MonoBehaviour
{
    static GlobalTimer _instance;

    List<IUpdater> handlers = new List<IUpdater>();
    List<IUpdater> toForget = new List<IUpdater>();
    List<IUpdater> toAdd = new List<IUpdater>();
    List<Action> actions = new List<Action>();

    public void add(IUpdater handler)
    {
        toAdd.Add(handler);
    }

    public void execute(Action action)
    {
        actions.Add(action);
    }

    public void forget(IUpdater handler)
    {
        toForget.Add(handler);
    }

    public static GlobalTimer instance
    {
        get
        {
            if (!_instance)
            {
                GameObject gobj = Activator.CreateInstance<GameObject>();
                _instance = gobj.AddComponent<GlobalTimer>();
            }
            return _instance;
        }
    }

    void forgetAll()
    {

    }

    void Update()
    {
        foreach (Action a in actions)
        {
            a();
        }
        actions.Clear();

        float delta = Time.deltaTime;

        foreach (IUpdater action in toForget)
        {
            handlers.Remove(action);
        }
        toForget.Clear();

        foreach (IUpdater action in toAdd)
        {
            handlers.Add(action);
        }
        toAdd.Clear();

        //int updateCounters = 0;
        string report = "";

        foreach (IUpdater action in handlers)
        {
            if (this.toForget.Contains(action))
            {
                continue;
            }
            action.update(delta);
            //updateCounters++; //   
            report = report + action.GetType().ToString() + "  ";
        }
        // Debug.Log(report);
    }
}