using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class Pooler : MonoBehaviour {

    public Transform[] poolablePrefabs;
    private Queue<Transform>[] pooledInstances;
	private Dictionary<NetworkHash128, int> assetIdToIndex;
    public int minPooledIds = 5;

	public delegate void NetworkInstantiateDelegate (Transform inst);

	private const short RemoteInstanceMsg = 1010;
    public class RemoteInstanceMessage : MessageBase
    {
        public int index;
        public Vector3 position;
        public Quaternion rotation;
    }

	private NetworkClient client;
	private List<Transform> localInstances;
	private List<NetworkInstantiateDelegate> callbacks;

    static private Pooler _instance = null;
    static public Pooler instance {
        get {
            if (_instance == null) {
                _instance = (Pooler) FindObjectOfType (typeof (Pooler));
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

		assetIdToIndex = new Dictionary<NetworkHash128, int> ();
		for (int i = 0; i < poolablePrefabs.Length; i ++) {
			NetworkHash128 assetId = poolablePrefabs[i].GetComponent<NetworkIdentity>().assetId;
			ClientScene.RegisterSpawnHandler (assetId, SpawnPoolable, UnspawnPoolable);
			assetIdToIndex[assetId] = i;
		}

		// These are used to hold instances we create until we hear back from the server
		localInstances = new List<Transform> ();
		callbacks = new List<NetworkInstantiateDelegate> ();

		if (NetworkServer.active) {
			NetworkServer.RegisterHandler (RemoteInstanceMsg, ReceiveRemoteInstanceFromClient);
		}
    }

    void OnDestroy () {
        _instance = null;
		for (int i = 0; i < poolablePrefabs.Length; i ++) {
			NetworkHash128 assetId = poolablePrefabs[i].GetComponent<NetworkIdentity>().assetId;
			ClientScene.UnregisterSpawnHandler (assetId);
		}
    }

	public void SetupClientConnection (NetworkClient c) {
		client = c;
	} 

	public void RemoveClientConnection () {
		client = null;
	}

    public Transform InstantiateFromPool (Transform prefab, Vector3 pos, Quaternion rot) {
        int index = PrefabIndex (prefab);
        if (index < 0) {
            Debug.LogError ("Prefab " + prefab.name + " is not in poolable set");
            return null;
        }
        return InstantiateInternal (index, pos, rot);
    }

    public Transform NetworkInstantiateFromPool (Transform prefab, Vector3 pos, Quaternion rot, NetworkInstantiateDelegate func  = null) {
        int index = PrefabIndex (prefab);
        if (index < 0) {
            Debug.LogError ("Prefab " + prefab.name + " is not in poolable set");
            return null;
        }
        Transform inst = InstantiateInternal (index, pos, rot);
		if (NetworkServer.active) {
			NetworkServer.Spawn(inst.gameObject);
		} else if (client != null) {
			localInstances.Add (inst);
			callbacks.Add (func);
			SendRemoteInstanceToServer (index, pos, rot);
		}
        return inst;
    }

    public void ReturnToPool (Transform instance, float time = 0.0f) {
		if (NetworkServer.active || client == null) {
			if (time > 0) {
        		StartCoroutine (TimedReturn (instance, time));
			} else {
        		StartCoroutine (DelayedReturn (instance));
			}
			NetworkServer.UnSpawn (instance.gameObject);
		} else {
			Debug.LogWarning ("Return to pool should only be called by the server or if offline");
		}
    }

	private GameObject SpawnPoolable (Vector3 position, NetworkHash128 assetId) {
		for (int i = 0; i < localInstances.Count; i ++) {
			if (localInstances[i].position == position && 
				localInstances[i].GetComponent<NetworkIdentity>().assetId.ToString() == assetId.ToString()) 
			{
				if (i > 0) {
					Debug.LogWarning ("The local instance we found as not the first one.  That's odd.");
				}
				Transform inst = localInstances[i];
				localInstances.RemoveAt (i);
				if (callbacks[i] != null) {
					callbacks[i](inst);
				}
				callbacks.RemoveAt (i);
				return inst.gameObject;
			}
		}

		// Not one of ours
		Transform newInst = InstantiateInternal (assetIdToIndex[assetId], position, Quaternion.identity);
		return newInst.gameObject;
	}

	private void UnspawnPoolable (GameObject go) {
		// Have we already retpooled this one?  If not then we need to 
		if (go.activeSelf) {
			StartCoroutine (DelayedReturn (go.transform));
		}
	}

    private int PrefabIndex (Transform prefab) {
		NetworkIdentity id = prefab.GetComponent<NetworkIdentity>();
		if (id != null) {
			return assetIdToIndex[id.assetId];
		}
		for (int i = 0; i < poolablePrefabs.Length; i ++) {
			if (prefab == poolablePrefabs[i]) {
				return i;
			}
		}
		return -1;
    }

    private Transform InstantiateInternal (int index, Vector3 pos, Quaternion rot) {
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
        inst.gameObject.SetActive (true);
        return inst;
    }

	private void SendRemoteInstanceToServer (int index, Vector3 pos, Quaternion rot) {
		RemoteInstanceMessage msg = new RemoteInstanceMessage ();
		msg.index = index;
		msg.position = pos;
		msg.rotation = rot;
		client.Send (RemoteInstanceMsg, msg);	
	}

	private void ReceiveRemoteInstanceFromClient (NetworkMessage msg) {
		RemoteInstanceMessage instMsg = msg.ReadMessage<RemoteInstanceMessage>();
		RemoteInstance (instMsg.index, instMsg.position, instMsg.rotation);
	}

    private void RemoteInstance (int index, Vector3 pos, Quaternion rot) {
        Transform inst = InstantiateInternal (index, pos, rot);
		NetworkServer.Spawn (inst.gameObject);
    }

    IEnumerator TimedReturn (Transform instance, float time) {
        yield return new WaitForSeconds (time);
        StartCoroutine (DelayedReturn (instance));
    }

    IEnumerator DelayedReturn (Transform instance) {
        yield return new WaitForEndOfFrame ();
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
