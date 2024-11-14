using UnityEngine;
using System.Collections.Generic;

public class Poolable : MonoBehaviour
{
    public int prefabIndex;
    public int poolId = -1;
    private bool needsStart = true;
    private bool needsRestore = false;

    private bool isQuitting = false;

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
    private void Update()
    {
        ProcessStartupIfNeeded();
    }

	protected void OnDestroy()
	{
        if (!isQuitting && Application.isPlaying) {
            Debug.LogWarning("Destroy shouldn't be called on Poolable in most cases");
        }
    }

    // Need to update in either fixed or update depending on which is going to run first (unknown apriori)
    private void FixedUpdate()
    {
        ProcessStartupIfNeeded();
    }

    private void ProcessStartupIfNeeded()
    {
        if (needsStart)
        {
            Application.wantsToQuit += () => isQuitting = true;
            
            SendMessage("PoolStart", SendMessageOptions.DontRequireReceiver);
            needsStart = false;
            enabled = false;
        }

        if (needsRestore) {
            for (int i = 0; i < container.states.Count; i++) {
                SendMessage("LoadStateFromPool", container.states[i],
                                 SendMessageOptions.DontRequireReceiver);
            }
            needsRestore = false;
            enabled = false;
        }
    }

    public void Return()
    {
        SendMessage("PoolReturn", SendMessageOptions.DontRequireReceiver);
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
        SendMessage("SaveStateToPool", this, SendMessageOptions.DontRequireReceiver);
        return JsonUtility.ToJson(container);
    }

    public void LoadState(string state)
    {
        container = JsonUtility.FromJson<StateContainer>(state);
        needsRestore = true;
        enabled = true;
    }
}
