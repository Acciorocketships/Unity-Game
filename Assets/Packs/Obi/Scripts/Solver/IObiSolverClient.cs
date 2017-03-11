using UnityEngine;
using System.Collections;

namespace Obi{

	/**
   	 * Interface for components that want to benefit from the simulation capabilities of an ObiSolver.
	 */
	public interface IObiSolverClient
	{
		bool AddToSolver(object info);
		bool RemoveFromSolver(object info);
		void PushDataToSolver(ObiSolverData data);
		void PullDataFromSolver(ObiSolverData data);
	}
}

