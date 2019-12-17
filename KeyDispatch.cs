using UnityEngine;
using System.Collections;

public class KeyDispatch : MonoBehaviour
{
    public bool CheckAnyKey;
    public KeyCode[] keys;

    void Start()
    {
        if (!CheckAnyKey && keys.Length == 0) { enabled = false; }
    }

    void Update()
    {
        if (CheckAnyKey && Input.anyKeyDown) { Dispatcher.instance.Dispatch("AnyKeyDown", ""); }
        foreach(KeyCode key in keys)
        {
            if (Input.GetKeyDown(key)) {
                Dispatcher.instance.Dispatch("KeyDown" + key.ToString(), "");
            }
            if (Input.GetKeyUp(key)) { Dispatcher.instance.Dispatch("KeyUp" + key.ToString(), ""); }
        }
    }
}
