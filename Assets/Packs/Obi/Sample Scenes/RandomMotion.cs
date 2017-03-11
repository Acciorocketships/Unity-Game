using UnityEngine;
using System.Collections;

public class RandomMotion : MonoBehaviour
{

	float angleX, angleY, angleZ;
	float incrementX,incrementY,incrementZ;
	Vector3 center;

	// Use this for initialization
	void Start ()
	{
		incrementX = 25 + Random.value * 75;
		incrementY = 25 + Random.value * 75;
		incrementZ = 25 + Random.value * 75;
		center = transform.position;
	}
	
	// Update is called once per frame
	void Update ()
	{
		angleX += Time.deltaTime * incrementX;
		angleY += Time.deltaTime * incrementY;
		angleZ += Time.deltaTime * incrementZ;
		Vector3 position = transform.position;
		position.x = Mathf.Cos(Mathf.Deg2Rad*angleX);
		position.y = Mathf.Sin(Mathf.Deg2Rad*angleY);
		position.z = Mathf.Cos(Mathf.Deg2Rad*angleZ);
		transform.position = center+position;
	}
}

