using UnityEngine;
using System.Collections;

public class NGUIMouseDispatch : MonoBehaviour {

    void Start () {
        UICamera.fallThrough = gameObject;
    }

    public void OnPress () {
        for (int i = 0; i < 3; i ++) {
	        if (Input.GetMouseButtonDown (i)) {
                Dispatcher.instance.Dispatch ("MouseButtonDown", i);
            }
        }
    }

    public void OnSelect () {
        for (int i = 0; i < 3; i ++) {
	        if (Input.GetMouseButtonDown (i)) {
                Dispatcher.instance.Dispatch ("MouseButtonUp", i);
            }
        }
    }
}
