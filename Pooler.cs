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
        networkView.RPC ("RemoteInstance", RPCMode.OthersBuffered, index, pos, rot, Network.player);
        Transform inst = InstantiateInternal (index, pos, rot);
        if (inst.networkView == null) {
            NetworkView view = inst.gameObject.AddComponent<NetworkView>();
            view.stateSynchronization = NetworkStateSynchronization.Off;
        }
        StartCoroutine (SetupViews (Network.player, inst));
        return inst;
    }

    public void ReturnToPool (Transform instance) {
        StartCoroutine (DelayedReturn (instance));
    }

    public void ReturnToPool (Transform instance, float time) {
        StartCoroutine (TimedReturn (instance, time));
    }

    public void NetworkReturnToPool (Transform instance) {
        if (instance.networkView.isMine) {
            NetworkViewID id = instance.networkView.viewID;
            networkView.RPC ("RPCReturnID", RPCMode.AllBuffered, id);
        }
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
        inst.parent = null;
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

    [RPC]
    void RemoteInstance (int index, Vector3 pos, Quaternion rot, NetworkPlayer sender) {
        Transform inst = InstantiateInternal (index, pos, rot);
        StartCoroutine (SetupViews (sender, inst));
    }

    IEnumerator DelayedReturn (Transform instance) {
        yield return new WaitForEndOfFrame ();
        instance.BroadcastMessage ("PoolReturn", SendMessageOptions.DontRequireReceiver);
        instance.parent = transform;
        foreach (NetworkView view in instance.GetComponentsInChildren<NetworkView>()) {
            if (view.viewID != NetworkViewID.unassigned) {
                ViewToPool (view.viewID);
                view.viewID = NetworkViewID.unassigned;
            }
        }
        foreach (Rigidbody rb in instance.GetComponentsInChildren<Rigidbody>()) {
            if (!rb.isKinematic) {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        Poolable pool = instance.GetComponent<Poolable>();
        if (!pool) {
            Debug.LogError ("Poolable hasn't been added to a component being returned.");
        }
        instance.gameObject.SetActiveRecursively (false);
        pooledInstances[pool.prefabIndex].Enqueue (instance);
    }

    IEnumerator TimedReturn (Transform instance, float time) {
        yield return new WaitForSeconds (time);
        StartCoroutine (DelayedReturn (instance));
    }

    [RPC]
    void RPCReturnID (NetworkViewID id) {
        NetworkView view = NetworkView.Find (id);
        Transform instance = view.transform;
        StartCoroutine (DelayedReturn (instance));
    }

    private bool semaphoreFillViewPool = false;
    IEnumerator FillViewPool () {
        if (semaphoreFillViewPool) {
            yield break;
        }
        semaphoreFillViewPool = true;
        while (pooledViewIDs[Network.player].Count < minPooledIds) {
            NetworkViewID viewID = Network.AllocateViewID ();
            networkView.RPC ("AddViewID", RPCMode.All, viewID, Network.player);
            yield return new WaitForEndOfFrame ();
        }
        semaphoreFillViewPool = false;
    }

    [RPC]
    void AddViewID (NetworkViewID viewID, NetworkPlayer sender) {
        if (!pooledViewIDs.ContainsKey (sender)) {
            Debug.Log ("Adding a view ID from player " + viewID.owner);
            pooledViewIDs[sender] = new Queue<NetworkViewID> ();
        }
        pooledViewIDs[sender].Enqueue (viewID);
    }

    NetworkViewID ViewFromPool (NetworkPlayer player) {
        NetworkViewID ret;
        if (pooledViewIDs[player].Count == 0) {
            ret = NetworkViewID.unassigned;
        } else {
            ret = pooledViewIDs[player].Dequeue ();
        }
        if (Network.player == player && pooledViewIDs[player].Count < minPooledIds) {
            StartCoroutine (FillViewPool ());
        }
        Debug.Log ("Returning pool ID " + ret);
        return ret;
    }

    void ViewToPool (NetworkViewID viewID) {
        /* Can't do this.  viewID.owner is the wrong thing 
        pooledViewIDs[viewID.owner].Enqueue (viewID);        
        */
    }

    public IEnumerator SetupViews (NetworkPlayer player, Transform inst) {
        Debug.Log ("Setting up views from player " + player);
        foreach (NetworkView view in inst.GetComponentsInChildren<NetworkView>()) {
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

}
