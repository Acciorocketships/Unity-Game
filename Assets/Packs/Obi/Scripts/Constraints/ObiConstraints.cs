using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Obi{

/**
 * Class to hold per-actor information for a kind of constraints.
 *
 * You can only add or remove constraints when the actor is not in the solver. If you need to continously
 * add and remove constraints, the best approach is to reserve a bunch of constraints beforehand and then
 * individually activate/deactivate/update them.
 */
public abstract class ObiConstraints : MonoBehaviour, IObiSolverClient 
{	

	[NonSerialized] protected ObiActor actor;
	[NonSerialized] protected int constraintOffset;
	[NonSerialized] protected bool inSolver;

	[HideInInspector] public List<bool> activeStatus = new List<bool>();		/**< activation flag for each constraint.*/

	public ObiActor Actor{
		get{return actor;}
	}

	public bool InSolver{
		get{return inSolver;}
	}

	public int ConstraintCount{
		get{return activeStatus.Count;}
	}

	public int ConstraintOffset{
		get{return constraintOffset;}
	}

	public abstract void Initialize();

	/**
	 * Returns a list of all constraint indices involving at least one the provided particle indices.
	 */
	public abstract List<int> GetConstraintsInvolvingParticle(int particleIndex);

	protected abstract Oni.ConstraintType GetConstraintType();
	protected abstract ObiSolverData GetParticleDataFlags();
	protected abstract void OnAddToSolver(object info);

	public virtual void PushDataToSolver(ObiSolverData data){}
	public virtual void PullDataFromSolver(ObiSolverData data){}

	public bool AddToSolver(object info){

		if (inSolver || actor == null || !actor.InSolver)
			return false;

		// Calculate our constraint offset:
		constraintOffset = 0;
		for (int i = 0; i < actor.actorID; i++){
			ObiConstraints c = actor.Solver.actors[i].GetComponent(GetType()) as ObiConstraints;
			if (c != null)
				constraintOffset += c.ConstraintCount;
		}

		// custom addition code:
		OnAddToSolver(info);

		inSolver = true;

		// push data to solver:
		PushDataToSolver(GetParticleDataFlags());

		// update constraints if enabled, deactivate all constraints otherwise.
		if (enabled)
			UpdateConstraintActiveStatus();
		else
			Oni.DeactivateConstraints(actor.Solver.OniSolver,
								   	  (int)GetConstraintType(),
								      Enumerable.Range(constraintOffset,constraintOffset + ConstraintCount).ToArray(),
								  	  ConstraintCount);
		
		return true;

	}
	public bool RemoveFromSolver(object info){

		if (!inSolver || actor == null || !actor.InSolver)
			return false;

		// Update other actor's constraint offset:
		for (int i = actor.actorID+1; i < actor.Solver.actors.Count; i++){
			ObiConstraints c = actor.Solver.actors[i].GetComponent(GetType()) as ObiConstraints;
			if (c != null)
				c.constraintOffset -= ConstraintCount;
		}

		Oni.RemoveConstraints(actor.Solver.OniSolver,(int)GetConstraintType(),ConstraintCount,constraintOffset);

		inSolver = false;

		return true;

	}
	
	/**
	 * Updates the activation status of all (active/inactive) constraints in the solver. Active constraints will
	 * only be marked as active in the solver if this component is enabled, they will be deactivated otherwise.
	 */
	public void UpdateConstraintActiveStatus(){
	
		if (actor == null || !actor.InSolver)
			return;

		// Calculate solver active indices:
		List<int> solverActiveIndices = new List<int>();
		List<int> solverInactiveIndices = new List<int>();
		for (int i = 0;i < ConstraintCount; ++i)
		{
		   if (activeStatus[i])
		   		solverActiveIndices.Add(constraintOffset+i);
		   else 
				solverInactiveIndices.Add(constraintOffset+i);
		}

		Oni.DeactivateConstraints(actor.Solver.OniSolver,
								 (int)GetConstraintType(),
								 solverInactiveIndices.ToArray(),
								 solverInactiveIndices.Count);

		Oni.ActivateConstraints(actor.Solver.OniSolver,
								(int)GetConstraintType(),
								solverActiveIndices.ToArray(),
								solverActiveIndices.Count);
		
	}

	/**
	 * When enabling constraints, active constraints get activated in the solver.
	 */
	public void OnEnable(){
		
		// get the actor in this gameobject.
		actor = GetComponent<ObiActor>();
		
		UpdateConstraintActiveStatus();
		
	}
	
	/**
 	 * When disabling constraints, all individual constraints get deactivated in the solver.
 	 */
	public void OnDisable(){

		if (actor == null || !actor.InSolver)
			return;

		Oni.DeactivateConstraints(actor.Solver.OniSolver,
								  (int)GetConstraintType(),
								  Enumerable.Range(constraintOffset,constraintOffset + ConstraintCount).ToArray(),
								  ConstraintCount);
		
	}
	
	public void OnDestroy(){
		RemoveFromSolver(null);
	}
}
}

