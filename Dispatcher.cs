using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Dispatcher : MonoBehaviour {

    private Dictionary< string, Stack<GameObject> > serialRecv;
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
            Destroy (instance.gameObject);
        }
        instance = null;

        serialRecv = new Dictionary< string, Stack<GameObject> > ();
        parallelRecv = new Dictionary< string, List<GameObject> > ();
    }

    public void Dispatch (string message, object parameter) {
        Debug.Log ("dispatching " + message);
        if (serialRecv.ContainsKey (message)) {
            serialRecv[message].Peek().SendMessage (message, parameter);
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
                serialRecv.Add (message, new Stack<GameObject> ());
            } 
            serialRecv[message].Push(obj);
        }
    }

    public void Unregister (string message, GameObject obj) {
        if (parallelRecv.ContainsKey (message)) {
            parallelRecv[message].Remove (obj);
        }
        if (serialRecv.ContainsKey (message)) {
            if (serialRecv[message].Peek () == obj) {
                serialRecv[message].Pop ();
            } else {
                Debug.LogError ("Trying to unregister serial receiver out of order");
            }
        }
    }
}
