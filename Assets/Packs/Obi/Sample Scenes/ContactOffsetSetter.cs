using UnityEngine;
using System.Collections;

public class ContactOffsetSetter : MonoBehaviour {

	Collider col;
	public float contactOffset = 0.01f;

	// Use this for initialization
	void Start () {
		col = GetComponent<Collider>();
		if (col != null)
			col.contactOffset = contactOffset;
	}

}
