using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
 	* Holds information about aerodynamic constraints for an actor.
 	*/
	[DisallowMultipleComponent]
	public class ObiAerodynamicConstraints : ObiConstraints
	{
		
		[Tooltip("Direction and magnitude of wind in word space.")]
		public Vector3 windVector = Vector3.zero;				/**< Wind force vector expressed in world space.*/
		
		[Tooltip("Air density in kg/m3. Higher densities will make both drag and lift forces stronger.")]
		public float airDensity = 1.225f;
		
		[Tooltip("How much is the cloth affected by drag forces. Extreme values can cause the cloth to behave unrealistically, so use with care.")]
		public float dragCoefficient = 0.05f;
		
		[Tooltip("How much is the cloth affected by lift forces. Extreme values can cause the cloth to behave unrealistically, so use with care.")]
		public float liftCoefficient = 0.05f;
		
		[HideInInspector] public List<int> aerodynamicIndices = new List<int>();				/**< particle indices.*/
		[HideInInspector] public List<Vector4> aerodynamicNormals = new List<Vector4>();		/**< particle normals.*/
		[HideInInspector] public List<Vector4> wind = new List<Vector4>();						/**< Per-particle wind vector.*/
		[HideInInspector] public List<float> aerodynamicCoeffs = new List<float>();				/**< Per-particle aerodynamic coeffs.*/

		int[] solverIndices = new int[0];

		public override void Initialize(){
			activeStatus.Clear();
			aerodynamicNormals.Clear();
			aerodynamicIndices.Clear();
			wind.Clear();
			aerodynamicCoeffs.Clear();
		}
		
		public void AddConstraint(bool active, int index, Vector3 normal, Vector3 wind, float area, float drag, float lift){

			if (InSolver){
				Debug.LogError("You need to remove the constraints from the solver before attempting to add new individual constraints.");
				return;
			}

			activeStatus.Add(active);
			aerodynamicIndices.Add(index);
			aerodynamicNormals.Add(normal);
			this.wind.Add(wind);
			aerodynamicCoeffs.Add(area);
			aerodynamicCoeffs.Add(drag);
			aerodynamicCoeffs.Add(lift);
		}
		
		protected override Oni.ConstraintType GetConstraintType(){
			return Oni.ConstraintType.Aerodynamics;
		}

		protected override ObiSolverData GetParticleDataFlags(){
			return new ObiSolverData(ObiSolverData.AerodynamicConstraintsData.ALL);
		}

		public override List<int> GetConstraintsInvolvingParticle(int particleIndex){
		
			List<int> constraints = new List<int>();
			
			for (int i = 0; i < wind.Count; i++){
				if (aerodynamicIndices[i] == particleIndex)
					constraints.Add(i);
			}
			
			return constraints;
		}
		
		protected override void OnAddToSolver(object info){
		
			ObiSolver solver = actor.Solver;
			
			// Set solver constraint data:
			solverIndices = new int[aerodynamicIndices.Count];
			for (int i = 0; i < aerodynamicNormals.Count; i++)
			{
				solverIndices[i] = actor.particleIndices[aerodynamicIndices[i]];
			}

			// Add constraints:
			Oni.SetAerodynamicConstraints(solver.OniSolver,solverIndices,aerodynamicNormals.ToArray(),wind.ToArray(),aerodynamicCoeffs.ToArray(),ConstraintCount,constraintOffset);
			
		}
		
		public override void PushDataToSolver(ObiSolverData data){ 
			
			if (actor == null || !actor.InSolver)
				return;

			if ((data.aerodynamicConstraintsData & ObiSolverData.AerodynamicConstraintsData.WIND) != 0){
				for (int i = 0; i < wind.Count; i++){
					wind[i] = windVector;
				}
			}
			
			if ((data.aerodynamicConstraintsData & ObiSolverData.AerodynamicConstraintsData.AERODYNAMIC_COEFFS) != 0){
				for (int i = 0; i < aerodynamicCoeffs.Count; i+=3){
					aerodynamicCoeffs[i+1] = dragCoefficient * airDensity;
					aerodynamicCoeffs[i+2] = liftCoefficient * airDensity;
				}
			}

			if (data.aerodynamicConstraintsData == ObiSolverData.AerodynamicConstraintsData.ACTIVE_STATUS)
				// special case for normals only, which is pretty common.
				Oni.UpdateAerodynamicNormals(actor.Solver.OniSolver,aerodynamicNormals.ToArray(),ConstraintCount,constraintOffset);
			else
				Oni.SetAerodynamicConstraints(actor.Solver.OniSolver,solverIndices,aerodynamicNormals.ToArray(),wind.ToArray(),aerodynamicCoeffs.ToArray(),ConstraintCount,constraintOffset);

			if ((data.aerodynamicConstraintsData & ObiSolverData.AerodynamicConstraintsData.ACTIVE_STATUS) != 0){
				UpdateConstraintActiveStatus();
			}
			
		}
	
	}
}




