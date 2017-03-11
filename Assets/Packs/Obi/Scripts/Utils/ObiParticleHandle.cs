using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

/**
 * Small helper class that allows particles to be (individually or in group) parented to a GameObject.
 */ 
[ExecuteInEditMode]
public class ObiParticleHandle : MonoBehaviour {

	[SerializeField][HideInInspector] private ObiActor actor;
	[SerializeField][HideInInspector] private List<int> handledParticleIndices = new List<int>();
	[SerializeField][HideInInspector] private List<Vector3> handledParticlePositions = new List<Vector3>();
	[SerializeField][HideInInspector] private List<float> handledParticleInvMasses = new List<float>();

	public int ParticleCount{
		get{return handledParticleIndices.Count;}
	}

	public ObiActor Actor{
		set{
			if (actor != value)
			{
				if (actor != null && actor.Solver != null)
				{
					actor.Solver.OnFixedParticlesUpdated -= Actor_solver_OnFixedParticlesUpdated;
				}
				actor = value;
				if (actor != null && actor.Solver != null)
				{
					actor.Solver.OnFixedParticlesUpdated += Actor_solver_OnFixedParticlesUpdated;
				}
			}
		}
		get{ return actor;}
	}

	void OnEnable(){
		if (actor != null && actor.Solver != null)
		{
			actor.Solver.OnFixedParticlesUpdated += Actor_solver_OnFixedParticlesUpdated;
		}
	}

	void OnDisable(){
		if (actor != null && actor.Solver != null)
		{
			actor.Solver.OnFixedParticlesUpdated -= Actor_solver_OnFixedParticlesUpdated;

			// Reset handled particles to their original mass:
			if (actor.InSolver){
				float[] invMass = new float[1];
				for (int i = 0; i < handledParticleIndices.Count; ++i)
				{
					int solverParticleIndex = actor.particleIndices[handledParticleIndices[i]];
		
					invMass[0] = actor.invMasses[handledParticleIndices[i]] = handledParticleInvMasses[i];
					Oni.SetParticleInverseMasses(actor.Solver.OniSolver,invMass,1,solverParticleIndex);
				}
			}
		}
	}

	public void AddParticle(int index, Vector3 position, float invMass){
		handledParticleIndices.Add(index);
		handledParticlePositions.Add(transform.InverseTransformPoint(position));
		handledParticleInvMasses.Add(invMass);
	}

	void Actor_solver_OnFixedParticlesUpdated (object sender, System.EventArgs e)
	{
		if (actor.InSolver){

			Vector4[] pos = new Vector4[1];
			Vector4[] vel = new Vector4[]{Vector4.zero};
			float[] invMass = new float[]{0};

			for (int i = 0; i < handledParticleIndices.Count; ++i){

				int solverParticleIndex = actor.particleIndices[handledParticleIndices[i]];

				// handled particles should always stay fixed:
				actor.velocities[handledParticleIndices[i]] = Vector3.zero;
				actor.invMasses[handledParticleIndices[i]] = 0;
				Oni.SetParticleVelocities(actor.Solver.OniSolver,vel,1,solverParticleIndex);
				Oni.SetParticleInverseMasses(actor.Solver.OniSolver,invMass,1,solverParticleIndex);

				// set particle position:
				pos[0] = transform.TransformPoint(handledParticlePositions[i]);
				Oni.SetParticlePositions(actor.Solver.OniSolver,pos,1,solverParticleIndex);
				
			}

		}
	}

}
}
