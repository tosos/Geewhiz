using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if CLICKS_USES_PUN
using Photon.Pun;
using Photon.Realtime;
#endif

public class Pooler : MonoBehaviour
{
    public delegate void OnPoolInstantiated(Transform instance);
    public event OnPoolInstantiated onPoolInstantiated;

    public List<Transform> poolablePrefabs;
    protected Queue<Transform>[] pooledInstances;
    protected List<Transform>[] activeInstances;
    protected Dictionary<int, Transform>poolIdToInstance;
    public int minPooledIds = 5;
    public List<Transform> dontSaveSet;

    protected int nextPoolId = 1;

    public delegate void NetworkInstantiateDelegate(Transform inst);
    // private List<NetworkInstantiateDelegate> callbacks;

    protected const byte RemoteInstanceMsg = 10;

    protected bool isRestoringState = false;
    public bool IsRestoringState
    {
        get
        {
            return isRestoringState;
        }
    }

    static private Pooler _instance = null;
    static public Pooler instance
    {
        get
        {
            if (_instance == null) { _instance = (Pooler) FindObjectOfType(typeof (Pooler)); }
            return _instance;
        }
    }

    public Transform GetPrefab(Transform inst)
    {
        Poolable pool = inst.GetComponent<Poolable>();
        return pool != null ? poolablePrefabs[pool.prefabIndex] : null;
    }

    public Transform FindInstance(int poolId)
    {
        if (poolIdToInstance.ContainsKey(poolId)) {
            return poolIdToInstance[poolId];
        } else {
            return null;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null) { Debug.LogError("Instance should be null"); }
        _instance = this;

        pooledInstances = new Queue<Transform>[ poolablePrefabs.Count ];
        // TODO allow pre-warming the instances
        activeInstances = new List<Transform>[ poolablePrefabs.Count ];
        poolIdToInstance = new Dictionary<int, Transform>();

        // callbacks = new List<NetworkInstantiateDelegate> ();
    }

    protected virtual void OnEnable()
    {
#if CLICKS_USES_PUN
        PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
#endif
    }

    protected virtual void OnDisable()
    {
#if CLICKS_USES_PUN
        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
#endif
    }

    protected void OnDestroy()
    {
        _instance = null;
    }

    public virtual Transform InstantiateFromPool(Transform prefab, Vector3 pos = default(Vector3),
                                                 Quaternion rot = default(Quaternion),
                                                 Transform parent = null)
    {
        return InstantiateFromPool(prefab, prefab.gameObject.tag, prefab.gameObject.layer, pos, rot,
                                   parent);
    }

    public virtual Transform InstantiateFromPool(Transform prefab, Transform parent)
    {
        return InstantiateFromPool(prefab, prefab.gameObject.tag, 
            prefab.gameObject.layer, prefab.position, prefab.rotation, parent);
    }

    public virtual Transform InstantiateFromPool(Transform prefab, string tag, int layer,
                                                 Vector3 pos = default(Vector3),
                                                 Quaternion rot = default(Quaternion),
                                                 Transform parent = null)
    {
        int index = PrefabIndex(prefab);
        if (index < 0) {
            Debug.LogError("Prefab " + prefab.name + " is not in poolable set");
            return null;
        }
        Transform inst = InstantiateInternal(index, nextPoolId, tag, layer, pos, rot, parent);
        nextPoolId++;
        return inst;
    }

    public virtual Transform NetworkInstantiateFromPool(Transform prefab,
                                                        Vector3 pos = default(Vector3),
                                                        Quaternion rot = default(Quaternion))
    {
        return NetworkInstantiateFromPool(prefab, prefab.gameObject.tag, prefab.gameObject.layer,
                                          pos, rot);
    }

    public virtual Transform NetworkInstantiateFromPool(Transform prefab, string tag, int layer,
                                                        Vector3 pos = default(Vector3),
                                                        Quaternion rot = default(Quaternion))
    {
        int prefabIndex = PrefabIndex(prefab);
        if (prefabIndex < 0) {
            Debug.LogError("Prefab " + prefab.name + " is not in poolable set");
            return null;
        }
        int viewId = -1;
#if CLICKS_USES_PUN
        if (PhotonNetwork.IsConnected) {
            viewId = PhotonNetwork.AllocateViewID(false);
        } else
#endif
        {
            viewId = nextPoolId ++;
        }
        Transform inst = InstantiateInternal(prefabIndex, viewId, tag, layer, pos, rot);
#if CLICKS_USES_PUN
        if (PhotonNetwork.IsConnected) {
            PhotonView view = inst.GetComponent<PhotonView>();
            if (view != null) { view.ViewID = viewId; }
            // override the auto-allocated poolID with the one from the network
            object[] content = {prefabIndex, viewId, prefab.gameObject.tag, prefab.gameObject.layer,
                                pos,         rot};
            RaiseEventOptions raiseEventOptions =
                new RaiseEventOptions{Receivers = ReceiverGroup.Others};
            var sendOptions = new ExitGames.Client.Photon.SendOptions{Reliability = true};
            if (!PhotonNetwork.RaiseEvent(RemoteInstanceMsg, content, raiseEventOptions,
                                          sendOptions)) {
                Debug.LogError("Failed to send new instance to network");
            }
        }
#endif
        return inst;
    }

    public virtual void ReturnAll()
    {
        List<Transform> allofthem = new List<Transform>();
        foreach (List<Transform> set in activeInstances) {
            if (set == null) { continue; }
            foreach (Transform inst in set) {
                allofthem.Add(inst);
            }
        }

        foreach (Transform inst in allofthem) {
            DirectReturn(inst);
        }
    }

    public void ReturnToPool(Transform instance, float time = 0.0f)
    {
        // Debug.Log ("Receiving a ReturnToPool call for " + instance.gameObject.name, instance.gameObject);
        if (time > 0) {
            StartCoroutine(TimedReturn(instance, time));
        } else {
            StartCoroutine(DelayedReturn(instance));
        }
    }

#if CLICKS_USES_PUN
    protected virtual void OnPhotonEvent(ExitGames.Client.Photon.EventData photonEvent)
    {
        if (photonEvent.Code == RemoteInstanceMsg) {
            object[] data = (object[]) photonEvent.CustomData;
            // TODO can we transfer the poolId somehow safely vs -1 here?
            Transform inst =
                InstantiateInternal((int) data[0], (int) data[1], (string) data[2], (int) data[3],
                                    (Vector3) data[4], (Quaternion) data[5]);
            PhotonView view = inst.GetComponent<PhotonView>();
            if (view != null) { view.ViewID = (int) data[1]; }
        }
    }
#endif

    protected int PrefabIndex(Transform prefab)
    {
        for (int i = 0; i < poolablePrefabs.Count; i++) {
            if (prefab == poolablePrefabs[i]) { return i; }
        }
        return -1;
    }

    protected Transform InstantiateInternal(int index, int id, string tag, int layer, Vector3 pos,
                                            Quaternion rot, Transform parent = null)
    {
        Transform inst;
        if (pooledInstances[index] == null) { pooledInstances[index] = new Queue<Transform>(); }
        if (activeInstances[index] == null) { activeInstances[index] = new List<Transform>(); }
        Poolable poolable = null;
        if (pooledInstances[index].Count == 0) {
            inst =
                (Transform) Instantiate(poolablePrefabs[index], Vector3.zero, Quaternion.identity);
            inst.gameObject.name = poolablePrefabs[index].name + "-" + id + "-" + activeInstances[index].Count;
            poolable = inst.GetComponent<Poolable>();
            if (poolable == null) { poolable = inst.gameObject.AddComponent<Poolable>(); }
            poolable.prefabIndex = index;
            // Do this here so that it happens before the pool start
            inst.BroadcastMessage("LoadVisuals", SendMessageOptions.DontRequireReceiver);
        } else {
            inst = pooledInstances[index].Dequeue();
            poolable = inst.GetComponent<Poolable>();
        }

        if (poolIdToInstance.ContainsKey(id)) {
            GameObject current = FindInstance(id)?.gameObject;
            Debug.LogError ("Catastrophic id error, repeated poolid " + id + " with index " + index + ".\n" +
                            "The current object with that id is " + current + ".\n" +
                            "New instance is " + inst.gameObject.name + "\n." +
                            "Attempting to correct, good luck", current);
            while (poolIdToInstance.ContainsKey(id)) {
                id ++;
            }
            nextPoolId = id + 1;
        }

        poolable.poolId = id;
        poolIdToInstance[id] = inst;

        activeInstances[index].Add(inst);

        inst.parent = parent;
        inst.gameObject.tag = tag;
        inst.gameObject.layer = layer;
        inst.position = pos;
        inst.rotation = rot;
        inst.gameObject.SetActive(true);
        inst.BroadcastMessage("EnableVisuals", SendMessageOptions.DontRequireReceiver);
        StartCoroutine(SendPoolInstantiated(inst));
        return inst;
    }

    protected IEnumerator SendPoolInstantiated(Transform inst)
    {
        // Allow the PoolStart to happen before this does
        yield return null;
        onPoolInstantiated?.Invoke(inst);
    }

    IEnumerator TimedReturn(Transform instance, float time)
    {
        yield return new WaitForSeconds(time);
        StartCoroutine(DelayedReturn(instance));
    }
    protected virtual void UnspawnPoolable(GameObject go)
    {
        // Have we already retpooled this one?  If not then we need to
        /* TODO need to work out the timing here if the local hasn't finished with it yet.
        if (go.activeSelf && go.transform.parent != transform) {
               StartCoroutine (DelayedReturn (go.transform));
        }
        */
    }

    protected virtual IEnumerator DelayedReturn(Transform instance)
    {
        yield return null;
        DirectReturn(instance);
    }

    protected virtual void DirectReturn(Transform instance)
    {
#if CLICKS_USES_PUN
        var view = instance.GetComponent<PhotonView>();
        if (view != null) {
            // TODO call UnspawnPoolable on remotes?
            PhotonNetwork.LocalCleanPhotonView(view);
        }
#endif
        Poolable pool = instance.GetComponent<Poolable>();
        if (!pool) { Debug.LogError("Poolable hasn't been added to " + instance.name); }
        pool.Return();

        if (activeInstances[pool.prefabIndex] == null) {
            Debug.LogWarning("We're pool returning an object that was never instantiated");
            activeInstances[pool.prefabIndex] = new List<Transform>();
        }
        activeInstances[pool.prefabIndex].Remove(instance);

        poolIdToInstance.Remove(pool.poolId);

        if (pooledInstances[pool.prefabIndex] == null) {
            // This case covers instances that exist in the scene
            // but are returned before an InstantiateFromPool
            pooledInstances[pool.prefabIndex] = new Queue<Transform>();
        }
        pooledInstances[pool.prefabIndex].Enqueue(instance);

        instance.parent = transform;
        instance.gameObject.SetActive(false);
    }

    private struct InstanceData {
        public int prefabIndex;
        public int poolId;
        public string tag;
        public int layer;
        public Vector3 position;
        public Quaternion rotation;
        public int parentPoolId;
        public string poolState;
    };
    
    protected bool DontSave(int prefabIndex) 
    {
        return dontSaveSet.Contains(poolablePrefabs[prefabIndex]);
    }

    public virtual string SaveState()
    {
        var storeInstances = new List<InstanceData>();
        for (int index = 0; index < activeInstances.Length; index++) {
            for (int i = 0; i < activeInstances[index].Count; i++) {
                if (DontSave(index)) {
                    continue;
                }
                var inst = activeInstances[index][i];
                var poolable = inst.gameObject.GetComponent<Poolable>();
                var data = new InstanceData();
                data.prefabIndex = poolable.prefabIndex;
                data.poolId = poolable.poolId;
                data.parentPoolId = -1;
                // TODO ensure the child is stored after the parent in the list
                // or handle it in the load by delaying children load states
                if (inst.parent != null) {
                    var parentPoolable = inst.parent.gameObject.GetComponent<Poolable>();
                    if (parentPoolable != null) {
                        data.parentPoolId = parentPoolable.poolId;
                    }
                }
                data.poolState = poolable.SaveState();
                data.tag = inst.gameObject.tag;
                data.layer = inst.gameObject.layer;
                data.position = inst.position;
                data.rotation = inst.rotation;
                storeInstances.Add(data);
            }
        }
        return JsonUtility.ToJson(storeInstances);
    }

    public virtual void LoadState(string state)
    {
        if (nextPoolId > 1) {
            Debug.LogError ("Constructed some assets before loading, that's a problem");
        }
        var storeInstances = JsonUtility.FromJson<List<InstanceData>>(state);
        isRestoringState = true;
        for (int i = 0; i < storeInstances.Count; i++) {
            var data = storeInstances[i];
            var parent = data.parentPoolId >= 0 ? FindInstance(data.parentPoolId) : null;
            var inst = InstantiateInternal(data.prefabIndex, data.poolId, data.tag,
                                                 data.layer, data.position, data.rotation, parent);
            inst.gameObject.GetComponent<Poolable>().LoadState(data.poolState);
            if (nextPoolId <= data.poolId) {
                nextPoolId = data.poolId + 1;
            }
        }
        Debug.Log("Pooler is done restoring state");
        isRestoringState = false;
    }
}
