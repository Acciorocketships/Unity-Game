using UnityEngine;
using System.Collections;

public class Rope : MonoBehaviour {

	public bool parent;
	public GameObject connectTo;

	void Start () {
		Init ();
	}

	public void Init() {
		gameObject.AddComponent<CharacterJoint>();
		GetComponent<CharacterJoint>().connectedBody = transform.parent.GetComponent<Rigidbody>();
		transform.SetParent(GameObject.Find("Rope").transform);
		if (connectTo) {
			Debug.Log (connectTo);
			connectTo.AddComponent<CharacterJoint>();
			connectTo.GetComponent<CharacterJoint> ().connectedBody = GetComponent<Rigidbody> ();
		}
		Physics.IgnoreCollision (GetComponent<Collider> (), GameObject.Find ("Player").GetComponent<Collider> ());
	}

	void FixedUpdate(){

	}
}
