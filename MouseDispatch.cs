using UnityEngine;
using System.Collections;

public class MouseDispatch : MonoBehaviour {

	// Update is called once per frame
	void Update () {
        for (int i = 0; i < 3; i ++) {
	        if (Input.GetMouseButtonDown (i)) {
                Dispatcher.GetInstance ().Dispatch ("MouseButtonDown", i);
            }
	        if (Input.GetMouseButtonUp (i)) {
                Dispatcher.GetInstance ().Dispatch ("MouseButtonUp", i);
            }
        }
	}
}
