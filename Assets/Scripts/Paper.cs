using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paper : MonoBehaviour {

	public GameObject Lines;
	public GameObject NotePanel;
	public GameObject StartVerticalLine;
	public GameObject EndVerticalLine;

	public float PaperWidth = 6.0f;

	void Start () {
		
	}

	public void InitPaper(){
		if (Lines == null)
			Lines = transform.Find ("Lines").gameObject;

		if (NotePanel == null)
			NotePanel = transform.Find ("NotePanel").gameObject;

		if (StartVerticalLine == null)
			StartVerticalLine = transform.Find ("VerticalLine_S").gameObject;
		
		if (EndVerticalLine == null)
			EndVerticalLine = transform.Find ("VerticalLine_E").gameObject;

		Vector3 v = transform.position;
		v.y = 0.0f;
		transform.position = v;
	}

	public void AddNoteToPaper(Note input){
		input.transform.SetParent (NotePanel.transform);
	}

	public void SetXScale(float lastXPos, float xOffset){
		float scaleFactor = (lastXPos + xOffset) / PaperWidth;

		Lines.transform.localScale = new Vector3 (scaleFactor, 1.0f, 1.0f);

		Vector3 v = EndVerticalLine.transform.localPosition;
		v.x = lastXPos + xOffset;
		EndVerticalLine.transform.localPosition = v;

		GameObject.FindGameObjectWithTag ("MainCamera").GetComponent<CameraMove> ().SetMinMax (StartVerticalLine.transform.position.x, EndVerticalLine.transform.position.x);
	}
}
