using UnityEngine;
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

    void Awake () {
        if (_instance != null) {
            Debug.LogError ("Instance should be null");
        }
        _instance = this;

        serialRecv = new Dictionary< string, List<GameObject> > ();
        parallelRecv = new Dictionary< string, List<GameObject> > ();
    }

    void OnDestroy () {
        _instance = null;
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

    [RPC]
    public void RPCDispatchE (string message) {
        Dispatch (message);
    }

    [RPC]
    public void RPCDispatchI (string message, int parameter) {
        Dispatch (message, parameter);
    }

    [RPC]
    public void RPCDispatchF (string message, float parameter) {
        Dispatch (message, parameter);
    }

    [RPC]
    public void RPCDispatchS (string message, string parameter) {
        Dispatch (message, parameter);
    }

    [RPC]
    public void RPCDispatchP (string message, NetworkPlayer parameter) {
        Dispatch (message, parameter);
    }

    [RPC]
    public void RPCDispatchID (string message, NetworkViewID parameter) {
        Dispatch (message, parameter);
    }

    [RPC]
    public void RPCDispatchV (string message, Vector3 parameter) {
        Dispatch (message, parameter);
    }

    [RPC]
    public void RPCDispatchQ (string message, Quaternion parameter) {
        Dispatch (message, parameter);
    }

    public void RemoteDispatch (string message, object parameter = null)
    {
        Dispatch (message, parameter);
        if (Network.peerType != NetworkPeerType.Disconnected && GetComponent<NetworkView>() != null) {
            if (parameter == null) {
                GetComponent<NetworkView>().RPC ("RPCDispatchE", RPCMode.Others, message);
            } else if (parameter is int) {
                GetComponent<NetworkView>().RPC ("RPCDispatchI", RPCMode.Others, message, parameter);
            } else if (parameter is float) {
                GetComponent<NetworkView>().RPC ("RPCDispatchF", RPCMode.Others, message, parameter);
            } else if (parameter is string) {
                GetComponent<NetworkView>().RPC ("RPCDispatchS", RPCMode.Others, message, parameter);
            } else if (parameter is NetworkPlayer) {
                GetComponent<NetworkView>().RPC ("RPCDispatchP", RPCMode.Others, message, parameter);
            } else if (parameter is NetworkViewID) {
                GetComponent<NetworkView>().RPC ("RPCDispatchID", RPCMode.Others, message, parameter);
            } else if (parameter is Vector3) {
                GetComponent<NetworkView>().RPC ("RPCDispatchV", RPCMode.Others, message, parameter);
            } else if (parameter is Quaternion) {
                GetComponent<NetworkView>().RPC ("RPCDispatchQ", RPCMode.Others, message, parameter);
            } else {
                Debug.Log ("Can't dispatch a non RPCable parameter");
            }
        }
    }

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
