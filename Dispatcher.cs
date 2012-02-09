using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Dispatcher : MonoBehaviour {

    private Dictionary< string, List<GameObject> > serialRecv;
    private Dictionary< string, List<GameObject> > parallelRecv;

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

    public void Dispatch (string message, object parameter = null) {
        if (serialRecv.ContainsKey (message)) {
            serialRecv[message][0].SendMessage (message, parameter);
        }

        if (parallelRecv.ContainsKey (message)) {
            foreach (GameObject go in parallelRecv[message]) {
                go.SendMessage (message, parameter);
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
        }
    }

    public void Unregister (string message, GameObject obj) {
        if (parallelRecv.ContainsKey (message)) {
            parallelRecv[message].Remove (obj);
        }
        if (serialRecv.ContainsKey (message)) {
            serialRecv[message].Remove (obj);
        }
    }
}
