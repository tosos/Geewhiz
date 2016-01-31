using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class Dispatcher : MonoBehaviour {

    private Dictionary< string, List<GameObject> > serialRecv;
    private Dictionary< string, List<GameObject> > parallelRecv;

    public string debugMessage = "";

    static private Dispatcher _instance = null;
    static public Dispatcher instance {
        get { return _instance; }
    }

	private const short DispatchEMessageId = 1000;
	private const short DispatchIMessageId = 1001;
	private const short DispatchFMessageId = 1002;
	private const short DispatchSMessageId = 1003;
	private const short DispatchVMessageId = 1004;
	private const short DispatchQMessageId = 1005;

	private class EMessage : MessageBase {
		public string message;
	}

	private class IMessage : MessageBase {
		public string message;
		public int parameter;
	}

	private class FMessage : MessageBase {
		public string message;
		public float parameter;
	}

	private class SMessage : MessageBase {
		public string message;
		public string parameter;
	}

	private class VMessage : MessageBase {
		public string message;
		public Vector3 parameter;
	}

	private class QMessage : MessageBase {
		public string message;
		public Quaternion parameter;
	}


    void Awake () {
        if (_instance != null) {
            Debug.LogError ("Instance should be null");
        }
        _instance = this;

        serialRecv = new Dictionary< string, List<GameObject> > ();
        parallelRecv = new Dictionary< string, List<GameObject> > ();

		if (NetworkServer.active) {
			NetworkServer.RegisterHandler (DispatchEMessageId, DispatchEMessage);
			NetworkServer.RegisterHandler (DispatchIMessageId, DispatchIMessage);
			NetworkServer.RegisterHandler (DispatchFMessageId, DispatchFMessage);
			NetworkServer.RegisterHandler (DispatchSMessageId, DispatchSMessage);
			NetworkServer.RegisterHandler (DispatchVMessageId, DispatchVMessage);
			NetworkServer.RegisterHandler (DispatchQMessageId, DispatchQMessage);
		}
    }

    void OnDestroy () {
        _instance = null;
		if (NetworkServer.active) {
			NetworkServer.UnregisterHandler (DispatchEMessageId);
			NetworkServer.UnregisterHandler (DispatchIMessageId);
			NetworkServer.UnregisterHandler (DispatchFMessageId);
			NetworkServer.UnregisterHandler (DispatchSMessageId);
			NetworkServer.UnregisterHandler (DispatchVMessageId);
			NetworkServer.UnregisterHandler (DispatchQMessageId);
		}
    }

	private NetworkClient client;

	public void SetupClientConnection (NetworkClient c) {
		client = c;
		client.RegisterHandler (DispatchEMessageId, DispatchEMessage);
		client.RegisterHandler (DispatchIMessageId, DispatchIMessage);
		client.RegisterHandler (DispatchFMessageId, DispatchFMessage);
		client.RegisterHandler (DispatchSMessageId, DispatchSMessage);
		client.RegisterHandler (DispatchVMessageId, DispatchVMessage);
		client.RegisterHandler (DispatchQMessageId, DispatchQMessage);
	}

	public void RemoveClientConnection () {
		if (client != null) {
			client.UnregisterHandler (DispatchEMessageId);
			client.UnregisterHandler (DispatchIMessageId);
			client.UnregisterHandler (DispatchFMessageId);
			client.UnregisterHandler (DispatchSMessageId);
			client.UnregisterHandler (DispatchVMessageId);
			client.UnregisterHandler (DispatchQMessageId);
		}
		client = null;
	}

    public void Dispatch (string message, object parameter = null) {
        if (serialRecv.ContainsKey (message) && serialRecv[message].Count > 0) {
            if (serialRecv[message][0] != null && serialRecv[message][0].activeSelf) {
                serialRecv[message][0].SendMessage (message, parameter);
            } else {
                serialRecv[message].RemoveAt (0);
            }
        }

        if (parallelRecv.ContainsKey (message)) {
            foreach (GameObject go in parallelRecv[message]) {
                if (go != null) {
                    go.SendMessage (message, parameter);
                }
            }
        }
    }

    public void DispatchEMessage (NetworkMessage msg) {
		EMessage emsg = msg.ReadMessage<EMessage> ();
       	Dispatch (emsg.message);
		if (NetworkServer.active) {
			// Send it to everyone but the sender
			for (int i = 0; i < NetworkServer.connections.Count; i ++) {
				if (NetworkServer.connections[i] != null && NetworkServer.connections[i] != msg.conn) {
					NetworkServer.SendToClient (NetworkServer.connections[i].connectionId, DispatchEMessageId, emsg);
				}
			}
		}
    }

    public void DispatchIMessage (NetworkMessage msg) {
		IMessage imsg = msg.ReadMessage<IMessage> ();
        Dispatch (imsg.message, imsg.parameter);
		if (NetworkServer.active) {
			// Send it to everyone but the sender
			for (int i = 0; i < NetworkServer.connections.Count; i ++) {
				if (NetworkServer.connections[i] != null && NetworkServer.connections[i] != msg.conn) {
					NetworkServer.SendToClient (NetworkServer.connections[i].connectionId, DispatchIMessageId, imsg);
				}
			}
		}
    }

    public void DispatchFMessage (NetworkMessage msg) {
		FMessage fmsg = msg.ReadMessage<FMessage> ();
        Dispatch (fmsg.message, fmsg.parameter);
		if (NetworkServer.active) {
			// Send it to everyone but the sender
			for (int i = 0; i < NetworkServer.connections.Count; i ++) {
				if (NetworkServer.connections[i] != null && NetworkServer.connections[i] != msg.conn) {
					NetworkServer.SendToClient (NetworkServer.connections[i].connectionId, DispatchFMessageId, fmsg);
				}
			}
		}
    }

    public void DispatchSMessage (NetworkMessage msg) {
		SMessage smsg = msg.ReadMessage<SMessage> ();
        Dispatch (smsg.message, smsg.parameter);
		if (NetworkServer.active) {
			// Send it to everyone but the sender
			for (int i = 0; i < NetworkServer.connections.Count; i ++) {
				if (NetworkServer.connections[i] != null && NetworkServer.connections[i] != msg.conn) {
					NetworkServer.SendToClient (NetworkServer.connections[i].connectionId, DispatchSMessageId, smsg);
				}
			}
		}
    }

    public void DispatchVMessage (NetworkMessage msg) {
		VMessage vmsg = msg.ReadMessage<VMessage> ();
        Dispatch (vmsg.message, vmsg.parameter);
		if (NetworkServer.active) {
			// Send it to everyone but the sender
			for (int i = 0; i < NetworkServer.connections.Count; i ++) {
				if (NetworkServer.connections[i] != null && NetworkServer.connections[i] != msg.conn) {
					NetworkServer.SendToClient (NetworkServer.connections[i].connectionId, DispatchVMessageId, vmsg);
				}
			}
		}
    }

    public void DispatchQMessage (NetworkMessage msg) {
		QMessage qmsg = msg.ReadMessage<QMessage> ();
        Dispatch (qmsg.message, qmsg.parameter);
		if (NetworkServer.active) {
			// Send it to everyone but the sender
			for (int i = 0; i < NetworkServer.connections.Count; i ++) {
				if (NetworkServer.connections[i] != null && NetworkServer.connections[i] != msg.conn) {
					NetworkServer.SendToClient (NetworkServer.connections[i].connectionId, DispatchQMessageId, qmsg);
				}
			}
		}
    }

	// TODO this can probably be improved...
    public void RemoteDispatch (string message, object parameter = null)
    {
        Dispatch (message, parameter);

		short id;
		MessageBase msg = ParseParameterToMessage (message, parameter, out id);
		if (msg == null) return;
		if (NetworkServer.active) {
			NetworkServer.SendToAll (id, msg);
		} else if (client != null) {
			client.Send (id, msg);
		}
    }


/* TODO
    public void RemoteDispatch (string message, NetworkPlayer player, object parameter = null)
    {
        if (Network.peerType != NetworkPeerType.Disconnected) {
            if (GetComponent<NetworkView>() == null) {
                Debug.LogError ("Cannot remote dispatch.  " +
                    "Need to attach a networkView to the Hub where Dispatch is attached");
            } else {
                if (parameter == null) {
                    GetComponent<NetworkView>().RPC ("RPCDispatchE", player, message);
                } else if (parameter is int) {
                    GetComponent<NetworkView>().RPC ("RPCDispatchI", player, message, parameter);
                } else if (parameter is float) {
                    GetComponent<NetworkView>().RPC ("RPCDispatchF", player, message, parameter);
                } else if (parameter is string) {
                    GetComponent<NetworkView>().RPC ("RPCDispatchS", player, message, parameter);
                } else if (parameter is NetworkPlayer) {
                    GetComponent<NetworkView>().RPC ("RPCDispatchP", player, message, parameter);
                } else if (parameter is NetworkViewID) {
                    GetComponent<NetworkView>().RPC ("RPCDispatchID", player, message, parameter);
                } else if (parameter is Vector3) {
                    GetComponent<NetworkView>().RPC ("RPCDispatchV", player, message, parameter);
                } else if (parameter is Quaternion) {
                    GetComponent<NetworkView>().RPC ("RPCDispatchQ", player, message, parameter);
                } else {
                    Debug.Log ("Can't dispatch a non RPCable parameter");
                }
            }
        }
    }
*/
	private MessageBase ParseParameterToMessage (string message, object parameter, out short id) {
		MessageBase msgToSend = null;
        if (parameter == null) {
			id = DispatchEMessageId;
            EMessage emsg = new EMessage ();
			emsg.message = message;
			msgToSend = emsg;
        } else if (parameter is int) {
			id = DispatchIMessageId;
            IMessage imsg = new IMessage ();
			imsg.message = message;
			imsg.parameter = (int)parameter;
			msgToSend = imsg;
        } else if (parameter is float) {
			id = DispatchFMessageId;
            FMessage fmsg = new FMessage ();
			fmsg.message = message;
			fmsg.parameter = (float)parameter;
			msgToSend = fmsg;
        } else if (parameter is string) {
			id = DispatchSMessageId;
            SMessage smsg = new SMessage ();
			smsg.message = message;
			smsg.parameter = parameter as string;
			msgToSend = smsg;
        } else if (parameter is Vector3) {
			id = DispatchVMessageId;
            VMessage vmsg = new VMessage ();
			vmsg.message = message;
			vmsg.parameter = (Vector3)parameter;
			msgToSend = vmsg;
        } else if (parameter is Quaternion) {
			id = DispatchQMessageId;
            QMessage qmsg = new QMessage ();
			qmsg.message = message;
			qmsg.parameter = (Quaternion)parameter;
			msgToSend = qmsg;
        } else {
			id = -1;
            Debug.LogError ("Can't dispatch a non RPCable parameter");
        }
		return msgToSend;
	}

    public void Register (string message, GameObject obj, bool isParallel = true) {
        if (isParallel) {
            if (!parallelRecv.ContainsKey (message)) {
                parallelRecv.Add (message, new List<GameObject> ());
            } 
            if (!parallelRecv[message].Contains (obj)) { 
                parallelRecv[message].Add(obj);
            }
        } else {
            if (!serialRecv.ContainsKey (message)) {
                serialRecv.Add (message, new List<GameObject> ());
            } 
            if (!serialRecv[message].Contains (obj)) {
                serialRecv[message].Insert(0, obj);
            }
            UpdateDebug ();
        }
    }

    public void Unregister (string message, GameObject obj) {
        if (parallelRecv.ContainsKey (message)) {
            parallelRecv[message].Remove (obj);
        }
        if (serialRecv.ContainsKey (message)) {
            serialRecv[message].Remove (obj);
            UpdateDebug ();
        }
    }

    void UpdateDebug () {
        if (debugMessage != "" && GetComponent<GUIText>() != null) {
            GetComponent<GUIText>().text = "";
            foreach (GameObject go in serialRecv[debugMessage]) {
                GetComponent<GUIText>().text += go.name + "\n";
            }
        }
    }
}
