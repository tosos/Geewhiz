using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Replay : MonoBehaviour {

    public int sendrate = 15;
    private List<ReplayView> views;

    static private Replay instance = null;
    static public Replay GetInstance () {
        if (instance == null) {
            instance = (Replay) FindObjectOfType (typeof(Replay));
        }
        return instance;
    }

    void Awake () {
        views = new List<ReplayView> ();
    }

	// Use this for initialization
	void Start () {
        float time = 1.0f / (float)sendrate;
        Scheduler.GetInstance ().AddSchedule (time, "HandleReplay", true);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void AddReplayView (ReplayView view) {
        views.Add (view);
    }

    public void RemoveReplayView (ReplayView view) {
        views.Remove (view);
    }
}
