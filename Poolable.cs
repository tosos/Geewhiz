using UnityEngine;
using System.Collections;

public class Poolable : MonoBehaviour {
    public int prefabIndex;
    private bool needsStart = true;
    
    // This script should be configured to run before all others.
    void Update () {
        if (needsStart) {
            BroadcastMessage ("PoolStart", SendMessageOptions.DontRequireReceiver);
            needsStart = false;
            enabled = false;
        }
    }

    // PoolStart is here, but PoolReturn is called in Pooler
    void PoolReturn () {
        needsStart = true;
        enabled = true;

        // Reset components to their default values when going back to the pool
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>()) {
            if (!rb.isKinematic) {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
