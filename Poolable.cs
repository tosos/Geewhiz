using UnityEngine;
using System.Collections.Generic;

public class Poolable : MonoBehaviour
{
    public int prefabIndex;
    private bool needsStart = true;
    private bool needsRestore = false;

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
            for (int i = 0; i < states.Count; i++) {
                BroadcastMessage("LoadStateFromPool", states[i],
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

    public struct StatePair {
        public string id;
        public string state;
    };
    private List<StatePair>states;

    public void AddStatePair(string id, string state)
    {
        StatePair sp = new StatePair();
        sp.id = id;
        sp.state = state;
        states.Add(sp);
    }

    public string SaveState()
    {
        states = new List<StatePair>();
        BroadcastMessage("SaveStateToPool", this, SendMessageOptions.DontRequireReceiver);
        return JsonUtility.ToJson(states);
    }

    public void LoadState(string state)
    {
        states = JsonUtility.FromJson<List<StatePair>>(state);
        needsRestore = true;
    }
}
