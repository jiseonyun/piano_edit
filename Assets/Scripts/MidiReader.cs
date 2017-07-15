using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MidiReader
{
	public class MidiEvent
	{
		private int delta;
		private bool status; // NoteOn(0x90): true, NoteOff(0x80): false
		private List<int> noteList;

		public MidiEvent(){
			noteList = new List<int>();
		}

		public void SetEvent(int _delta, bool _status){
			delta = _delta;
			status = _status;
		}

		public int GetDelta(){
			return delta;
		}

		public bool GetStatus(){
			return status;
		}

		public void AddEvent(int keyNote){
			noteList.Add (keyNote);
		}

		public void AddList(List<int> inputList){
			noteList = inputList;
		}

		public List<int>.Enumerator GetEnumerator(){
			return noteList.GetEnumerator ();
		}

		public List<int> GetNoteList(){
			return noteList;
		}

		public int GetListSize(){
			return noteList.Count;
		}
	}

	public class NextEvent
	{
		public int totalDelta;
		public List<int> list;

		public NextEvent(int _totalDelta, List<int> _list){
			totalDelta = _totalDelta;
			list = _list;
		}

		public int GetTotalDelta(){
			return totalDelta;
		}

		public List<int> GetList(){
			return list;
		}
	}

	public class MidiTrack
	{
		private List<MidiEvent> track;

		public MidiTrack(){
			track = new List<MidiEvent> ();
		}

		public void AddToTrack(MidiEvent newEvent){
			track.Add (newEvent);
		}

		public List<MidiEvent>.Enumerator GetEnumerator(){
			return track.GetEnumerator ();
		}

		public List<MidiEvent> GetEventList(){
			return track;
		}
	}

	public class MidiSequence
	{
		private string name;
		private List<MidiTrack> sequence;
		private List<MidiEvent>.Enumerator enumerator;
		private int division;
		private int bpm;
		private float pulsePerSecond;
		private float pulseToNext;
		private float pulseCount;
		private int format;
		private int trackNum;
		private int nominator;
		private int denominator;
		private int playTrack;
		private int currentIndex;
		private bool playing;
		private bool finished;

		public MidiSequence(){
			sequence = new List<MidiTrack> ();
			playing = false;
			currentIndex = 0;
		}

		public void AddTrack(MidiTrack newTrack){
			sequence.Add (newTrack);
		}

		public void SetName(string _name){
			name = _name;
		}

		public void SetHeader(int _format, int _trackNum, int _division, int _bpm){
			format = _format;
			trackNum = _trackNum;
			division = _division;
			bpm = _bpm;
			pulsePerSecond = bpm / 60.0f * division;
		}

		public void SetTempo(int _nominator, int _denominator){
			nominator = _nominator;
			denominator = _denominator;
		}

		public int GetFormat(){return format;}
		public int GetDivision(){return division;}
		public int GetNominator(){return nominator;}
		public int GetDenominator(){return denominator;}
		public int GetBpm(){return bpm;}
		public float GetPPS(){return pulsePerSecond;}

		public List<MidiTrack> GetList(){
			return sequence;
		}

		public int GetTrackNum(){
			return sequence.Count;
		}

		public bool isPlaying(){
			return playing;
		}

		public bool isFinished(){
			return finished;
		}

		public List<MidiTrack>.Enumerator GetEnumerator(){
			return sequence.GetEnumerator ();
		}

		public void Reset(){
			enumerator.Dispose ();
			playing = false;
			currentIndex = 0;
		}

		public MidiEvent Start(int trackIndex, float startTime = 0.0f){
			playTrack = trackIndex;
			enumerator = sequence[trackIndex].GetEnumerator();
			if (enumerator.MoveNext ()) {
				pulseToNext = enumerator.Current.GetDelta ();
				pulseCount = 0.0f;
				playing = true;
				finished = false;
				currentIndex = 0;
				return Advance (startTime);
			} else {
				playing = false;
				return null;
			}
		}

		public MidiEvent Advance(float deltaTime){
			if (!playing) {
				return null;
			}

			pulseCount += pulsePerSecond * deltaTime;

			if (pulseCount < pulseToNext) {
				return null;
			}

			MidiEvent message = new MidiEvent();
			message = null;
			while (pulseCount >= pulseToNext) {
				message = enumerator.Current;
				if (!enumerator.MoveNext ()) {
					playing = false;
					finished = true;
					break;
				}
				currentIndex++;
				pulseCount -= pulseToNext;
				pulseToNext = enumerator.Current.GetDelta ();
			}
			return message;
		}

		public NextEvent GetNextOnEvent(){
			int i = 0;
			int totalDelta = 0;

			while (true) {
				if (sequence [playTrack].GetEventList () [currentIndex + i].GetStatus ())
					break;
				
				totalDelta += sequence [playTrack].GetEventList () [currentIndex + i].GetDelta ();

				/*if (sequence [playTrack].GetEventList () [currentIndex + i].GetStatus ()) {
					break;
				}*/
				if (currentIndex + i + 1 >= sequence [playTrack].GetEventList ().Count)
					break;
				i++;
			}
			NextEvent nextEvent = new NextEvent (totalDelta, sequence [playTrack].GetEventList () [currentIndex + i].GetNoteList ());
			return nextEvent;
		}
	}

	public static class MidiLoader
	{
		public static MidiSequence Load(string name, byte[] data){
			MidiSequence sequence = new MidiSequence ();
			DataStreamReader reader = new DataStreamReader (data);
			sequence.SetName (name);
			int length = data.Length;
			reader.ResetPosition ();

			if (new string (reader.ReadChars (4)) != "MThd") {
				return null;
			}

			reader.Advance (4);
			int format = reader.ReadByteToInt2 ();
			int trackNum = reader.ReadByteToInt2 ();
			int division = reader.ReadByteToInt2 ();
			int bpm = 0, nominator = 0, denominator = 0;

			if (new string(reader.ReadChars (4)) != "MTrk")
				return null;

			reader.Advance (4);
			ReadMetaEvent (data, reader, format, ref bpm, ref nominator, ref denominator, length);
			sequence.SetHeader (format, trackNum, division, bpm);
			sequence.SetTempo (nominator, denominator);

			byte eventCode;
			if (format == 0) {
				while (true) {
					eventCode = (byte)(reader.PeekByte (1) & 0xF0);
					if (eventCode == (byte)0x80 || eventCode == (byte)0x90 || eventCode == (byte)0xC0) {
						break;
					} else {
						reader.Advance (1);
					}
				}
			} else {
				if (new string(reader.ReadChars (4)) != "MTrk")
					return null;
				reader.Advance (4);
			}

			int readCount;
			if (format == 0) {
				readCount = 1;
			} else {
				readCount = trackNum - 1;
			}

			for (int i = 0; i < readCount; i++) {
				sequence.AddTrack (ReadTrack(data, reader, readCount, i, length));
			}

			return sequence;
		}

		static void ReadMetaEvent(byte[] data, DataStreamReader reader, int format, ref int bpm, ref int nom, ref int denom, int length){
			int metaEventCount = 0;

			while (true) {
				if (reader.PeekByte (1) == (byte)0xFF) {
					switch (reader.PeekByte (2)) {
					case (byte)0x51:
						reader.Advance (4);
						bpm = (int)(60000000 / (float)reader.ReadByteToInt3 ());
						metaEventCount++;
						break;
					case (byte)0x58:
						reader.Advance (4);
						nom = reader.ReadByteAdvance ();
						denom = reader.ReadByteAdvance ();
						reader.Advance (2);
						metaEventCount++;
						break;
					case (byte)0x2F:
						reader.Advance (4);
						return;
					default:
						reader.Advance (1);
						break;
					}
				} else {
					reader.Advance (1);
				}

				if (format == 0 && metaEventCount > 1) {
					return;
				} else if (reader.GetPosition () > length) {
					return;
				}
			}
		}

		static MidiTrack ReadTrack(byte[] data, DataStreamReader reader, int readCount, int i, int length){
			MidiTrack newTrack = new MidiTrack ();
			MidiEvent newEvent = null;
			byte eventCode1, eventCode2;
			bool eventFound1 = false, eventFound2 = false;
			int delta, key;

			while (true) {
				eventFound1 = false;
				eventFound2 = false;
				eventCode1 = (byte)(reader.PeekByte (1) & 0xF0);
				eventCode2 = (byte)(reader.PeekByte (2) & 0xF0);

				if (eventCode1 == 0x80 || eventCode1 == 0x90 || eventCode1 == 0xC0) {
					eventFound1 = true;
				} else if (eventCode2 == 0x80 || eventCode2 == 0x90 || eventCode2 == 0xC0) {
					eventFound2 = true;
				} else {
					if (reader.PeekByte (1) == 0xFF && reader.PeekByte (2) == 0x2F) {
						if (newEvent != null)
							newTrack.AddToTrack (newEvent);
						if (i < readCount - 1)
							reader.Advance (12);
						return newTrack;
					} else if (reader.PeekByte (2) == 0xFF && reader.PeekByte (3) == 0x2F) {
						if (newEvent != null)
							newTrack.AddToTrack (newEvent);
						if (i < readCount - 1)
							reader.Advance (13);
						return newTrack;
					}
				}

				if (eventFound1 || eventFound2) {
					if (eventFound2) {
						delta = reader.ReadByteToInt2 ();
						eventCode1 = eventCode2;
					} else {
						delta = (int)reader.ReadByteAdvance ();
					}

					key = (int)reader.PeekByte (1);

					if (newEvent != null) {
						if (eventCode1 == 0x80) {
							
							if (newEvent.GetStatus ()) {
								newTrack.AddToTrack (newEvent);
								newEvent = null;
								newEvent = new MidiEvent ();
								newEvent.SetEvent (delta, false);
								newEvent.AddEvent (key);
							} else {
								if (delta == 0)
									newEvent.AddEvent ((int)reader.PeekByte (1));
								else {
									newTrack.AddToTrack (newEvent);
									newEvent = null;
									newEvent = new MidiEvent ();
									newEvent.SetEvent (delta, false);
									newEvent.AddEvent (key);
								}
							}
							reader.Advance (3);

						} else if (eventCode1 == 0x90) {

							if (newEvent.GetStatus ()) {
								if (delta == 0)
									newEvent.AddEvent ((int)reader.PeekByte (1));
								else {
									newTrack.AddToTrack (newEvent);
									newEvent = null;
									newEvent = new MidiEvent ();
									newEvent.SetEvent (delta, true);
									newEvent.AddEvent (key);
								}
							} else {
								newTrack.AddToTrack (newEvent);
								newEvent = null;
								newEvent = new MidiEvent ();
								newEvent.SetEvent (delta, true);
								newEvent.AddEvent (key);
							}
							reader.Advance (3);

						} else {
							reader.Advance (2);
						}
					} else {
						if (eventCode1 == 0x90) {
							newEvent = new MidiEvent ();
							newEvent.SetEvent (delta, true);
							newEvent.AddEvent (key);
							reader.Advance (3);
						} else if (eventCode1 == 0x80) {
							newEvent = new MidiEvent ();
							newEvent.SetEvent (delta, false);
							newEvent.AddEvent (key);
							reader.Advance (3);
						} else {
							reader.Advance (2);
						}
					}
				}
				//
				if (reader.GetPosition () > length) {
					return null;
				}
			}
		}

		public static byte[] WriteToBytes(MidiSequence sequence){
			List<byte> listToWrite = new List<byte> ();

			byte[] MThd = new byte[] {
				0x4D, 0x54, 0x68, 0x64, //MThd
				0x00, 0x00, 0x00, 0x06, //Length
				0x00, 0x01, //Format
				0x00, 0x03, //TrackNum
				0x00, 0x10  //Division
			};

			byte[] MTrk = new byte[] {
				0x4D, 0x54, 0x72, 0x6B
			};

			byte[] TimeSig = new byte[] {
				0x00, 0xFF, 0x58, 0x04, 0x02, 0x02, 0x24, 0x08
			};

			byte[] SetTempo = new byte[] {
				0x00, 0xFF, 0x51, 0x03, 0x09, 0x89, 0x68
			};

			byte[] MetaFooter = new byte[] {
				0x83, 0x00, 0xFF, 0x2F, 0x00
			};

			byte[] NormalFooter = new byte[] {
				0x00, 0xFF, 0x2F, 0x00
			};

			MThd [9] = (byte)sequence.GetFormat ();
			MThd [11] = (byte) (sequence.GetTrackNum () + 1);
			AddArrayToList (listToWrite, MThd);

			int firstEventLength = 0;
			for (int i = 0; i < sequence.GetList () [0].GetEventList ().Count; i++) {
				firstEventLength += sequence.GetList () [0].GetEventList () [i].GetListSize() * 4;
			}
			firstEventLength += 3;
			firstEventLength += NormalFooter.Length;

			if (sequence.GetFormat () == 0) {
				AddArrayToList (listToWrite, MTrk);
				int SingleTrackLength = firstEventLength + TimeSig.Length + SetTempo.Length;

				int high = SingleTrackLength / 256;
				int low = SingleTrackLength - (high * 256);

				listToWrite.Add ((byte) 0);
				listToWrite.Add ((byte) 0);
				listToWrite.Add ((byte) high);
				listToWrite.Add ((byte) low);

				TimeSig [4] = (byte) sequence.GetNominator ();
				TimeSig [5] = (byte)sequence.GetDenominator ();
				AddArrayToList (listToWrite, TimeSig);

				int bpm = 60000000 / sequence.GetBpm ();

				int bpmHigh = bpm / 65536;
				int bpmMiddle = (625000 - (bpmHigh * 65536)) / 256;
				int bpmLow = (625000 - (bpmHigh * 65536)) - (bpmMiddle * 256);

				SetTempo [4] = (byte)bpmHigh;
				SetTempo [5] = (byte)bpmMiddle;
				SetTempo [6] = (byte)bpmLow;
				AddArrayToList (listToWrite, SetTempo);
				AddArrayToList (listToWrite, NormalFooter);

				//TODO: End here, make listToWrite to byte[], then return it

			} else {
				AddArrayToList (listToWrite, MTrk);
				int MetaLength = TimeSig.Length + SetTempo.Length + MetaFooter.Length;

				int high = MetaLength / 256;
				int low = MetaLength - (high * 256);

				listToWrite.Add ((byte) 0);
				listToWrite.Add ((byte) 0);
				listToWrite.Add ((byte) high);
				listToWrite.Add ((byte) low);

				TimeSig [4] = (byte) sequence.GetNominator ();
				TimeSig [5] = (byte)sequence.GetDenominator ();
				AddArrayToList (listToWrite, TimeSig);

				int bpm = 60000000 / sequence.GetBpm ();

				int bpmHigh = bpm / 65536;
				int bpmMiddle = (bpm - (bpmHigh * 65536)) / 256;
				int bpmLow = (bpm - (bpmHigh * 65536)) - (bpmMiddle * 256);

				SetTempo [4] = (byte)bpmHigh;
				SetTempo [5] = (byte)bpmMiddle;
				SetTempo [6] = (byte)bpmLow;
				AddArrayToList (listToWrite, SetTempo);
				AddArrayToList (listToWrite, MetaFooter);

				AddArrayToList (listToWrite, MTrk);

				int firstHigh = firstEventLength / 256;
				int firstLow = firstEventLength - (firstHigh * 256);

				listToWrite.Add ((byte) 0);
				listToWrite.Add ((byte) 0);
				listToWrite.Add ((byte) firstHigh);
				listToWrite.Add ((byte) firstLow);

				WriteEventsToList (listToWrite, sequence.GetList () [0]);

				for (int i = 1; i < sequence.GetList ().Count; i++) {
					AddArrayToList (listToWrite, MTrk);
					int tmpTrackLength = 0;
					for (int j = 0; j < sequence.GetList () [i].GetEventList ().Count; j++) {
						tmpTrackLength += sequence.GetList () [i].GetEventList () [j].GetListSize() * 4;
					}
					tmpTrackLength += 3;
					tmpTrackLength += NormalFooter.Length;

					int tmpTrackHigh = tmpTrackLength / 256;
					int tmpTrackLow = tmpTrackLength - (tmpTrackHigh * 256);

					listToWrite.Add ((byte) 0);
					listToWrite.Add ((byte) 0);
					listToWrite.Add ((byte) tmpTrackHigh);
					listToWrite.Add ((byte) tmpTrackLow);


					WriteEventsToList (listToWrite, sequence.GetList () [i]);
				}
			}

			byte[] result = new byte[listToWrite.Count];
			for (int i = 0; i < listToWrite.Count; i++) {
				result [i] = listToWrite [i];
			}
			return result;
		}

		static void AddArrayToList(List<byte> list, byte[] input){
			for (int i = 0; i < input.Length; i++) {
				list.Add (input [i]);
			}
		}

		static void WriteEventsToList(List<byte> list, MidiTrack track){
			byte[] NoteOn = new byte[] {
				0x00, 0x90, 0x00, 0x7F
			};
			byte[] NoteOff = new byte[] {
				0x00, 0x80, 0x00, 0x00
			};
			byte[] ProgChange = new byte[] {
				0x00, 0xC0, 0x00
			};
			byte[] NormalFooter = new byte[] {
				0x00, 0xFF, 0x2F, 0x00
			};

			//Receive Program No. and apply it to array.
			AddArrayToList (list, ProgChange);

			for (int i = 0; i < track.GetEventList ().Count; i++) {
				int j = 0;
				MidiEvent tmp = track.GetEventList () [i];

				if (tmp.GetStatus ()) {
					//NoteOn
					do{
						if(j == 0)
							NoteOn[0] = (byte)tmp.GetDelta();
						else
							NoteOn[0] = (byte)0;
						NoteOn [2] = (byte)tmp.GetNoteList () [j];
						AddArrayToList (list, NoteOn);
						j++;
					}while(j < tmp.GetListSize());
						
				} else {
					//NoteOff
					do{
						if(j == 0)
							NoteOff[0] = (byte)tmp.GetDelta();
						else
							NoteOff[0] = (byte)0;
						NoteOff [2] = (byte)tmp.GetNoteList () [j];
						AddArrayToList (list, NoteOff);
						j++;
					}while(j < tmp.GetListSize());

				}
			}
			AddArrayToList (list, NormalFooter);
		}
	}

	class DataStreamReader
	{
		private byte[] data;
		private int position;

		public DataStreamReader(byte[] input){
			data = input;
			position = 0;
		}

		public int GetPosition(){
			return position;
		}

		public void ResetPosition(){
			position = 0;
		}

		public byte PeekByte(int offset){
			return data [position + offset];
		}

		public byte ReadByteAdvance(){
			return data [position++];
		}

		public void Advance(int length){
			position += length;
		}

		public char[] ReadChars(int length){
			char[] tmp = new char[length];
			for (int i = 0; i < length; i++) {
				tmp [i] = (char)data [position + i];
			}
			Advance (length);
			return tmp;
		}

		public int ReadByteToInt4(){
			int b1 = ReadByteAdvance ();
			int b2 = ReadByteAdvance ();
			int b3 = ReadByteAdvance ();
			int b4 = ReadByteAdvance ();
			return b4 + (b3 << 8) + (b2 << 16) + (b1 << 24);
		}

		public int ReadByteToInt3(){
			int b1 = ReadByteAdvance ();
			int b2 = ReadByteAdvance ();
			int b3 = ReadByteAdvance ();
			return b3 + (b2 << 8) + (b1 << 16);
		}

		public int ReadByteToInt2(){
			int b1 = ReadByteAdvance ();
			int b2 = ReadByteAdvance ();
			return b2 + (b1 << 8);
		}
	}
}