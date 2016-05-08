﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Visuals : MonoBehaviour 
{
	public RuntimeAnimatorController controller;
	public Avatar avatar;
	public string[] visualAssets;
	static private Dictionary<string, GameObject> resourceCache;

	public void LoadVisuals () {
		if (resourceCache == null) {
			resourceCache = new Dictionary<string, GameObject> ();
		}

		for (int i = 0; i < visualAssets.Length; i ++) {
			if (!resourceCache.ContainsKey (visualAssets[i])) {
				resourceCache[visualAssets[i]] = (GameObject) Resources.Load (visualAssets[i]);
			}
			GameObject prefab = resourceCache[visualAssets[i]];
			GameObject inst = (GameObject) Instantiate (prefab, Vector3.zero, Quaternion.identity);
			inst.transform.parent = transform;
			inst.transform.localPosition = prefab.transform.position;
			inst.transform.localRotation = prefab.transform.rotation;
		}

		Animator animator = gameObject.GetComponent<Animator>();
		if (animator == null) {
			animator = gameObject.AddComponent<Animator>();
		}
		animator.avatar = avatar;
		animator.runtimeAnimatorController = controller;
	}
}
