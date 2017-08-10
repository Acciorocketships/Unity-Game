using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RopeController : MonoBehaviour {

	private LinkedList<GameObject> children;
	public float length;
	private bool needsInit;
	public GameObject start;
	public GameObject end;



	void Start() {
		needsInit = true;
		// Must be initialize after its children because the hierarchy changes
		ConnectEnds();
	}



	void Init () {
		
		children = new LinkedList<GameObject>();
		for(int i = 0; i < gameObject.transform.childCount; i++){
			GameObject link = gameObject.transform.GetChild (i).gameObject;
			if (link.GetComponent<CharacterJoint> ()) {
				link.GetComponent<CharacterJoint> ().autoConfigureConnectedAnchor = false;
			}
			children.AddLast (link);
		}

		ConnectEnds ();

		needsInit = false;
	}



	void FixedUpdate () {

		if (needsInit) {
			Init ();
		}

		Debug.Log (children);
		foreach (GameObject link in children) {
			link.transform.localScale = link.transform.localScale.Change (y: length);
			link.GetComponent<CharacterJoint> ().connectedAnchor = link.GetComponent<CharacterJoint> ().connectedAnchor.Change (y: -length / 2f * 0.95f);
		}

		if (length >= 1.1) { AddLinks (); }
		if (length <= 0.9) { RemoveLinks (); }
	}



	void AddLinks() {
		
		int numtoadd = (int) Mathf.Round( children.Count * (length - 1) );
		for (int i = 0; i < numtoadd; i++) {
			GameObject link = (GameObject) Instantiate (children.Last.Value, children.Last.Value.transform);
			children.AddLast (link);
		}

		ConnectEnds ();
	}


	void RemoveLinks() {
		
		int numtoremove = (int) Mathf.Round( children.Count * (1 - length) );
		for (int i = 0; i < numtoremove; i++) {
			children.RemoveLast ();
		}

		ConnectEnds ();
	}


	void ConnectEnds() {
		
		if (start != null) {
			start.AddComponent<CharacterJoint> ();
			start.GetComponent<CharacterJoint> ().connectedBody = GetComponent<Rigidbody> ();
		}

		if (end != null) {
			children.Last.Value.GetComponent<Rope> ().connectTo = end;
			children.Last.Value.GetComponent<Rope> ().Init();
		}
	}
}