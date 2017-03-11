using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace Obi
{
public static class ObiUtils
{

	/**
	 * Same as AddRange for Lists, but for arrays which are conveniently a blittable type.
     */
	public static void AddRange<T>(ref T[] array, T[] other){

		if (array == null || other == null) return;

		int blitStart = array.Length;
		System.Array.Resize(ref array,array.Length + other.Length);
		other.CopyTo(array,blitStart);

	}

	/**
	 * Same as RemoveRange for Lists, but for arrays which are conveniently a blittable type.
     */
	public static void RemoveRange<T>(ref T[] array, int index, int count){

		if (array == null) return;
	
		if (index < 0 || count < 0){
			throw new System.ArgumentOutOfRangeException("Index and/or count are < 0.");
		}

		if (index + count > array.Length){
			throw new System.ArgumentException("Index and count do not denote a valid range of elements.");
		}

		for (int i = index; i < array.Length - count; i++){
			array.SetValue(array.GetValue(i + count),i);
		}
		System.Array.Resize (ref array,array.Length - count);

	}

	public static float Remap (this float value, float from1, float to1, float from2, float to2) {
		return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
	}

	/**
	 * Calculates the area of a triangle.
	 */
	public static float TriangleArea(Vector3 p1, Vector3 p2, Vector3 p3){
		return Mathf.Sqrt(Vector3.Cross(p2-p1,p3-p1).sqrMagnitude) / 2f;
	}
}
}

