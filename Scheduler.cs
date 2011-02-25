using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class TimerMessage {
    public string message;
    public float secondsBetween;
    [HideInInspector]
    public float nextTime;
};

public class Scheduler : MonoBehaviour {

    public TimerMessage[] messages;

    private int next = -1;

	// Use this for initialization
	void Start () {
	    for (int i = 0; i < messages.Length; i ++) {
            messages[i].nextTime = Time.time + messages[i].secondsBetween;
            if (next == -1 || messages[i].nextTime < messages[next].nextTime) {
                next = i;
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
	    if (Time.time > messages[next].nextTime) {
            Dispatcher.GetInstance ().Dispatch (messages[next].message, 
                                                messages[next].secondsBetween);
            messages[next].nextTime = Time.time + messages[next].secondsBetween;
	        for (int i = 0; i < messages.Length; i ++) {
                if (messages[i].nextTime < messages[next].nextTime) {
                    next = i;
                }
            }
        }
	}
}
