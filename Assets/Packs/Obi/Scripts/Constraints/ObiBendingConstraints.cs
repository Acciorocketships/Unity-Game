using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
 	* Holds information about bending constraints for an actor.
 	*/
	[DisallowMultipleComponent]
	public class ObiBendingConstraints : ObiConstraints
	{
		
		[Tooltip("Bending offset. Leave at zero to keep the original bending amount.")]
		public float maxBending = 0;				/**< Stiffness of structural spring constraints.*/
		
		[Range(0,1)]
		[Tooltip("Cloth resistance to bending. Higher values will yield more stiff cloth.")]
		public float stiffness = 1;		   /**< Resistance of structural spring constraints to stretch..*/
		
		[HideInInspector] public List<int> bendingIndices = new List<int>();				/**< Distance constraint indices.*/
		[HideInInspector] public List<float> restBends = new List<float>();					/**< Rest distances.*/
		[HideInInspector] public List<Vector2> bendingStiffnesses = new List<Vector2>();	/**< Bend offsets and stiffnesses of distance constraits.*/

		int[] solverIndices = new int[0];

		public override void Initialize(){
			activeStatus.Clear();
			bendingIndices.Clear();
			restBends.Clear();
			bendingStiffnesses.Clear();
		}
		
		public void AddConstraint(bool active, int index1, int index2, int index3, float restBend, float bending, float stiffness){

			if (InSolver){
				Debug.LogError("You need to remove the constraints from the solver before attempting to add new individual constraints.");
				return;
			}

			activeStatus.Add(active);
			bendingIndices.Add(index1);
			bendingIndices.Add(index2);
			bendingIndices.Add(index3);
			restBends.Add(restBend);
			bendingStiffnesses.Add(new Vector2(bending,stiffness));
		}

		public void RemoveConstraint(int index){

			if (InSolver){
				Debug.LogError("You need to remove the constraints from the solver before attempting to remove individual constraints.");
				return;
			}
	
			activeStatus.RemoveAt(index);
			bendingIndices.RemoveRange(index*3,3);
			restBends.RemoveAt(index);
	        bendingStiffnesses.RemoveAt(index);
		}

		protected override Oni.ConstraintType GetConstraintType(){
			return Oni.ConstraintType.Bending;
		}

		protected override ObiSolverData GetParticleDataFlags(){
			return new ObiSolverData(ObiSolverData.BendingConstraintsData.ALL);
		}
		
		public override List<int> GetConstraintsInvolvingParticle(int particleIndex){
			List<int> constraints = new List<int>();
			
			for (int i = 0; i < restBends.Count; i++){
				if (bendingIndices[i*3] == particleIndex || 
					bendingIndices[i*3+1] == particleIndex || 
					bendingIndices[i*3+2] == particleIndex) 
					constraints.Add(i);
			}
			
			return constraints;
		}
		
		protected override void OnAddToSolver(object info){
			
			ObiSolver solver = actor.Solver;
			
			// Set solver constraint data:
			solverIndices = new int[bendingIndices.Count];
			for (int i = 0; i < restBends.Count; i++)
			{
				solverIndices[i*3] = actor.particleIndices[bendingIndices[i*3]];
				solverIndices[i*3+1] = actor.particleIndices[bendingIndices[i*3+1]];
				solverIndices[i*3+2] = actor.particleIndices[bendingIndices[i*3+2]];
			}

			Oni.SetBendingConstraints(solver.OniSolver,solverIndices,restBends.ToArray(),bendingStiffnesses.ToArray(),ConstraintCount,constraintOffset);

		}
		
		public override void PushDataToSolver(ObiSolverData data){ 
			
			if (actor == null || !actor.InSolver)
				return;
			
			if ((data.bendingConstraintsData & ObiSolverData.BendingConstraintsData.BENDING_STIFFNESSES) != 0){
				for (int i = 0; i < bendingStiffnesses.Count; i++){
					bendingStiffnesses[i] = new Vector2(maxBending,stiffness);
				}
			}

			Oni.SetBendingConstraints(actor.Solver.OniSolver,solverIndices,restBends.ToArray(),bendingStiffnesses.ToArray(),ConstraintCount,constraintOffset);
			
			if ((data.bendingConstraintsData & ObiSolverData.BendingConstraintsData.ACTIVE_STATUS) != 0){
				UpdateConstraintActiveStatus();
			}
			
		}

	}
}

