using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Obi
{
	/**
	 * Helper class that holds data for a mesh, allowing it to be modified in several ways
	 * before applying any changes back to the mesh. This avoid copying arrays back and forth
	 * for every change.
	 */
	public class MeshBuffer
	{
	
		Mesh mesh;
		public Vector3[] vertices;
		public int[] triangles;
		public Vector3[] normals;
		public Vector4[] tangents;
		public Vector2[] uv;
		public Vector2[] uv2;
		public Vector2[] uv3;
		public Vector2[] uv4;
		public Color32[] colors;

		public int vertexCount{
			get{return vertices.Length;}
		}
	
		// Use this for initialization
		public MeshBuffer(Mesh mesh){
			if (mesh == null)
				throw new InvalidOperationException("Cannot create a mesh buffer from an empty mesh.");
			this.mesh = mesh;
			vertices = mesh.vertices;
			triangles = mesh.triangles;
			normals = mesh.normals;
			tangents = mesh.tangents;
			uv = mesh.uv;
			uv2 = mesh.uv2;
			uv3 = mesh.uv3;
			uv4 = mesh.uv4;
			colors = mesh.colors32;
		}
		
		// Update is called once per frame
		public void Apply(){
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.normals = normals;
			mesh.tangents = tangents;
			mesh.uv = uv;
			mesh.uv2 = uv2;
			mesh.uv3 = uv3;
			mesh.uv4 = uv4;
			mesh.colors32 = colors;
		}

		public void AddVertex(int sourceVertexIndex){

			if (sourceVertexIndex < 0 || sourceVertexIndex >= vertices.Length)
				throw new InvalidOperationException("Invalid source vertex index.");

			int newSize = vertexCount + 1;

			Array.Resize(ref vertices,newSize);
			vertices[vertices.Length-1] = vertices[sourceVertexIndex];

			if (normals != null){
				Array.Resize(ref normals,newSize);
				normals[normals.Length-1] = normals[sourceVertexIndex];
			}
			if (tangents != null){
				Array.Resize(ref tangents,newSize);
				tangents[tangents.Length-1] = tangents[sourceVertexIndex];
			}
			if (uv != null){
				Array.Resize(ref uv,newSize);
				uv[uv.Length-1] = uv[sourceVertexIndex];
			}
			if (uv2 != null){
				Array.Resize(ref uv2,newSize);
				uv2[uv.Length-1] = uv2[sourceVertexIndex];
			}
			if (uv3 != null){
				Array.Resize(ref uv3,newSize);
				uv3[uv.Length-1] = uv3[sourceVertexIndex];
			}		
			if (uv4 != null){
				Array.Resize(ref uv4,newSize);
				uv4[uv.Length-1] = uv4[sourceVertexIndex];
			}	
			if (colors != null){
				Array.Resize(ref colors,newSize);
				colors[uv.Length-1] = colors[sourceVertexIndex];
			}

		}
	}
}

