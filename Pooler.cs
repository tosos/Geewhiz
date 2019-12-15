using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if CLICKS_USES_PUN
using Photon.Pun;
using Photon.Realtime;
#endif

public class Pooler : MonoBehaviour {

	public delegate void OnPoolInstantiated (Transform instance);
	public event OnPoolInstantiated onPoolInstantiated;

    public Transform[] poolablePrefabs;
    protected Queue<Transform>[] pooledInstances;
	// protected Dictionary<NetworkHash128, int> assetIdToIndex;
    public int minPooledIds = 5;

	public delegate void NetworkInstantiateDelegate (Transform inst);
    // private List<NetworkInstantiateDelegate> callbacks;

    protected const byte RemoteInstanceMsg = 10;

    static private Pooler _instance = null;
    static public Pooler instance {
        get {
            if (_instance == null) {
                _instance = (Pooler) FindObjectOfType (typeof (Pooler));
            }
            return _instance;
        }
    }

	public Transform GetPrefab (Transform inst) {
		Poolable pool = inst.GetComponent<Poolable> ();	
		return pool != null ? poolablePrefabs[pool.prefabIndex] : null;
	}

    protected virtual void Awake () {
        if (_instance != null) {
            Debug.LogError ("Instance should be null");
        }
        _instance = this;

        pooledInstances = new Queue<Transform>[poolablePrefabs.Length];
		// TODO allow pre-warming the instances

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

    protected void OnDestroy () {
        _instance = null;
    }

    public virtual Transform InstantiateFromPool (Transform prefab, Vector3 pos, Quaternion rot, Transform parent = null) {
        int index = PrefabIndex (prefab);
        if (index < 0) {
            Debug.LogError ("Prefab " + prefab.name + " is not in poolable set");
            return null;
        }
        Transform inst = InstantiateInternal (index, prefab.gameObject.tag, prefab.gameObject.layer, pos, rot, parent);
		
		return inst;
    }

    public virtual Transform NetworkInstantiateFromPool (Transform prefab, Vector3 pos, Quaternion rot) {
        int index = PrefabIndex (prefab);
        if (index < 0) {
            Debug.LogError ("Prefab " + prefab.name + " is not in poolable set");
            return null;
        }
        Transform inst = InstantiateInternal(index, prefab.gameObject.tag, prefab.gameObject.layer, pos, rot);
#if CLICKS_USES_PUN
        if (PhotonNetwork.IsConnected)
        {
            int viewID = -1;
            PhotonView view = inst.GetComponent<PhotonView>();
            if (view != null)
            {
                view.ViewID = PhotonNetwork.AllocateViewID(false);
                viewID = view.ViewID;
            }
            object[] content = { viewID, index, prefab.gameObject.tag, prefab.gameObject.layer, pos, rot };
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            var sendOptions = new ExitGames.Client.Photon.SendOptions { Reliability = true };
            if (!PhotonNetwork.RaiseEvent(RemoteInstanceMsg, content, raiseEventOptions, sendOptions))
            {
                Debug.LogError("Failed to send new instance to network");
            }
        }
#endif
        return inst;
    }

    public void ReturnToPool (Transform instance, float time = 0.0f) {
		// Debug.Log ("Receiving a ReturnToPool call for " + instance.gameObject.name, instance.gameObject);
		if (time > 0) {
       		StartCoroutine (TimedReturn (instance, time));
		} else {
       		StartCoroutine (DelayedReturn (instance));
		}
    }

#if CLICKS_USES_PUN
    protected virtual void OnPhotonEvent (ExitGames.Client.Photon.EventData photonEvent)
    {
        if (photonEvent.Code == RemoteInstanceMsg)
        {
            object[] data = (object[])photonEvent.CustomData;
            Transform inst = InstantiateInternal((int)data[1], (string)data[2], (int)data[3], (Vector3)data[4], (Quaternion)data[5]);
            PhotonView view = inst.GetComponent<PhotonView>();
            if (view != null)
            {
                view.ViewID = (int)data[0];
            }
        }
    }
#endif

    protected int PrefabIndex (Transform prefab) {
		for (int i = 0; i < poolablePrefabs.Length; i ++) {
			if (prefab == poolablePrefabs[i]) {
				return i;
			}
		}
		return -1;
    }

    protected Transform InstantiateInternal (int index, string tag, int layer, Vector3 pos, Quaternion rot, Transform parent = null) {
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
			// Do this here so that it happens before the pool start
			inst.BroadcastMessage ("LoadVisuals", SendMessageOptions.DontRequireReceiver);
        } else {
            inst = pooledInstances[index].Dequeue ();
        }
        inst.parent = parent;
		inst.gameObject.tag = tag;
		inst.gameObject.layer = layer;
        inst.position = pos;
        inst.rotation = rot;
        inst.gameObject.SetActive (true);
		inst.BroadcastMessage ("EnableVisuals", SendMessageOptions.DontRequireReceiver);
        StartCoroutine(SendPoolInstantiated(inst));
        return inst;
    }

	protected IEnumerator SendPoolInstantiated (Transform inst) {
		// Allow the PoolStart to happen before this does
		yield return null;

		if (onPoolInstantiated != null)
			onPoolInstantiated (inst);
	}

    IEnumerator TimedReturn (Transform instance, float time) {
        yield return new WaitForSeconds (time);
        StartCoroutine (DelayedReturn (instance));
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

    protected virtual IEnumerator DelayedReturn (Transform instance) {
        yield return null;

#if CLICKS_USES_PUN
        var view = instance.GetComponent<PhotonView>();
        if (view != null)
        {
            // TODO call UnspawnPoolable on remotes?
            PhotonNetwork.LocalCleanPhotonView(view);
        }
#endif

        instance.parent = transform;

        Poolable pool = instance.GetComponent<Poolable>();
        if (!pool) {
            Debug.LogError ("Poolable hasn't been added to " + instance.name);
        }
		pool.Return ();
        instance.gameObject.SetActive (false);
		if (pooledInstances[pool.prefabIndex] == null) {
			// This case covers instances that exist in the scene
			// but are returned before an InstantiateFromPool
			pooledInstances[pool.prefabIndex] = new Queue<Transform>();
		}
		pooledInstances[pool.prefabIndex].Enqueue (instance);
    }
}
