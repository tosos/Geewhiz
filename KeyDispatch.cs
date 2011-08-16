using UnityEngine;
using System.Collections;

public class KeyDispatch : MonoBehaviour {
    public bool CheckAnyKey;
    public KeyCode[] keys;

	void Start () {
	    if (!CheckAnyKey && keys.Length == 0) {
            enabled = false;
        }
	}
	
	void Update () {
        if (CheckAnyKey && Input.anyKeyDown) {
            Dispatcher.GetInstance ().Dispatch ("AnyKeyDown", ""); 
        }
        foreach (KeyCode key in keys) {
            if (Input.GetKeyDown (key)) {
                Dispatcher.GetInstance ().Dispatch ("KeyDown" + key.ToString(), "");
            }
            if (Input.GetKeyUp (key)) {
                Dispatcher.GetInstance ().Dispatch ("KeyUp" + key.ToString(), "");
            }
        }
	}
}
