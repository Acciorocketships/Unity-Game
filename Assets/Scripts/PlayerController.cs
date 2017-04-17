using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public GameObject player; // unused
	public GameObject cam;
	public GameObject ground;
	private Rigidbody playerbody;

	public float movespeed;
	public float jumpheight;
	public float jumpfloatforce; // For jetpack, edit this value

	private Vector3 forward;
	private Vector3 right;
	private bool touching;
	private bool swinging;

	void Start () {
		playerbody = GetComponent<Rigidbody>();
		Cursor.visible = false;
		swinging = false;
	}

	void FixedUpdate () {
		// Inputs
		forward = new Vector3 (transform.position.x - cam.transform.position.x, 0f, transform.position.z - cam.transform.position.z);
		forward = forward.normalized;
		right = Vector3.Cross (forward, Vector3.up);
		float moveHorizontal = -1 * Input.GetAxis ("Horizontal");
		float moveVertical = Input.GetAxis ("Vertical");
		float jump = Input.GetAxis ("Jump");
		float fire = Input.GetAxis ("Fire1");

		// Value Calculation
		Vector3 movement = new Vector3 (forward.x * moveVertical + right.x * moveHorizontal, 0f, forward.z * moveVertical + right.z * moveHorizontal);
		RaycastHit hit;
		touching = Physics.Raycast(transform.position,Vector3.down, out hit, 0.501f);

		// Movement

		playerbody.AddForce (movement * movespeed - new Vector3 (playerbody.velocity.x, 0f, playerbody.velocity.z), ForceMode.VelocityChange);
		if (swinging) {
			if (fire == 1) {
				// Delete Rope
				swinging = false;
			}
			transform.rotation = Quaternion.identity;
			playerbody.AddForce (movement * movespeed * 50, ForceMode.Acceleration);
		}
		else {
			if (fire == 1) {
				Physics.Raycast (cam.transform.position, (cam.transform.rotation * Vector3.forward), out hit, 20);
				float distance = Vector3.Distance (player.transform.position, hit.point);
				// Fire rope
				swinging = true;
			}
		}
		if (jump != 0) {
			playerbody.AddForce (Vector3.up * jumpfloatforce);
			if (touching && playerbody.velocity.y >= -0.5) {
				playerbody.AddForce (Vector3.up * jumpheight, ForceMode.VelocityChange);
			}
		}	
	}

}


// Get public class variable:
// objectinstance.GetComponent<scriptname>().variablename

// Get child gameobject:
// GameObject child = parent.transform.Find("childname").gameObject;