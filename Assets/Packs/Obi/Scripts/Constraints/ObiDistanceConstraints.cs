using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

/**
 * Holds information about distance constraints for an actor.
 */
[DisallowMultipleComponent]
public class ObiDistanceConstraints : ObiConstraints
{

	[Range(0.1f,2)]
	[Tooltip("Scale of stretching constraints. Values > 1 will expand initial cloth size, values < 1 will make it shrink.")]
	public float stretchingScale = 1;				/**< Stiffness of structural spring constraints.*/
	
	[Range(0,2)]
	[Tooltip("Cloth resistance to stretching. Lower values will yield more elastic cloth.")]
	public float stretchingStiffness = 1;		   /**< Resistance of structural spring constraints to stretch..*/
	
	[Range(0,2)]
	[Tooltip("Cloth resistance to compression. Lower values will yield more elastic cloth.")]
	public float compressionStiffness = 1;		   /**< Resistance of structural spring constraints to compression.*/
		
	[HideInInspector] public List<int> springIndices = new List<int>();					/**< Distance constraint indices.*/
	[HideInInspector] public List<float> restLengths = new List<float>();				/**< Rest distances.*/
	[HideInInspector] public List<Vector2> stiffnesses = new List<Vector2>();			/**< Stiffnesses of distance constraits.*/

	[HideInInspector][NonSerialized] public float[] stretching = new float[0];
	int[] solverIndices = new int[0];

	public override void Initialize(){
		activeStatus.Clear();
		springIndices.Clear();
		restLengths.Clear();
		stiffnesses.Clear();	
	}

	public void AddConstraint(bool active, int index1, int index2, float restLength, float stretchStiffness, float compressionStiffness){

		if (InSolver){
			Debug.LogError("You need to remove the constraints from the solver before attempting to add new individual constraints.");
			return;
		}

		activeStatus.Add(active);
		springIndices.Add(index1);
		springIndices.Add(index2);
		restLengths.Add(restLength);
        stiffnesses.Add(new Vector2(stretchStiffness,compressionStiffness));
	}

	public void RemoveConstraint(int index){

		if (InSolver){
			Debug.LogError("You need to remove the constraints from the solver before attempting to remove individual constraints.");
			return;
		}

		activeStatus.RemoveAt(index);
		springIndices.RemoveRange(index*2,2);
		restLengths.RemoveAt(index);
        stiffnesses.RemoveAt(index);
	}

	protected override Oni.ConstraintType GetConstraintType(){
		return Oni.ConstraintType.Distance;
	}

	protected override ObiSolverData GetParticleDataFlags(){
		return new ObiSolverData(ObiSolverData.DistanceConstraintsData.ALL);
	}
	
	public override List<int> GetConstraintsInvolvingParticle(int particleIndex){
	
		List<int> constraints = new List<int>();
		
		for (int i = 0; i < ConstraintCount; i++){
			if (springIndices[i*2] == particleIndex || springIndices[i*2+1] == particleIndex) 
				constraints.Add(i);
		}
		
		return constraints;
	}

	protected override void OnAddToSolver(object info){

		ObiSolver solver = actor.Solver;

		// Set solver constraint data:
		solverIndices = new int[springIndices.Count];
		for (int i = 0; i < restLengths.Count; i++)
		{
			solverIndices[i*2] = actor.particleIndices[springIndices[i*2]];
			solverIndices[i*2+1] = actor.particleIndices[springIndices[i*2+1]];
		}

		// Add constraints:
		Oni.SetDistanceConstraints(solver.OniSolver,solverIndices,restLengths.ToArray(),stiffnesses.ToArray(),ConstraintCount,constraintOffset);
	}

	public override void PushDataToSolver(ObiSolverData data){ 

		if (actor == null || !actor.InSolver)
			return;

		float[] scaledRestLengths = new float[restLengths.Count];		
		if ((data.distanceConstraintsData & ObiSolverData.DistanceConstraintsData.DISTANCE_REST_LENGHTS) != 0){
			for (int i = 0; i < restLengths.Count; i++){
				scaledRestLengths[i] = restLengths[i]*stretchingScale;
			}
		}

		if ((data.distanceConstraintsData & ObiSolverData.DistanceConstraintsData.DISTANCE_STIFFNESSES) != 0){
			for (int i = 0; i < stiffnesses.Count; i++){
				stiffnesses[i] = new Vector2(stretchingStiffness,compressionStiffness);
			}
		}

		Oni.SetDistanceConstraints(actor.Solver.OniSolver,solverIndices,scaledRestLengths,stiffnesses.ToArray(),ConstraintCount,constraintOffset);

		if ((data.distanceConstraintsData & ObiSolverData.DistanceConstraintsData.ACTIVE_STATUS) != 0){
			UpdateConstraintActiveStatus();
		}

	}

	public override void PullDataFromSolver(ObiSolverData data){
		if (actor != null && actor.Solver != null && stretching != null){
			stretching = new float[ConstraintCount];
			if ((data.distanceConstraintsData & ObiSolverData.DistanceConstraintsData.DISTANCE_STRETCH) != 0){
				Oni.GetDistanceConstraintsStretching(actor.Solver.OniSolver,stretching,ConstraintCount,ConstraintOffset);
			}
		}
	}	

}
}
