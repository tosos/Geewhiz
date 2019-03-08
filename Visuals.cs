using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Visuals : MonoBehaviour 
{
	// public RuntimeAnimatorController controller;
	// public Avatar avatar;
	public string[] visualAssets;
	static private Dictionary<string, GameObject> resourceCache;
	private List<GameObject> visualInsts;
	private bool visualsLoaded = false;

	public void LoadVisuals () {
		if (visualsLoaded) {
			return;
		}

		if (resourceCache == null) {
			resourceCache = new Dictionary<string, GameObject> ();
		}

		visualInsts = new List<GameObject> ();

		for (int i = 0; i < visualAssets.Length; i ++) {
			if (!resourceCache.ContainsKey (visualAssets[i])) {
				resourceCache[visualAssets[i]] = (GameObject) Resources.Load (visualAssets[i]);
			}
			GameObject prefab = resourceCache[visualAssets[i]];
			GameObject inst = (GameObject) Instantiate (prefab, Vector3.zero, Quaternion.identity);
			inst.name = prefab.name;
			inst.transform.parent = transform;
			inst.transform.localPosition = prefab.transform.position;
			inst.transform.localRotation = prefab.transform.rotation;
			inst.transform.localScale = prefab.transform.localScale;
			visualInsts.Add (inst);
		}

		visualsLoaded = true;
	}

	public void EnableVisuals () {
		Debug.Log ("Enabling construction visuals");
		for (int i = 0; i < visualInsts.Count; i ++) {
			visualInsts[i].SetActive (true);
		}
	}
	public void DisableVisuals () {
		Debug.Log ("Disabing construction visuals");
		for (int i = 0; i < visualInsts.Count; i ++) {
			visualInsts[i].SetActive (false);
		}
	}

}
