using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PseudoPooler : MonoBehaviour
{
    private void Start()
    {
        SendMessage("PoolStart", SendMessageOptions.DontRequireReceiver);
    }

    private void OnDestroy()
    {
        SendMessage("PoolReturn", SendMessageOptions.DontRequireReceiver);
    }
}
