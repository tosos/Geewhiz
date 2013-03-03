using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pooler : MonoBehaviour {

    public Transform[] poolablePrefabs;
    private Queue<Transform>[] pooledInstances;
    private Dictionary<NetworkPlayer, Queue<NetworkViewID> > pooledViewIDs;

    static private Pooler _instance = null;
    static public Pooler instance {
        get {
            if (_instance == null) {
                _instance = (Pooler) FindObjectOfType (typeof (Dispatcher));
            }
            return _instance;
        }
    }

    void Awake () {
        if (_instance != null) {
            Debug.LogError ("Instance should be null");
        }
        _instance = this;

        pooledInstances = new Queue<Transform>[poolablePrefabs.Length];
        pooledViewIDs = new Dictionary<NetworkPlayer, Queue<NetworkViewID> >();
    }

    void OnDestroy () {
        _instance = null;
    }

    int PrefabIndex (Transform prefab) {
        for (int i = 0; i < poolablePrefabs.Length; i ++) {
            if (poolablePrefabs[i] == prefab) {
                return i;
            }
        }
        return -1;
    }

    Transform InstantiateFromPool (Transform prefab, Vector3 pos, Quaternion rot) {
        int index = PrefabIndex (prefab);
        if (index < 0) {
            Debug.LogError ("Prefab " + prefab.name + " is not in poolable set");
            return null;
        }
        Transform inst;
        if (pooledInstances[index].Count == 0) {
            inst = (Transform) Instantiate (prefab, Vector3.zero, Quaternion.identity);
            if (!inst.GetComponent<Poolable>()) {
                Poolable pool = inst.gameObject.AddComponent<Poolable>();
                pool.prefabIndex = index;
            }
            pooledInstances[index].Enqueue (inst);
        } else {
            inst = pooledInstances[index].Dequeue ();
        }
        inst.position = pos;
        inst.rotation = rot;
        inst.gameObject.SetActiveRecursively (true);
        inst.BroadcastMessage ("PoolInstantiated", SendMessageOptions.DontRequireReceiver);
        return inst;
    }

    Transform NetworkInstantiateFromPool (Transform prefab, Vector3 pos, Quaternion rot) {
        Transform inst = InstantiateFromPool (prefab, pos, rot);
        if (inst) {
            NetworkView[] networkViews = inst.GetComponentsInChildren<NetworkView>();
            string viewIDs = "";
            foreach (NetworkView nv in networkViews) {
                nv.viewID = ViewFromPool ();
                viewIDs += "" + nv.viewID + ",";
            }
            int index = inst.GetComponent<Poolable>().prefabIndex;
            networkView.RPC ("RemoteInstance", RPCMode.OthersBuffered, 
                viewIDs, index, pos, rot);
        }
        return inst;
    }

    [RPC]
    void RemoteInstance (string viewIDstring, int index, Vector3 pos, Quaternion rot) {
    }

    void ReturnToPool (Transform instance) {
        instance.BroadcastMessage ("PoolReturned", SendMessageOptions.DontRequireReceiver);
        instance.gameObject.SetActiveRecursively (false);
        pooledInstances[instance.GetComponent<Poolable>().prefabIndex].Enqueue (instance);
    }

    void NetworkReturnToPool (Transform instance) {
        // TODO returntopool networked.
    }

    NetworkViewID ViewFromPool () {
        if (pooledViewIDs.Count == 0) {
            pooledViewIDs.Enqueue (Network.AllocateViewID ());
        }
        return pooledViewIDs.Dequeue ();
    }

    void ViewToPool (NetworkViewID viewID) {
        pooledViewIDs.Enqueue (viewID);        
    }
}
