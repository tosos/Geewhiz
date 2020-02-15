using UnityEngine;
using System.Collections.Generic;

public class Poolable : MonoBehaviour
{
    public int prefabIndex;
    public int poolId = -1;
    private bool needsStart = true;
    private bool needsRestore = false;

    [System.Serializable]
    public class StatePair {
        public string id;
        public string state;
    };
    [System.Serializable]
    private class StateContainer {
        public List<StatePair>states = new List<StatePair> ();
    }
    private StateContainer container;

    // This script should be configured to run before all others.
    void Update()
    {
        if (needsStart) {
            BroadcastMessage("PoolStart", SendMessageOptions.DontRequireReceiver);
            needsStart = false;
            enabled = false;
        }
    }

    // Need to update in either fixed or update depending on which is going to run first (unknown apriori)
    void FixedUpdate()
    {
        if (needsStart) {
            BroadcastMessage("PoolStart", SendMessageOptions.DontRequireReceiver);
            needsStart = false;
            enabled = false;
        }

        if (needsRestore) {
            for (int i = 0; i < container.states.Count; i++) {
                BroadcastMessage("LoadStateFromPool", container.states[i],
                                 SendMessageOptions.DontRequireReceiver);
            }
            needsRestore = false;
        }
    }

    public void Return()
    {
        BroadcastMessage("PoolReturn", SendMessageOptions.DontRequireReceiver);
        needsStart = true;
        enabled = true;

        // Reset components to their default values when going back to the pool
        foreach(Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (!rb.isKinematic) {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void AddStatePair(string id, string state)
    {
        StatePair sp = new StatePair();
        sp.id = id;
        sp.state = state;
        container.states.Add(sp);
    }

    public string SaveState()
    {
        container = new StateContainer ();
        BroadcastMessage("SaveStateToPool", this, SendMessageOptions.DontRequireReceiver);
        return JsonUtility.ToJson(container);
    }

    public void LoadState(string state)
    {
        container = JsonUtility.FromJson<StateContainer>(state);
        needsRestore = true;
    }
}
