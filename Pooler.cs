using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pooler : MonoBehaviour {

    public Transform[] poolablePrefabs;
    private Queue<Transform>[] pooledInstances;
    private Dictionary<NetworkPlayer, Queue<NetworkViewID> > pooledViewIDs;
    public int minPooledIds = 5;

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

        pooledViewIDs[Network.player] = new Queue<NetworkViewID> ();
        if (Network.peerType != NetworkPeerType.Disconnected) {
            StartCoroutine (FillViewPool ());
        }
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

    Transform InstantiateInternal (int index, Vector3 pos, Quaternion rot) {
        Transform inst;
        if (pooledInstances[index] == null) {
            pooledInstances[index] = new Queue<Transform>();
        }
        if (pooledInstances[index].Count == 0) {
            inst = (Transform) Instantiate (poolablePrefabs[index], Vector3.zero, Quaternion.identity);
            Poolable pool = inst.GetComponent<Poolable>();
            if (pool == null) {
                pool = inst.gameObject.AddComponent<Poolable>();
            }
            pool.prefabIndex = index;
        } else {
            inst = pooledInstances[index].Dequeue ();
        }
        inst.position = pos;
        inst.rotation = rot;
        inst.gameObject.SetActiveRecursively (true);
        StartCoroutine (DelayedStartMessage (inst));
        return inst;
    }

    IEnumerator DelayedStartMessage (Transform inst) {
        yield return new WaitForEndOfFrame ();
        inst.BroadcastMessage ("PoolStart", SendMessageOptions.DontRequireReceiver);
    }

    public Transform InstantiateFromPool (Transform prefab, Vector3 pos, Quaternion rot) {
        int index = PrefabIndex (prefab);
        if (index < 0) {
            Debug.LogError ("Prefab " + prefab.name + " is not in poolable set");
            return null;
        }
        return InstantiateInternal (index, pos, rot);
    }

    public Transform NetworkInstantiateFromPool (Transform prefab, Vector3 pos, Quaternion rot) {
        int index = PrefabIndex (prefab);
        if (index < 0) {
            Debug.LogError ("Prefab " + prefab.name + " is not in poolable set");
            return null;
        }
        networkView.RPC ("RemoteInstance", RPCMode.OthersBuffered, index, pos, rot);
        Transform inst = InstantiateInternal (index, pos, rot);
        if (inst.networkView == null) {
            NetworkView view = inst.gameObject.AddComponent<NetworkView>();
            view.stateSynchronization = NetworkStateSynchronization.Off;
        }
        StartCoroutine (SetupViews (Network.player, inst));
        return inst;
    }

    [RPC]
    void RemoteInstance (int index, Vector3 pos, Quaternion rot, NetworkMessageInfo info) {
        Transform inst = InstantiateInternal (index, pos, rot);
        StartCoroutine (SetupViews (info.sender, inst));
    }

    IEnumerator SetupViews (NetworkPlayer player, Transform inst) {
        NetworkView[] views = inst.GetComponentsInChildren<NetworkView>();
        foreach (NetworkView view in views) {
            NetworkViewID id = ViewFromPool (player);
            while (id == NetworkViewID.unassigned) {
                Debug.LogError ("Having to wait on Ids.  minPooledIds must be increased");
                yield return new WaitForSeconds (0.5f);
                id = ViewFromPool (player);
            }
            view.viewID = id;
        }
        yield break;
    }

    IEnumerator DelayedReturn (Transform instance) {
        yield return new WaitForEndOfFrame ();
        instance.BroadcastMessage ("PoolReturn", SendMessageOptions.DontRequireReceiver);
        instance.gameObject.SetActiveRecursively (false);
        pooledInstances[instance.GetComponent<Poolable>().prefabIndex].Enqueue (instance);
        NetworkView[] views = instance.GetComponentsInChildren<NetworkView>();
        foreach (NetworkView view in views) {
            if (view.viewID != NetworkViewID.unassigned) {
                ViewToPool (view.viewID);
                view.viewID = NetworkViewID.unassigned;
            }
        }
    }

    public void ReturnToPool (Transform instance) {
        StartCoroutine (DelayedReturn (instance));
    }

    public void NetworkReturnToPool (Transform instance) {
        if (instance.networkView.isMine) {
            NetworkViewID id = instance.networkView.viewID;
            networkView.RPC ("RPCReturnID", RPCMode.AllBuffered, id);
        }
    }

    [RPC]
    void RPCReturnID (NetworkViewID id) {
        NetworkView view = NetworkView.Find (id);
        Transform instance = view.transform;
        StartCoroutine (DelayedReturn (instance));
    }

    IEnumerator FillViewPool () {
        while (pooledViewIDs[Network.player].Count < minPooledIds) {
            NetworkViewID viewID = Network.AllocateViewID ();
            networkView.RPC ("AddViewID", RPCMode.AllBuffered, viewID);
            yield return new WaitForEndOfFrame ();
        }
    }

    [RPC]
    void AddViewID (NetworkViewID viewID) {
        if (!pooledViewIDs.ContainsKey (viewID.owner)) {
            pooledViewIDs[viewID.owner] = new Queue<NetworkViewID> ();
        }
        pooledViewIDs[viewID.owner].Enqueue (viewID);
    }

    NetworkViewID ViewFromPool (NetworkPlayer player) {
        NetworkViewID ret;
        if (pooledViewIDs[player].Count == 0) {
            ret = NetworkViewID.unassigned;
        } else {
            ret = pooledViewIDs[player].Dequeue ();
        }
        if (Network.player == player && pooledViewIDs[player].Count < minPooledIds) {
            FillViewPool ();
        }
        return ret;
    }

    void ViewToPool (NetworkViewID viewID) {
        pooledViewIDs[viewID.owner].Enqueue (viewID);        
    }
}
