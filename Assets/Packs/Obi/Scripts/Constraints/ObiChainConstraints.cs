using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
 	 * Holds information about volume constraints for an actor.
 	 */
	[DisallowMultipleComponent]
	public class ObiChainConstraints : ObiConstraints 
	{
		
		[Range(0,1)]
		[Tooltip("Low tightness values allow the chain to contract.")]
		public float tightness = 1;		   /**< Inverse of the percentage of contraction allowed for chain segments.*/
		
		[HideInInspector] [NonSerialized] int chainParticlesOffset;	/**< start of particle indices in solver*/

		[HideInInspector] public List<int> particleIndices = new List<int>();			/**< Triangle indices.*/
		[HideInInspector] public List<int> firstParticle = new List<int>();				/**< index of first triangle for each constraint.*/
		[HideInInspector] public List<int> numParticles = new List<int>();				/**< num of triangles for each constraint.*/

		[HideInInspector] public List<Vector2> lengths = new List<Vector2>();			/**< min/max lenghts for each constraint.*/
		
		int[] solverIndices;
		int[] solverFirstIndex;

		/**
		 * Initialize with the total amount of triangles used by all constraints, and the number of constraints.
		 */
		public override void Initialize(){
			activeStatus.Clear();
			particleIndices.Clear();
			firstParticle.Clear();
			numParticles.Clear();
			lengths.Clear();
		}
		

		public void AddConstraint(bool active,  int[] indices, float restLength, float stretchStiffness, float compressionStiffness){

			if (InSolver){
				Debug.LogError("You need to remove the constraints from the solver before attempting to add new individual constraints.");
				return;
			}

			activeStatus.Add(active);

			firstParticle.Add((int)particleIndices.Count);
			numParticles.Add((int)indices.Length);

			particleIndices.AddRange(indices);

			lengths.Add(new Vector2(restLength*tightness,restLength));
			
		}

		protected override Oni.ConstraintType GetConstraintType(){
			return Oni.ConstraintType.Chain;
		}

		protected override ObiSolverData GetParticleDataFlags(){
			return new ObiSolverData(ObiSolverData.ChainConstraintsData.ALL);
		}
		
		public override List<int> GetConstraintsInvolvingParticle(int particleIndex){
		
			List<int> constraints = new List<int>();
			
			for (int i = 0; i < firstParticle.Count; i++){
			
				/*for (int j = 0; j < numParticles[i]; j++){
					if (particleIndices[firstParticle[i]+j] == particleIndex) 
						constraints.Add(i);
				}*/
				
			}
			
			return constraints;
		}
		
		protected override void OnAddToSolver(object info){
			
			ObiSolver solver = actor.Solver;
			
			// Set solver constraint data:
			solverIndices = new int[particleIndices.Count];
			for (int i = 0; i < particleIndices.Count; i++)
			{
				solverIndices[i] = actor.particleIndices[particleIndices[i]];
			}

			solverFirstIndex = new int[firstParticle.Count];
			for (int i = 0; i < firstParticle.Count; i++)
			{
				solverFirstIndex[i] = Oni.GetChainParticleCount(solver.OniSolver) + firstParticle[i];
			}

			Oni.SetChainConstraints(solver.OniSolver,solverIndices,
												  	  solverFirstIndex,
											     	  numParticles.ToArray(),
												      lengths.ToArray(),
												  	  ConstraintCount,constraintOffset);
			
		}
		
		public override void PushDataToSolver(ObiSolverData data){ 
			
			if (actor == null || !actor.InSolver)
				return;
			
			if ((data.chainConstraintsData & ObiSolverData.ChainConstraintsData.CHAIN_LENGTHS) != 0){

				for (int i = 0; i < lengths.Count; i++){
					lengths[i] = new Vector2(lengths[i].y*tightness,lengths[i].y);
				}

			}

			Oni.SetChainConstraints(actor.Solver.OniSolver,solverIndices,
												  solverFirstIndex,
											      numParticles.ToArray(),
												  lengths.ToArray(),
												  ConstraintCount,constraintOffset);

			if ((data.chainConstraintsData & ObiSolverData.ChainConstraintsData.ACTIVE_STATUS) != 0){
				UpdateConstraintActiveStatus();
			}
			
		}
		
	}
}





