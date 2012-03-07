using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Dispatcher : MonoBehaviour {

    private Dictionary< string, List<GameObject> > serialRecv;
    private Dictionary< string, List<GameObject> > parallelRecv;

    public string debugMessage = "";

    static private Dispatcher instance = null;
    static public Dispatcher GetInstance () {
        if (instance == null) {
            instance = (Dispatcher) FindObjectOfType (typeof(Dispatcher));
        }
        return instance;
    }

    void Awake () {
        if (instance != null) {
            Debug.LogError ("Instance should be null");
        }
        instance = this;

        serialRecv = new Dictionary< string, List<GameObject> > ();
        parallelRecv = new Dictionary< string, List<GameObject> > ();
    }

    void OnDestroy () {
        instance = null;
    }

    [RPC]
    public void Dispatch (string message, object parameter = null) {
        if (serialRecv.ContainsKey (message)) {
            if (serialRecv[message][0] != null) {
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

    public void RemoteDispatch (string message, object parameter = null)
    {
        Dispatch (message, parameter);
        if (Network.peerType != NetworkPeerType.Disconnected && networkView != null) {
            networkView.RPC ("Dispatch", RPCMode.Others, message, parameter);
        }
    }

    public void RemoteDispatch (string message, NetworkPlayer player, object parameter = null)
    {
        if (Network.peerType != NetworkPeerType.Disconnected) {
            if (networkView == null) {
                Debug.LogError ("Cannot remote dispatch.  " +
                    "Need to attach a networkView to the Hub where Dispatch is attached");
            } else {
                networkView.RPC ("Dispatch", player, message, parameter);
            }
        }
    }

    public void Register (string message, GameObject obj, bool isParallel = true) {
        if (isParallel) {
            if (!parallelRecv.ContainsKey (message)) {
                parallelRecv.Add (message, new List<GameObject> ());
            } 
            parallelRecv[message].Add(obj);
        } else {
            if (!serialRecv.ContainsKey (message)) {
                serialRecv.Add (message, new List<GameObject> ());
            } 
            serialRecv[message].Insert(0, obj);
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
        if (debugMessage != "" && guiText != null) {
            guiText.text = "";
            foreach (GameObject go in serialRecv[debugMessage]) {
                guiText.text += go.name + "\n";
            }
        }
    }
}
