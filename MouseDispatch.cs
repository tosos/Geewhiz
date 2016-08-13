using UnityEngine;
using UnityEngine.EventSystems;

using System.Collections;

public class MouseDispatch : MonoBehaviour {

	// Update is called once per frame
	void Update () {
		if (EventSystem.current.IsPointerOverGameObject()) {
			return;
		}

		if (EventSystem.current.currentSelectedGameObject == null) {
        	for (int i = 0; i < 3; i ++) {
	        	if (Input.GetMouseButtonDown (i)) {
                	Dispatcher.instance.Dispatch ("MouseButtonDown", i);
            	}
	        	if (Input.GetMouseButtonUp (i)) {
                	Dispatcher.instance.Dispatch ("MouseButtonUp", i);
            	}
        	}
		}
	}
}
