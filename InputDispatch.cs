using UnityEngine;
using System.Collections;

public class InputDispatch : MonoBehaviour 
{
	public bool CheckAnyKey;
	public string[] buttons;

	void Start () {
		if (!CheckAnyKey && buttons.Length == 0) {
			enabled = false;
		}	
	}
	
	// Update is called once per frame
	void Update () {
        if (CheckAnyKey && Input.anyKeyDown) {
            Dispatcher.instance.Dispatch ("AnyKeyDown", ""); 
        }
	
		foreach (string button in buttons) {
			if (Input.GetButtonDown (button)) {
				Dispatcher.instance.Dispatch ("ButtonDown" + button);
			}
			if (Input.GetButtonUp (button)) {
				Dispatcher.instance.Dispatch ("ButtonUp" + button);
			}
		}
	}
}
