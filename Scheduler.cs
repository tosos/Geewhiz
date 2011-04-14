using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class TimerMessage {
    public string message;
    public float secondsBetween;
    [HideInInspector]
    public float nextTime;
    [HideInInspector]
    public GameObject gameObject;
};

public class Scheduler : MonoBehaviour {

    public TimerMessage[] messages;
    private List<TimerMessage> priorityQueue;

    static private Scheduler instance = null;
    static public Scheduler GetInstance () {
        if (instance == null) {
            instance = (Scheduler) FindObjectOfType (typeof(Scheduler));
        }
        return instance;
    }
	void Awake () {
        if (instance != null) {
            Destroy (instance.gameObject);
        }
        instance = null;

        priorityQueue = new List<TimerMessage> ();
        // blank initial because 0 index shouldn't be used.
        priorityQueue.Add (new TimerMessage ()); 

	    for (int i = 0; i < messages.Length; i ++) {
            messages[i].gameObject = null;
            messages[i].nextTime = Time.time + messages[i].secondsBetween;
            
            priorityQueue.Add (messages[i]);
            BubbleUp (priorityQueue.Count - 1);
        }

        if (priorityQueue.Count > 1) {
            // StartCoroutine (Schedule());
        } else {
            enabled = false;
        }
	}

    public void AddSchedule (float time, string message, bool repeat, GameObject obj = null) {
        TimerMessage msg = new TimerMessage ();
        msg.message = message;
        msg.nextTime = Time.time + time;
        msg.secondsBetween = (repeat ? time : 0.0f);
        msg.gameObject = obj;
        priorityQueue.Add (msg);
        BubbleUp (priorityQueue.Count - 1);
        enabled = true;
    }

    public void CancelSchedule (string message, GameObject obj = null) {
        bool continueLoop = true;
        while (continueLoop) {
            continueLoop = false;
            for (int i = 1; i < priorityQueue.Count; i ++) {
                if (priorityQueue[i].message == message && 
                        (obj == null || priorityQueue[i].gameObject == obj)) {

                    int last = priorityQueue.Count - 1;
                    priorityQueue[i] = priorityQueue[last];
                    priorityQueue.RemoveAt (last);
                    BubbleDown (i);

                    continueLoop = true;
                    break;
                }
            }
        }
        if (priorityQueue.Count <= 1) {
            enabled = false;
        }
    }

    void Update () {
        if (Time.time > priorityQueue[1].nextTime) {
            // store these off so we can move the top level or remove it
            GameObject obj = priorityQueue[1].gameObject;
            string message = priorityQueue[1].message;
            float secondsBetween = priorityQueue[1].secondsBetween; 

            // move it before we call the message in case the message modifies the queue
            if (priorityQueue[1].secondsBetween > 0) {
                priorityQueue[1].nextTime = Time.time + priorityQueue[1].secondsBetween;
                BubbleDown (1);
            } else {
                int last = priorityQueue.Count - 1;
                priorityQueue[1] = priorityQueue[last];
                priorityQueue.RemoveAt (last);
                BubbleDown (1);
            }

            if (priorityQueue.Count <= 1) {
                enabled = false;
            }

            if (obj) {
                obj.SendMessage (message, secondsBetween);
            } else {
                Dispatcher.GetInstance ().Dispatch (message, secondsBetween);
            }

        }
    }

/* TODO need to be able to interrupt the WaitForSeconds when Add or Cancel is called.
    IEnumerator Schedule () {
        while (true) {
            yield return new WaitForSeconds(priorityQueue[1].nextTime - Time.time);
            if (priorityQueue[1].gameObject) {
                priorityQueue[1].gameObject.SendMessage (priorityQueue[1].message, 
                                                         priorityQueue[1].secondsBetween);
            } else {
                Dispatcher.GetInstance ().Dispatch (priorityQueue[1].message, 
                                                    priorityQueue[1].secondsBetween);
            }
            if (priorityQueue[1].secondsBetween > 0) {
                priorityQueue[1].nextTime = Time.time + priorityQueue[1].secondsBetween;
                BubbleDown (1);
            } else {
                int last = priorityQueue.Count - 1;
                priorityQueue[1] = priorityQueue[last];
                priorityQueue.RemoveAt (last);
                BubbleDown (1);
            }
        }
    }
*/

    void BubbleUp (int ind) {
        while (ind > 1) {
            int parent = ind / 2;
            if (priorityQueue[ind].nextTime < priorityQueue[parent].nextTime) {
                TimerMessage tmp = priorityQueue[parent];
                priorityQueue[parent] = priorityQueue[ind];
                priorityQueue[ind] = tmp;
                ind = parent;
            } else {
                break;
            }
        }
    }

    void BubbleDown (int ind) {
        while (ind < priorityQueue.Count) {
            int left = ind * 2;
            int right = ind * 2 + 1;

            if (left >= priorityQueue.Count) {
                break;
            }

            int minInd;
            if (right >= priorityQueue.Count || 
                priorityQueue[left].nextTime < priorityQueue[right].nextTime) 
            {
                minInd = left;
            }  else {
                minInd = right;
            }

            if (priorityQueue[minInd].nextTime < priorityQueue[ind].nextTime) {
                TimerMessage tmp = priorityQueue[minInd];
                priorityQueue[minInd] = priorityQueue[ind];
                priorityQueue[ind] = tmp;
                ind = minInd;
            } else {
                break;
            }
        }
        
    }
}
