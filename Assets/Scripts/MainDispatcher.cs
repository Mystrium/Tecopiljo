using System.Collections.Generic;
using UnityEngine;
using System;

public class MainDispatcher : MonoBehaviour {
    static readonly Queue<Action> _actions = new Queue<Action>();
    static MainDispatcher _instance;

    void Awake() { _instance = this; DontDestroyOnLoad(gameObject); }

    void Update() {
        lock (_actions) {
            while (_actions.Count > 0) _actions.Dequeue().Invoke();
        }
    }

    public static void Enqueue(Action a) {
        lock (_actions) _actions.Enqueue(a);
    }
}