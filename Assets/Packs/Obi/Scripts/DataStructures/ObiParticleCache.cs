using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obi
{
	/**
	 * An ObiParticleCache can store the result of the simulation performed by an ObiSolver, for later playback.
	 */
	public class ObiParticleCache : ScriptableObject
	{

		public class UncompressedFrame{
			public List<int> indices = new List<int>();
			public List<Vector3> positions = new List<Vector3>();
		}

		/**
		 * Particle cache frames use a cell-based compression scheme, in which
		 * particles are inserted in "cells"(buckets) based on their 3d coordinates. Then they store
		 * a 24-bit offset from their cell position. This meets our 3 requirements for particle caches:
		 * - On the fly compression.
		 * - On the fly decompression.
		 * - Fast frame interpolation. 
		 */
		[Serializable]
		public class Frame{

			public float time;

			public List<Vector3> positions;			/**< List of cell offsets for each particle. Assumed to be ordered in increasing index order.*/
			public List<int> indices;				/**< List of cells.*/

			public Frame(){
				time = 0;
				positions = new List<Vector3>();
				indices = new List<int>();
			}

			public int SizeInBytes(){
				return sizeof(float) + 
					   positions.Count * sizeof(float) * 3 + 
					   indices.Count * sizeof(int);
			}

			/**
			 * Interpolates two frames and returns the result.
			 */
			public static Frame Lerp(Frame a, Frame b, float mu){
	
				Frame f = new Frame();

				// Since both indices arrays are guaranteed to be sorted,
				// we can find and interpolate common particles in linear time.
				int i = 0; 
				int j = 0;
				while(i < a.indices.Count && j < b.indices.Count){

					int indexa = a.indices[i];
					int indexb = b.indices[j];
					
					if (indexa > indexb){ //only exists in b.
						f.indices.Add(indexb);
						f.positions.Add(b.positions[j]);
						j++;
					}

					else if (indexa < indexb){ //only exists in a.
						f.indices.Add(indexa);
						f.positions.Add(a.positions[i]);
						i++;
					}

					else{ // common particle, interpolate:
						f.indices.Add(indexa);
						f.positions.Add(Vector3.Lerp(a.positions[i],
													 b.positions[j],mu));
						i++; j++;
					}
				}
				
				return f;
			}
		}

		public float referenceIntervalSeconds = 0.5f;			/**< Interval in seconds between frame references.*/
		public bool localSpace = true;							/**< If true, particle positions will be expressed in the solver's local space.*/
		[SerializeField] private float duration = 0;			/**< Amount of baked time in seconds.*/
		[SerializeField] private List<Frame> frames;			/**< List of frames.*/
		[SerializeField] private List<int> references;			/**< List of reference frame indices. Used for eficient playback.*/

		public float Duration{
			get{return duration;}
		}
	
		public int FrameCount{
			get{return frames.Count;}
		}
		
		public void OnEnable(){

			if (frames == null)
				frames = new List<Frame>();

			if (references == null)
				references = new List<int>(){0};

    	}

		public int SizeInBytes(){
			int size = 0;
			foreach(Frame f in frames){
				size += f.SizeInBytes();
			}
			size += references.Count * sizeof(int);
			return size;
		}
		
		public void Clear(){

			duration = 0;
			frames.Clear();
			references.Clear();
			references.Add(0);

			#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
			#endif

		}

		private int GetBaseFrame(float time){
			int referenceIndex = Mathf.FloorToInt(time / referenceIntervalSeconds); 
			if (referenceIndex < references.Count)
				return references[referenceIndex];
			return int.MaxValue;
		}

		public void AddFrame(Frame frame){
			
			// Get reference frame index:
			int referenceIndex = Mathf.FloorToInt(frame.time / referenceIntervalSeconds); 

			// if the reference doesnt exist, create new references up to it.
			if (referenceIndex >= references.Count){
				
				// calculate how much time we need to fill with references:
				float unreferencedTime = frame.time - Mathf.Max(0,references.Count-1) * referenceIntervalSeconds;

				// generate reference frames to fill the gap:
				while (unreferencedTime >= referenceIntervalSeconds){
					references.Add(frames.Count);
					unreferencedTime -= referenceIntervalSeconds;
				}

			}
		
			// Append frame:
			if (frame.time >= duration){
				frames.Add(frame);
				duration = frame.time;
			}

			// Replace frame:
			else{

				int baseFrame = references[referenceIndex];
	
				// Get the first frame after the current one, and replace it.
				for (int nextFrame = baseFrame; nextFrame < frames.Count; ++nextFrame){

					if (frames[nextFrame].time > frame.time){
		
						frames[nextFrame] = frame;

						return;
			
					}

				}

			}

			#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
			#endif

		}

		/**
		 * Retrieves the frame for a given time. If the provided time is between two frames, performs
		 * linear interpolation of these frames.
		 */
		public Frame GetFrame(float time, bool interpolate){

			time = time % duration;

			int baseFrame = GetBaseFrame(time);

			// short linear search from base frame:
			for (int nextFrame = baseFrame; nextFrame < frames.Count; ++nextFrame){

				if (frames[nextFrame].time > time){

					if (interpolate){

						// Get previous frame:
						int prevFrame = Mathf.Max(0,nextFrame-1);
	
						// Calulate interpolation parameter:
						float mu = (time-frames[prevFrame].time)/(frames[nextFrame].time-frames[prevFrame].time);
	
						// Return interpolated frame:
						return Frame.Lerp(frames[prevFrame],frames[nextFrame],mu);

					}else{
						return frames[nextFrame];
					}

				}

			}
			return null;
		}

	}
}

