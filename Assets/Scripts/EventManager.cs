using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class EventManager : Manager<EventManager> {

    public event Action OnUpdate = delegate { };
    public event Action OnFixedUpdate = delegate { };
    public event Action OnLateUpdate = delegate { };

    List<Action> delayedActions = new List<Action>(100);
    List<float> delayedActionTimers = new List<float>(100);

    List<Action> delayedActionsByFrame = new List<Action>(100);
    List<int> delayedActionFrames = new List<int>(100);

    void Start() {
        SceneManager.sceneUnloaded += scene => {
            delayedActions.Clear();
            delayedActionTimers.Clear();
            delayedActionsByFrame.Clear();
            delayedActionFrames.Clear();
        };
    }

    public void Delay(Action action, float seconds) {
        if (seconds <= 0) {
            action();
        } else if (!float.IsPositiveInfinity(seconds)) {
            delayedActions.Add(action);
            delayedActionTimers.Add(seconds);
        }
    }

    public void DelayByFrames(Action action, int frames) {
        if (frames <= 0) {
            action();
        } else {
            delayedActionsByFrame.Add(action);
            delayedActionFrames.Add(frames);
        }
    }

    public void ClearAction(Action action) {
        for (int i = 0; i < delayedActions.Count; i++) {
            if (delayedActions[i] == action) {
                delayedActions.RemoveAt(i);
                delayedActionTimers.RemoveAt(i);
                i--;
            }
        }
        for (int i = 0; i < delayedActionsByFrame.Count; i++) {
            if (delayedActionsByFrame[i] == action) {
                delayedActionsByFrame.RemoveAt(i);
                delayedActionFrames.RemoveAt(i);
                i--;
            }
        }
    }

    void Update() {
        // fire delayed actions if they ran out in the previous lateupdate
        for (int i = 0; i < delayedActions.Count; i++) {
            if (delayedActionTimers[i] <= 0) {
                var action = delayedActions[i];
                delayedActions.RemoveAt(i);
                delayedActionTimers.RemoveAt(i);
                try {
                    action();
                } catch (Exception e) {
                    Debug.LogError(e.Message);
                }
                i--;
            }
        }
        for (int i = 0; i < delayedActionsByFrame.Count; i++) {
            if (delayedActionFrames[i] <= 0) {
                var action = delayedActionsByFrame[i];
                delayedActionsByFrame.RemoveAt(i);
                delayedActionFrames.RemoveAt(i);
                try {
                    action();
                } catch (Exception e) {
                    Debug.LogError(e.Message);
                }
                i--;
            }
        }
        // call update handlers
        OnUpdate();
    }

    void FixedUpdate() {
        // call fixed update handlers
        OnFixedUpdate();
    }

    void LateUpdate() {
        // subtract time after all updates to ensure no non-zero delays are fired on the same frame they are registered
        for (int i = 0; i < delayedActions.Count; i++) {
            delayedActionTimers[i] -= Time.deltaTime;
        }
        for (int i = 0; i < delayedActionsByFrame.Count; i++) {
            delayedActionFrames[i]--;
        }
        // call late update handlers
        OnLateUpdate();
    }
}
