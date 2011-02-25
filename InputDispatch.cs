using UnityEngine;
using System.Collections;

public class InputDispatch : MonoBehaviour {
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
                Dispatcher.GetInstance ().Dispatch (key.ToString(), true);
            }
            if (Input.GetKeyUp (key)) {
                Dispatcher.GetInstance ().Dispatch (key.ToString(), false);
            }
        }
	}
}
