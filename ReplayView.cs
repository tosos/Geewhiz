using UnityEngine;
using System.Collections;

public class ReplayView : MonoBehaviour {

    [SerializeField]
    private int viewID = 0;
    private static int sharedViewID = 0;

    void Reset () {
        if (viewID == 0) {
            sharedViewID ++;
            viewID = sharedViewID;
        }
    }

	void Start () {
	    Replay.GetInstance ().AddReplayView (this);
        Dispatcher.GetInstance ().Register ("HandleReplay", gameObject);
	}

    void OnDestroy () {
        Dispatcher d = Dispatcher.GetInstance ();
        if (d) {
            d.Unregister ("HandleReplay", gameObject);
        }
	    Replay.GetInstance ().RemoveReplayView (this);
    }
	
    void HandleReplay () {
    }
}
