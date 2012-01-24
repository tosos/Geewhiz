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
    private PriorityQueue<TimerMessage> priorityQueue;

    private static int Comparison (TimerMessage a, TimerMessage b) {
        if (a.nextTime < b.nextTime) {
            return -1;
        } else if (a.nextTime > b.nextTime) {
            return 1;
        } else {
            return 0;
        }
    }

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

        priorityQueue = new PriorityQueue<TimerMessage> ();
        priorityQueue.comparator = Comparison;

	    for (int i = 0; i < messages.Length; i ++) {
            messages[i].gameObject = null;
            messages[i].nextTime = Time.time + messages[i].secondsBetween;
            
            priorityQueue.Add (messages[i]);
        }

        if (!priorityQueue.Empty ()) { 
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
        enabled = true;
    }

    public void UpdateSchedule (float time, string message, GameObject obj = null) {
        TimerMessage msg = 
            priorityQueue.Find ((a) => a.message == message && a.gameObject == obj);
        if (msg != null && msg.nextTime > 0) {
            msg.nextTime = Time.time + time;
            priorityQueue.Update (msg);
        } else {
            AddSchedule (time, message, false, obj);
        }
    }

    public void CancelSchedule (string message, GameObject obj = null) {
        bool continueLoop = true;
        while (continueLoop) {
            continueLoop = 
                priorityQueue.Remove ((a) => a.message == message && (obj == null || a.gameObject == obj));
        }
        if (priorityQueue.Empty ()) {
            enabled = false;
        }
    }

    void Update () {
        if (Time.time > priorityQueue.Top.nextTime) {
            // store these off so we can move the top level or remove it
            GameObject obj = priorityQueue.Top.gameObject;
            string message = priorityQueue.Top.message;
            float secondsBetween = priorityQueue.Top.secondsBetween; 

            // move it before we call the message in case the message modifies the queue
            if (priorityQueue.Top.secondsBetween > 0) {
                priorityQueue.Top.nextTime = Time.time + priorityQueue.Top.secondsBetween;
                priorityQueue.Update (priorityQueue.Top);
            } else {
                priorityQueue.Remove (priorityQueue.Top);
            }

            if (priorityQueue.Empty ()) {
                enabled = false;
            }

            if (obj) {
                obj.SendMessage (message, secondsBetween);
            } else {
                Dispatcher.GetInstance ().Dispatch (message, secondsBetween);
            }

        }
    }
}
