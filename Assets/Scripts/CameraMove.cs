using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour {
	
	public float DragSpeed = 0.025f;

	public float MinXPos;
	public float MaxXPos;

	private Vector3 lastPosition;

	void Start () {
		
	}

	void Update () {
		if (Input.GetMouseButtonDown (0)) {
			lastPosition = Input.mousePosition;
			return;
		}

		if (!Input.GetMouseButton (0))
			return;

		Vector3 delta = Input.mousePosition - lastPosition;
		if (transform.position.x - delta.x * DragSpeed > MinXPos && transform.position.x - delta.x * DragSpeed < MaxXPos) {
			transform.Translate (-delta.x * DragSpeed, 0f, 0f);
			lastPosition = Input.mousePosition;
		}
	}

	public void SetMinMax(float min, float max){
		MinXPos = min;
		MaxXPos = max;
	}
}