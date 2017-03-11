using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
 	* Holds information about tether constraints for an actor.
 	*/
	[DisallowMultipleComponent]
	public class ObiTetherConstraints : ObiConstraints
	{
		
		[Range(0.1f,2)]
		[Tooltip("Scale of tether constraints. Values > 1 will expand initial tether length, values < 1 will make it shrink.")]
		public float tetherScale = 1;				/**< Stiffness of structural spring constraints.*/
		
		[Range(0,1)]
		[Tooltip("Tether resistance to stretching. Lower values will enforce tethers with more strenght.")]
		public float stiffness = 1;		   /**< Resistance of structural spring constraints to stretch..*/
		
		
		[HideInInspector] public List<int> tetherIndices = new List<int>();					/**< Tether constraint indices.*/
		[HideInInspector] public List<Vector2> maxLengthsScales = new List<Vector2>();				/**< Max distance and scale for each tether.*/
		[HideInInspector] public List<float> stiffnesses = new List<float>();				/**< Stiffnesses of distance constraits.*/
		
		int[] solverIndices = new int[0];

		public override void Initialize(){
			activeStatus.Clear();
			tetherIndices.Clear();
			maxLengthsScales.Clear();
			stiffnesses.Clear();	
		}
		
		public void AddConstraint(bool active, int index1, int index2, float maxLength, float scale, float stiffness){
			
			if (InSolver){
				Debug.LogError("You need to remove the constraints from the solver before attempting to add new individual constraints.");
				return;
			}
			activeStatus.Add(active);
			tetherIndices.Add(index1);
			tetherIndices.Add(index2);
			maxLengthsScales.Add(new Vector2(maxLength,scale));
			stiffnesses.Add(stiffness);
		}

		protected override Oni.ConstraintType GetConstraintType(){
			return Oni.ConstraintType.Tether;
		}

		protected override ObiSolverData GetParticleDataFlags(){
			return new ObiSolverData(ObiSolverData.TetherConstraintsData.ALL);
		}
		
		public override List<int> GetConstraintsInvolvingParticle(int particleIndex){
			List<int> constraints = new List<int>();
			
			for (int i = 0; i < stiffnesses.Count; i++){
				if (tetherIndices[i*2] == particleIndex || tetherIndices[i*2+1] == particleIndex) 
					constraints.Add(i);
			}
			
			return constraints;
		}
		
		protected override void OnAddToSolver(object info){

			ObiSolver solver = actor.Solver;
			
			// Set solver constraint data:
			solverIndices = new int[tetherIndices.Count];
			for (int i = 0; i < maxLengthsScales.Count; i++)
			{
				solverIndices[i*2] = actor.particleIndices[tetherIndices[i*2]];
				solverIndices[i*2+1] = actor.particleIndices[tetherIndices[i*2+1]];
			}

			Oni.SetTetherConstraints(solver.OniSolver,solverIndices,maxLengthsScales.ToArray(),stiffnesses.ToArray(),ConstraintCount,constraintOffset);

		}

		public override void PushDataToSolver(ObiSolverData data){ 
			
			if (actor == null || !actor.InSolver)
				return;

			if ((data.tetherConstraintsData & ObiSolverData.TetherConstraintsData.TETHER_MAX_LENGHTS_SCALES) != 0){
				for (int i = 0; i < maxLengthsScales.Count; i++){
					maxLengthsScales[i] = new Vector2(maxLengthsScales[i].x, tetherScale);
				} 
			}
			
			if ((data.tetherConstraintsData & ObiSolverData.TetherConstraintsData.TETHER_STIFFNESSES) != 0){
				for (int i = 0; i < stiffnesses.Count; i++){
					stiffnesses[i] = stiffness;
				}
			}

			Oni.SetTetherConstraints(actor.Solver.OniSolver,solverIndices,maxLengthsScales.ToArray(),stiffnesses.ToArray(),ConstraintCount,constraintOffset);
			
			if ((data.tetherConstraintsData & ObiSolverData.TetherConstraintsData.ACTIVE_STATUS) != 0){
				UpdateConstraintActiveStatus();
			}
			
		}

	}
}
