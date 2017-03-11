using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

	public GameObject Player;
	public  float heightoffset;
	public float distoffset;
	public float heightmultiplier;
	public float mousesensitivity;
	public float ymousesensitivity;
	private float pan = 0;
	private float tilt = 0;

	void Start () {
		Cursor.visible = false;
	}

	void Update() {
		pan += -1 * Input.GetAxis ("Mouse X") * mousesensitivity * Time.deltaTime;
		tilt += -1 * Input.GetAxis ("Mouse Y") * ymousesensitivity * Time.deltaTime;
		if (tilt > 90)
			tilt = 90;
		else if (tilt < -90)
			tilt = -90;
	}

	void LateUpdate () {
		Vector3 xzdirection = new Vector3 (-1 * Mathf.Cos (Mathf.PI / 180 * pan), 0f, -1 * Mathf.Sin (Mathf.PI / 180 * pan));
		Vector3 ydirection = new Vector3 (0f, Mathf.Sin (Mathf.PI / 180 * tilt), 0f);
		transform.position = Player.transform.position + distoffset * xzdirection + heightmultiplier * ydirection;
		transform.LookAt(transform.position + -1 * xzdirection.normalized + -1 * heightmultiplier * ydirection + new Vector3(0f,heightoffset,0f));
	}
}
