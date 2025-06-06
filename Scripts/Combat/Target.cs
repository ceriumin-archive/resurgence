﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour {
    [SerializeField]
    new string name;

    public string Name {
        get {
            return name;
        }
    }

    public Vector3 Position {
        get {
            return rigidbody.position;
        }
    }

    public Vector3 Velocity {
        get {
            return rigidbody.velocity;
        }
    }

    public Plane Plane { get; private set; }
    new Rigidbody rigidbody;

    List<Missile> incomingMissiles;
    const float sortInterval = 0.5f;
    float sortTimer;

    void Awake() {

        rigidbody = GetComponent<Rigidbody>();
        Plane = GetComponent<Plane>();
        incomingMissiles = new List<Missile>();
    }

    void FixedUpdate() {
        sortTimer = Mathf.Max(0, sortTimer - Time.fixedDeltaTime);

        if (sortTimer == 0) {
            SortIncomingMissiles();
            sortTimer = sortInterval;
        }
    }

    void SortIncomingMissiles() {
        var position = Position;

        //sorts all the missiles by distance to the target
        if (incomingMissiles.Count > 0) {
            incomingMissiles.Sort((Missile a, Missile b) => {
                if(a == null || b == null)
                    return 0;
                var distA = Vector3.Distance(a.Rigidbody.position, position);
                var distB = Vector3.Distance(b.Rigidbody.position, position);
                return distA.CompareTo(distB);
            });
        }
    }

    public Missile GetIncomingMissile() {
        if (incomingMissiles.Count > 0) {
            return incomingMissiles[0];
        }

        return null;
    }

    public void NotifyMissileLaunched(Missile missile, bool value) {
        //notifies aircraft that a missile has been launched at it which allows it to sort the missiles by distance
        //then in the future there cna be functionality for aircraft to dodge missiles, or for example to notify the player for alerts/warnings
        if (value) {
            incomingMissiles.Add(missile);
            SortIncomingMissiles();
        } else {
            incomingMissiles.Remove(missile);
        }
    }
}
