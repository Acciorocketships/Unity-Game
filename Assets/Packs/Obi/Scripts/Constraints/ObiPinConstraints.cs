using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

/**
 * Holds information about pin constraints for an actor.
 */
[DisallowMultipleComponent]
public class ObiPinConstraints : ObiConstraints
{
	
	[Range(0,1)]
	[Tooltip("Pin resistance to stretching. Lower values will yield more elastic pin constraints.")]
	public float stiffness = 1;		   /**< Resistance of structural spring constraints to stretch..*/
	
	public List<int> pinParticleIndices = new List<int>();			/**< Pin constraint indices.*/
	public List<Collider> pinBodies = new List<Collider>();			/**< Stiffnesses of pin constraits.*/
	public List<Vector4> pinOffsets = new List<Vector4>();			/**< Offset expressed in the attachment's local space.*/
	public List<float> stiffnesses = new List<float>();				/**< Stiffnesses of pin constraits.*/

	int[] solverIndices = new int[0];

	public override void Initialize(){
		activeStatus.Clear();
		pinParticleIndices.Clear();
		pinBodies.Clear();
		pinOffsets.Clear();
		stiffnesses.Clear();	
	}

	public void AddConstraint(bool active, int index1, Collider body, Vector3 offset, float stiffness){

		if (InSolver){
			Debug.LogError("You need to remove the constraints from the solver before attempting to add new individual constraints.");
			return;
		}

		activeStatus.Add(active);
		pinParticleIndices.Add(index1);
		pinBodies.Add(body);
		pinOffsets.Add(offset);
        stiffnesses.Add(stiffness);

	}

	public void RemoveConstraint(int index){

		if (index >= 0 && index < pinOffsets.Count){
			activeStatus.RemoveAt(index);
			pinParticleIndices.RemoveAt(index);
			pinBodies.RemoveAt(index);
			pinOffsets.RemoveAt(index);
			stiffnesses.RemoveAt(index);
		}

	}

	protected override Oni.ConstraintType GetConstraintType(){
		return Oni.ConstraintType.Pin;
	}

	protected override ObiSolverData GetParticleDataFlags(){
		return new ObiSolverData(ObiSolverData.PinConstraintsData.ALL);
	}
	
	public override List<int> GetConstraintsInvolvingParticle(int particleIndex){
	
		List<int> constraints = new List<int>();
		
		for (int i = 0; i < pinOffsets.Count; i++){
			if (pinParticleIndices[i*2] == particleIndex) 
				constraints.Add(i);
		}
		
		return constraints;
	}

	protected override void OnAddToSolver(object info){

		ObiSolver solver = actor.Solver;

		// Set solver constraint data:
		solverIndices = new int[pinParticleIndices.Count*2];
		for (int i = 0; i < pinOffsets.Count; i++)
		{
			solverIndices[i*2] = actor.particleIndices[pinParticleIndices[i]];
			
			if (actor.Solver.colliderGroup != null) 
				solverIndices[i*2+1] = actor.Solver.colliderGroup.GetIndexOfCollider(pinBodies[i]);
			else
				solverIndices[i*2+1] = -1;
		}

		Oni.SetPinConstraints(solver.OniSolver,solverIndices,pinOffsets.ToArray(),stiffnesses.ToArray(),ConstraintCount,constraintOffset);

	}

	public override void PushDataToSolver(ObiSolverData data){ 

		if (actor == null || !actor.InSolver)
			return;

		if ((data.pinConstraintsData & ObiSolverData.PinConstraintsData.PIN_STIFFNESSES) != 0){
			for (int i = 0; i < stiffnesses.Count; i++){
				stiffnesses[i] = stiffness;
			}
		}

		Oni.SetPinConstraints(actor.Solver.OniSolver,solverIndices,pinOffsets.ToArray(),stiffnesses.ToArray(),ConstraintCount,constraintOffset);

		if ((data.pinConstraintsData & ObiSolverData.PinConstraintsData.ACTIVE_STATUS) != 0){
			UpdateConstraintActiveStatus();
		}

	}

}
}
