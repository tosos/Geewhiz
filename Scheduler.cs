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

	void Awake () {
	    for (int i = 0; i < messages.Length; i ++) {
            messages[i].nextTime = Time.time + messages[i].secondsBetween;
            if (next == -1 || messages[i].nextTime < messages[next].nextTime) {
                next = i;
            }
        }
        if (next >= 0) {
            StartCoroutine (Schedule());
        }
	}
    IEnumerator Schedule () {
        while (true) {
            yield return new WaitForSeconds(messages[next].nextTime - Time.time);
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
