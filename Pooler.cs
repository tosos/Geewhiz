using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class Pooler : MonoBehaviour {

	public delegate void OnPoolInstantiated (Transform instance);
	public event OnPoolInstantiated onPoolInstantiated;

    public Transform[] poolablePrefabs;
    private Queue<Transform>[] pooledInstances;
	private Dictionary<NetworkHash128, int> assetIdToIndex;
    public int minPooledIds = 5;

	public delegate void NetworkInstantiateDelegate (Transform inst);

	private const short RemoteInstanceMsg = 1010;
    public class RemoteInstanceMessage : MessageBase
    {
        public int index;
		public string tag;
		public int layer;
        public Vector3 position;
        public Quaternion rotation;
    }
	private List<RemoteInstanceMessage> queuedInstances;

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

	public Transform GetPrefab (Transform inst) {
		Poolable pool = inst.GetComponent<Poolable> ();	
		return pool != null ? poolablePrefabs[pool.prefabIndex] : null;
	}

    void Awake () {
        if (_instance != null) {
            Debug.LogError ("Instance should be null");
        }
        _instance = this;

        pooledInstances = new Queue<Transform>[poolablePrefabs.Length];
		// TODO allow pre-warming the instances

		assetIdToIndex = new Dictionary<NetworkHash128, int> ();
		for (int i = 0; i < poolablePrefabs.Length; i ++) {
			NetworkHash128 assetId = poolablePrefabs[i].GetComponent<NetworkIdentity>().assetId;
			ClientScene.RegisterSpawnHandler (assetId, SpawnPoolable, UnspawnPoolable);
			assetIdToIndex[assetId] = i;
		}

		// These are used to hold instances we create until we hear back from the server
		localInstances = new List<Transform> ();
		callbacks = new List<NetworkInstantiateDelegate> ();
		queuedInstances = new List<RemoteInstanceMessage> ();

		if (NetworkServer.active) {
			NetworkServer.RegisterHandler (RemoteInstanceMsg, ReceiveRemoteInstanceFromClient);
		}

		if (NetworkClient.active) {
			NetworkManager.singleton.client.RegisterHandler (RemoteInstanceMsg, ReceiveRemoteInstanceFromServer);
		}
    }

    void OnDestroy () {
        _instance = null;
		for (int i = 0; i < poolablePrefabs.Length; i ++) {
			NetworkHash128 assetId = poolablePrefabs[i].GetComponent<NetworkIdentity>().assetId;
			ClientScene.UnregisterSpawnHandler (assetId);
		}
    }

    public Transform InstantiateFromPool (Transform prefab, Vector3 pos, Quaternion rot) {
        int index = PrefabIndex (prefab);
        if (index < 0) {
            Debug.LogError ("Prefab " + prefab.name + " is not in poolable set");
            return null;
        }
        return InstantiateInternal (index, prefab.gameObject.tag, prefab.gameObject.layer, pos, rot);
    }

    public Transform NetworkInstantiateFromPool (Transform prefab, Vector3 pos, Quaternion rot, NetworkInstantiateDelegate func  = null) {
        int index = PrefabIndex (prefab);
        if (index < 0) {
            Debug.LogError ("Prefab " + prefab.name + " is not in poolable set");
            return null;
        }
        Transform inst = InstantiateInternal (index, prefab.gameObject.tag, prefab.gameObject.layer, pos, rot);
		if (NetworkServer.active) {
			if (inst.GetComponent<NetworkIdentity>() == null) {
				Debug.LogError ("pooler Trying to instantiate a prefab " + prefab.gameObject.name + " without id", prefab);
			}
			Debug.Log ("pooler Sending RPC " + PrefabIndex(prefab) + " at " + pos);
			
			SendRemoteInstanceToClients (PrefabIndex(prefab), prefab.gameObject.tag, prefab.gameObject.layer, pos, rot);
			NetworkServer.Spawn(inst.gameObject);
		} else if (NetworkClient.active != null) {
			localInstances.Add (inst);
			callbacks.Add (func);
			SendRemoteInstanceToServer (index, prefab.gameObject.tag, prefab.gameObject.layer, pos, rot);
		}
        return inst;
    }

    public void ReturnToPool (Transform instance, float time = 0.0f) {
		if (time > 0) {
       		StartCoroutine (TimedReturn (instance, time));
		} else {
       		StartCoroutine (DelayedReturn (instance));
		}
		if (NetworkServer.active) {
			NetworkServer.UnSpawn (instance.gameObject);
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

		Debug.Log ("pooler Looking for " + assetIdToIndex[assetId] + " at " + position);
		for (int i = 0; i < queuedInstances.Count; i ++) {
			// Debug.Log ("queuedInfo " + assetIdToIndex[assetId] + " at " + queuedInfo[i].position);
			if (queuedInstances[i].index == assetIdToIndex[assetId] && queuedInstances[i].position == position) {
				Transform newInst = InstantiateInternal (assetIdToIndex[assetId], queuedInstances[i].tag, queuedInstances[i].layer, position, Quaternion.identity);
				return newInst.gameObject;
			}
		}

		Debug.LogWarning ("No queued info available for Spawned prefab");
		return null;
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

    private Transform InstantiateInternal (int index, string tag, int layer, Vector3 pos, Quaternion rot) {
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
			if (!NetworkServer.active) {
				Visuals visuals = inst.GetComponent<Visuals>();
				if (visuals != null) {
					visuals.LoadVisuals ();
				}
			}
        } else {
            inst = pooledInstances[index].Dequeue ();
        }
        inst.parent = null;
		inst.gameObject.tag = tag;
		inst.gameObject.layer = layer;
        inst.position = pos;
        inst.rotation = rot;
        inst.gameObject.SetActive (true);

		if (onPoolInstantiated != null)
			onPoolInstantiated (inst);
        return inst;
    }

	private void SendRemoteInstanceToServer (int index, string tag, int layer, Vector3 pos, Quaternion rot) {
		Debug.Log ("pooler Sending remote instance to the server");
		RemoteInstanceMessage msg = new RemoteInstanceMessage ();
		msg.index = index;
		msg.tag = tag;
		msg.layer = layer;
		msg.position = pos;
		msg.rotation = rot;
		NetworkManager.singleton.client.SendByChannel (RemoteInstanceMsg, msg, 0);
	}

	private void ReceiveRemoteInstanceFromClient (NetworkMessage msg) {
		Debug.Log ("Received a remote intance from the client");
		RemoteInstanceMessage instMsg = msg.ReadMessage<RemoteInstanceMessage>();
		RemoteInstance (msg.conn, instMsg);
	}

	private void SendRemoteInstanceToClients (int index, string tag, int layer, Vector3 pos, Quaternion rot) {
		RemoteInstanceMessage msg = new RemoteInstanceMessage ();
		msg.index = index;
		msg.tag = tag;
		msg.layer = layer;
		msg.position = pos;
		msg.rotation = rot;
		NetworkServer.SendByChannelToAll (RemoteInstanceMsg, msg, 0);
	}

	private void ReceiveRemoteInstanceFromServer (NetworkMessage msg) {
		Debug.Log ("Received a remote intance from the client");
		RemoteInstanceMessage queuedInst = msg.ReadMessage<RemoteInstanceMessage>();
		queuedInstances.Add (queuedInst);
	}


    private void RemoteInstance (NetworkConnection conn, RemoteInstanceMessage msg) {
        Transform inst = InstantiateInternal (msg.index, msg.tag, msg.layer, msg.position, msg.rotation);
		SendRemoteInstanceToClients (msg.index, msg.tag, msg.layer, msg.position, msg.rotation);
		NetworkServer.SpawnWithClientAuthority (inst.gameObject, conn);
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
