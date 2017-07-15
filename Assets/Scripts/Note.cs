using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour {

	public float xPos;
	public float yPos; // 0.0f for middle B(47)

	public int KeyNum;
	public int delta;

	private int Octave;
	private int noteName;
	private bool spriteFlip;

	public void SetLine(NoteMaker maker, int xNum){
		xPos = maker.StartXPos + xNum * maker.XOffset;

		Octave = KeyNum / 12;
		noteName = KeyNum % 12;

		int tmpLine = SelectLine (noteName);
		float tmpYPos = tmpLine * maker.LineOffset;

		if (Octave == 5) {
			tmpYPos = tmpYPos - 1.5f;
		} else if (Octave == 6) {
			tmpYPos = tmpYPos + 0.25f;
		} else {
			
		}

		yPos = tmpYPos;

		if (yPos >= 0.0f)
			spriteFlip = true;
		else
			spriteFlip = false;

		if (spriteFlip) {
			gameObject.GetComponent<SpriteRenderer> ().flipX = true;
			gameObject.GetComponent<SpriteRenderer> ().flipY = true;
		}

		transform.localPosition = new Vector3 (xPos, yPos, 0.0f);
		transform.localScale = new Vector3 (1.0f, 1.0f, 1.0f);
	}

	private int SelectLine(int _noteName){
		switch (_noteName) {
		case 0: // C
		case 1: // Cs
			return 0;
			break;
		case 2: // D
		case 3: // Ds
			return 1;
			break;
		case 4: // E
			return 2;
			break;
		case 5: // F
		case 6: // Fs
			return 3;
			break;
		case 7: // G
		case 8: // Gs
			return 4;
			break;
		case 9: // A
		case 10: // As
			return 5;
			break;
		case 11: // B
			return 6;
			break;
		default:
			return 7;
			break;
		}
	}

	public void PrintDebug(int idx){
		print (idx + "_KeyNum" + KeyNum + "/ Octave: " + Octave + "/ NoteName: " + noteName + "/ yPos: " + yPos);
	}
}
