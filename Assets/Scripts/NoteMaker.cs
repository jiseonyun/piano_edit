using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiReader;

public class NoteMaker : MonoBehaviour {

	public TextAsset TestSong;

	public float StartXPos = 5.0f;
	public float XOffset = 2.0f;
	public float LineOffset = 0.25f;

	public Paper paper;
	public MidiSequence sequence;

	private List<Note> NoteList;

	void Start () {

		/*
		if (SongDatabase.Database.CurSequence != null)
			sequence = SongDatabase.Database.CurSequence;*/

		sequence = MidiLoader.Load ("Butterfly", TestSong.bytes);

		if (paper == null)
			paper = GameObject.Find ("Paper").GetComponent<Paper> ();

		paper.InitPaper ();

		NoteList = new List<Note> ();

		MakeNoteList ();
	}

	private void MakeNoteList(){
		List<MidiEvent> tmpList = sequence.GetList () [0].GetEventList ();

		for (int i = 0; i < tmpList.Count; i++) {
			if (!tmpList [i].GetStatus ()) {
				
				for (int j = 0; j < tmpList [i].GetNoteList ().Count; j++) {
					
					GameObject tmpNote = Instantiate(Resources.Load<GameObject>("Prefabs/Note")) as GameObject;				
					tmpNote.GetComponent<Note> ().KeyNum = tmpList [i].GetNoteList () [j];
					tmpNote.GetComponent<Note> ().delta = tmpList [i].GetDelta ();
					NoteList.Add (tmpNote.GetComponent<Note> ());
					paper.AddNoteToPaper (tmpNote.GetComponent<Note> ());
				}

			}
		}

		for (int i = 0; i < NoteList.Count; i++) {
			NoteList [i].SetLine (this, i);
			//NoteList [i].PrintDebug (i);
		}

		paper.SetXScale (NoteList [NoteList.Count - 1].transform.localPosition.x, XOffset);
	}

}
