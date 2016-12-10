using UnityEngine;
using System.Collections;

public class InputDispatch : MonoBehaviour 
{
	public enum Modifier {
		None = 0x00,
		Shift = 0x01,
		Ctrl = 0x02,
		CtrlShift = 0x03,
		Alt = 0x04,
		ShiftAlt = 0x05,
		CtrlAlt = 0x06,
		CtrlAltShift = 0x07
	}
	[System.Serializable]
	public class Definition {
		public string button;
		public Modifier mod;
	}
	public Definition[] definitions;

	public bool CheckAnyKey;

	void Start () {
		if (!CheckAnyKey && definitions.Length == 0) {
			enabled = false;
		}	
	}
	
	// Update is called once per frame
	void Update () {
        if (CheckAnyKey && Input.anyKeyDown) {
            Dispatcher.instance.Dispatch ("AnyKeyDown", ""); 
        }

		int modVal = 0;
		if (Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift)) {
			modVal |= (int)Modifier.Shift;
		}
		if (Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl)) {
			modVal |= (int)Modifier.Ctrl;
		}
		if (Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt)) {
			modVal |= (int)Modifier.Alt;
		}
	
		foreach (Definition def in definitions) {
			if ((int)def.mod != modVal) {
				continue;
			}

			if (Input.GetButtonDown (def.button)) {
				Dispatcher.instance.Dispatch ("ButtonDown" + def.button);
			}
			if (Input.GetButtonUp (def.button)) {
				Dispatcher.instance.Dispatch ("ButtonUp" + def.button);
			}
		}
	}
}
