using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ObiParticleRenderer : MonoBehaviour
{
	public Color particleColor = Color.white; 
	public float radiusScale = 1;
	public ObiActor Actor{
		set{
			if (actor != value)
			{
				if (actor != null && actor.Solver != null)
				{
					actor.Solver.OnFrameEnd -= Actor_solver_OnFrameEnd;
				}
				actor = value;
				if (actor != null && actor.Solver != null)
				{
					actor.Solver.OnFrameEnd += Actor_solver_OnFrameEnd;
				}
			}
		}
		get{ return actor;}
	}

	[SerializeField][HideInInspector] private ObiActor actor;
	private Mesh mesh;
	private Renderer renderer;
	private MeshFilter filter;
	private Material material;

	// Particle buffers:
	private Vector3[] particlePositions = new Vector3[0];
	private Color[] particleColors = new Color[0];
	private float[] particleSizes = new float[0];
	private Vector2[] particleInfo = new Vector2[0];

	// Geometry buffers:
	private Vector3[] vertices = new Vector3[0];
	private Vector3[] normals = new Vector3[0];
	private Vector2[] uv = new Vector2[0];
	private Vector2[] uv2 = new Vector2[0];
	private Color[] colors = new Color[0];
	int[] triangles = new int[0];

	public Mesh ParticleMesh{
		get{return mesh;}
	}

	public void OnEnable(){

		renderer = GetComponent<Renderer>();
		filter = GetComponent<MeshFilter>();

		this.mesh = new Mesh();
		mesh.name = "Particle imposters";
		mesh.hideFlags = HideFlags.HideAndDontSave;
		mesh.MarkDynamic();
	
		filter.sharedMesh = mesh;

	 	material = new Material(Shader.Find("Obi/Particles"));
		material.hideFlags = HideFlags.HideAndDontSave;
		renderer.sharedMaterial = material;

		if (actor != null && actor.Solver != null)
		{
			actor.Solver.OnFrameEnd += Actor_solver_OnFrameEnd;
		}

	}

	void Actor_solver_OnFrameEnd (object sender, EventArgs e)
	{
		if (actor == null || !actor.InSolver || !actor.isActiveAndEnabled)
			return;

		// Update particle renderer values:
		List<Color> colors = new List<Color>();
		List<Vector3> drawPos = new List<Vector3>();
		List<Vector2> info = new List<Vector2>();
		for (int i = 0; i < actor.particleIndices.Count; i++){
			if (actor.active[i]){
				drawPos.Add(transform.InverseTransformPoint(actor.Solver.renderablePositions[actor.particleIndices[i]]));
				colors.Add(Color.white);
				info.Add(new Vector2(0,actor.solidRadii[i]));
			}
		}
		
		SetParticles(drawPos.ToArray(),actor.solidRadii,colors.ToArray(),info.ToArray());
	}

	public void OnDisable(){

		if (actor != null && actor.Solver != null)
		{
			actor.Solver.OnFrameEnd -= Actor_solver_OnFrameEnd;
		}

		filter.sharedMesh = null;
		renderer.sharedMaterial = null;

		GameObject.DestroyImmediate(mesh);
		GameObject.DestroyImmediate(material);
	}

	private void Resize(int particleCount){
		Array.Resize(ref vertices,particleCount*4);
		Array.Resize(ref normals,particleCount*4);
		Array.Resize(ref uv,particleCount*4);
		Array.Resize(ref uv2,particleCount*4);
		Array.Resize(ref colors,particleCount*4);
		Array.Resize(ref triangles,particleCount*6);
	}

	public void SetParticles(Vector3[] positions, float[] sizes, Color[] colors, Vector2[] info){

		particlePositions = positions;
		particleSizes = sizes;
		particleColors = colors;
		particleInfo = info;

		Resize(particlePositions.Length);

		//Convert particle data to mesh geometry:		
		for(int i = 0; i < particlePositions.Length; i++)
		{
			SetParticle(i,particlePositions[i],
			            particleSizes[i]*radiusScale,
			            particleColors[i], 
                        particleInfo[i]*radiusScale);
		}
		
		Apply();

	}

	private void SetParticle(int i, Vector3 position, float size, Color color, Vector2 glowCoords){
		
		int i4 = i*4;
		int i41 = i4+1;
		int i42 = i4+2;
		int i43 = i4+3;
		int i6 = i*6;
		Color pColor = color * particleColor;

		normals[i4] = new Vector3(size,size,0);
		normals[i41] = new Vector3(-size,size,0);
		normals[i42] = new Vector3(-size,-size,0);
		normals[i43] = new Vector3(size,-size,0);
		
		vertices[i4] = position;
		vertices[i41] = position;
		vertices[i42] = position;
		vertices[i43] = position;

		uv[i4] = Vector2.one;
		uv[i41] = Vector2.up;
		uv[i42] = Vector2.zero;
		uv[i43] = Vector2.right;

		uv2[i4] = glowCoords;
		uv2[i41] = glowCoords;
		uv2[i42] = glowCoords;
		uv2[i43] = glowCoords;
		
		colors[i4] = pColor;
		colors[i41] = pColor;
		colors[i42] = pColor;
        colors[i43] = pColor;
		
		triangles[i6] = i42;
		triangles[i6+1] = i41;
		triangles[i6+2] = i4;
		triangles[i6+3] = i43;
        triangles[i6+4] = i42;
        triangles[i6+5] = i4;
    }

	private void Apply(){
		if (mesh == null) return;
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uv;
		mesh.uv2 = uv2;
		mesh.colors = colors;
		mesh.triangles = triangles;
		mesh.RecalculateBounds();
    }

}
}

