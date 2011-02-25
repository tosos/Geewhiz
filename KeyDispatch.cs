using UnityEngine;
using System.Collections;

public class KeyDispatch : MonoBehaviour {
    public KeyCode[] keys;

	// Use this for initialization
	void Start () {
	    if (keys.Length == 0) {
            enabled = false;
        }
	}
	
	// Update is called once per frame
	void Update () {
        foreach (KeyCode key in keys) {
            if (Input.GetKeyDown (key)) {
                Dispatcher.GetInstance ().Dispatch ("KeyDown", key.ToString());
            }
            if (Input.GetKeyUp (key)) {
                Dispatcher.GetInstance ().Dispatch ("KeyUp", key.ToString());
            }
        }
	}
}
