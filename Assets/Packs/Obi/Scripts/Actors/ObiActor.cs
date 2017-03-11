using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Obi{

/**
 * Represents a group of related particles. ObiActor does not make
 * any assumptions about the relationship between these particles, except that they get allocated 
 * and released together.
 */
[DisallowMultipleComponent]
public abstract class ObiActor : MonoBehaviour, IObiSolverClient
{
	[Serializable]
	public class CollisionIgnoreList{
		public int[] ignoredParticleIndices;	

		public CollisionIgnoreList(int[] indices){
			ignoredParticleIndices = indices;
		}

		public int[] GetIndicesForActor(ObiActor actor){
			int[] ignored = new int[ignoredParticleIndices.Length];
			for (int j = 0; j < ignoredParticleIndices.Length; ++j){
				if (ignoredParticleIndices[j] >= 0 && ignoredParticleIndices[j] < actor.particleIndices.Count)
					ignored[j] = actor.particleIndices[ignoredParticleIndices[j]];
				else
					ignored[j] = ignoredParticleIndices[j];
			}
			return ignored;
		}
	}

	public event EventHandler OnAddedToSolver;
	public event EventHandler OnRemovedFromSolver;

	public ObiCollisionMaterial material;
	public bool selfCollisions = false;

	[HideInInspector][NonSerialized] public int actorID = -1; 						/**< actor ID in the solver..*/
	[HideInInspector][NonSerialized] public List<int> particleIndices;				/**< indices of allocated particles in the solver.*/
	[HideInInspector] public CollisionIgnoreList[] ignoredCollisions;		/**< Per particle collision ignore lists*/		
	
	[HideInInspector] public bool[] active;					/**< Particle activation status.*/
	[HideInInspector] public Vector3[] positions;			/**< Particle positions.*/
	[HideInInspector] public Vector3[] velocities;			/**< Particle velocities.*/
	[HideInInspector] public Vector3[] vorticities;			/**< Particle vorticities.*/
	[HideInInspector] public float[] invMasses;				/**< Particle inverse masses*/
	[HideInInspector] public float[] solidRadii;			/**< Particle solid radii (physical radius of each particle)*/
	[HideInInspector] public int[] phases;					/**< Particle phases.*/

	private bool inSolver = false;
	protected bool initializing = false;	
	[HideInInspector][SerializeField] protected ObiSolver solver;	
	[HideInInspector][SerializeField] protected bool initialized = false;


	private bool oldSelfCollisions = false;
	private int oldLayer = 0;

	public ObiSolver Solver{
		get{return solver;}
		set{
			if (solver != value){
				RemoveFromSolver(null);
				solver = value;
			}
		}
	}
	
	public bool Initializing{
		get{return initializing;}
	}
	
	public bool Initialized{
		get{return initialized;}
	}

	public bool InSolver{
		get{return inSolver;}
	}

	/**
	 * Max amount of tethers per particle. Default is 4, can be overriden by subclasses.
	 */
	public virtual int MaxTethers{ 
		get{return 4;}
	}

	public virtual void Awake(){
		oldLayer = gameObject.layer;
		oldSelfCollisions = selfCollisions;
    }

	/**
	 * Since Awake is not guaranteed to be called before OnEnable, we must add the mesh to the solver here.
	 */
	public virtual void Start(){
		if (Application.isPlaying)
			AddToSolver(null);
	}

	public virtual void OnDestroy(){
		RemoveFromSolver(null);
	}

	public abstract void DestroyRequiredComponents();

	/**
	 * Flags all particles allocated by this actor as active or inactive depending on the "active array".
	 * The solver will then only simulate the active ones.
	 */
	public virtual void OnEnable(){

		if (!InSolver) return;

		// update active status of all particles in the actor:
		for (int i = 0; i < particleIndices.Count; ++i){
			int k = particleIndices[i];
			if (!active[i])
				solver.activeParticles.Remove(k);
            else
                solver.activeParticles.Add(k);
		}
		solver.UpdateActiveParticles();
	}

	/**
	 * Flags all particles allocated by this actor as inactive, so the solver will not include them 
	 * in the simulation. To "teleport" the actor to a new position, disable it and then pull positions
	 * and velocities from the solver. Move it to the new position, and enable it.
	 */
	public virtual void OnDisable(){

		if (!InSolver) return;

		// flag all the actor's particles as disabled:
		for (int i = 0; i < particleIndices.Count; ++i){
			int k = particleIndices[i];
			solver.activeParticles.Remove(k);
		}
		solver.UpdateActiveParticles();

		// pull current position / velocity data from solver:
		PullDataFromSolver(new ObiSolverData(ObiSolverData.ParticleData.POSITIONS | ObiSolverData.ParticleData.VELOCITIES));

	}

	/**
	 * Resets the actor to its original state.
	 */
	public virtual void ResetActor(){
	}

	/**
	 * Updates particle phases in the solver.
	 */
	public virtual void UpdateParticlePhases(){

		if (!InSolver) return;

		for(int i = 0; i < particleIndices.Count; i++){
			phases[i] = Oni.MakePhase(gameObject.layer,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
		}
		PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.PHASES));
	}

	/**
	 * Adds this actor to a solver. No simulation will take place for this actor
 	 * unless it has been added to a solver. Returns true if the actor was succesfully added,
 	 * false if it was already added or couldn't add it for any other reason.
	 */
	public virtual bool AddToSolver(object info){
		
		if (solver != null && !InSolver){
			
			// Allocate particles in the solver:
			particleIndices = solver.AllocateParticles(positions.Length);
			if (particleIndices == null){
				Debug.LogWarning("Obi: Solver could not allocate enough particles for this actor. Please increase max particles.");
				return false;
			}

			inSolver = true;
			
			// Get an actor ID from the solver:
			actorID = solver.SetActor(actorID,this);

			// Update particle phases before sending data to the solver, as layers/flags settings might have changed.
			UpdateParticlePhases();
			
			// Send our particle data to the solver:
			PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.ALL));

			// Update collision ignore lists:
			for (int i = 0; i < ignoredCollisions.Length; ++i){
				Oni.SetIgnoredParticles(solver.OniSolver,ignoredCollisions[i].GetIndicesForActor(this),2,particleIndices[i]);
			}

			if (OnAddedToSolver != null)
				OnAddedToSolver(this,null);

			return true;
		}
		
		return false;
	}
	
	/**
	 * Adds this actor from its current solver, if any.
	 */
	public virtual bool RemoveFromSolver(object info){
		
		if (solver != null && InSolver){

			// remove collision ignore lists:
			for (int i = 0; i < particleIndices.Count; ++i){
				Oni.SetIgnoredParticles(solver.OniSolver,new int[]{},0,particleIndices[i]);
			}
			
			solver.FreeParticles(particleIndices);
			particleIndices = null;

			inSolver = false;
			
			solver.RemoveActor(actorID);

			if (OnRemovedFromSolver != null)
				OnRemovedFromSolver(this,null);

			return true;
		}
		
		return false;
		
	}

	/**
	 * Sends local particle data to the solver.
	 */
	public virtual void PushDataToSolver(ObiSolverData data){

		if (!InSolver) return;

		for (int i = 0; i < particleIndices.Count; i++){
			int k = particleIndices[i];

			if ((data.particleData & ObiSolverData.ParticleData.ACTIVE_STATUS) != 0){
				if (!active[i])
					solver.activeParticles.Remove(k);
				else
					solver.activeParticles.Add(k);
			}

			if ((data.particleData & ObiSolverData.ParticleData.POSITIONS) != 0 && i < positions.Length)
				Oni.SetParticlePositions(solver.OniSolver,new Vector4[]{transform.TransformPoint(positions[i])},1,k);
			if ((data.particleData & ObiSolverData.ParticleData.VELOCITIES) != 0 && i < velocities.Length)
				Oni.SetParticleVelocities(solver.OniSolver,new Vector4[]{transform.TransformVector(velocities[i])},1,k);
			if ((data.particleData & ObiSolverData.ParticleData.VORTICITIES) != 0 && i < vorticities.Length)
				Oni.SetParticleVorticities(solver.OniSolver,new Vector4[]{transform.TransformVector(vorticities[i])},1,k);
			if ((data.particleData & ObiSolverData.ParticleData.INV_MASSES) != 0 && i < invMasses.Length)
				Oni.SetParticleInverseMasses(solver.OniSolver,new float[]{invMasses[i]},1,k);
			if ((data.particleData & ObiSolverData.ParticleData.SOLID_RADII) != 0 && i < solidRadii.Length)
				Oni.SetParticleSolidRadii(solver.OniSolver,new float[]{solidRadii[i]},1,k);
			if ((data.particleData & ObiSolverData.ParticleData.PHASES) != 0 && i < phases.Length)
				Oni.SetParticlePhases(solver.OniSolver,new int[]{phases[i]},1,k);
		}
        
        if ((data.particleData & ObiSolverData.ParticleData.ACTIVE_STATUS) != 0)
			solver.UpdateActiveParticles();

	}

	/**
	 * Retrieves particle simulation data from the solver. Common uses are
	 * retrieving positions and velocities to set the initial status of the simulation,
 	 * or retrieving solver-generated data such as tensions, densities, etc.
	 */
	public virtual void PullDataFromSolver(ObiSolverData data){
		
		if (!InSolver) return;

		for (int i = 0; i < particleIndices.Count; i++){
			int k = particleIndices[i];
			if ((data.particleData & ObiSolverData.ParticleData.POSITIONS) != 0){
				Vector4[] wsPosition = {positions[i]};
				Oni.GetParticlePositions(solver.OniSolver,wsPosition,1,k);
				positions[i] = transform.InverseTransformPoint(wsPosition[0]);
			}
			if ((data.particleData & ObiSolverData.ParticleData.VELOCITIES) != 0){
				Vector4[] wsVelocity = {velocities[i]};
				Oni.GetParticleVelocities(solver.OniSolver,wsVelocity,1,k);
				velocities[i] = transform.InverseTransformVector(wsVelocity[0]);
			}
		}
		
	}

	/**
	 * Returns the position of a particle in world space. 
	 * Works both when the actor is managed by a solver and when it isn't. 
	 */
	public Vector3 GetParticlePosition(int index){
		if (InSolver)
			return solver.renderablePositions[particleIndices[index]];
		else
			return transform.TransformPoint(positions[index]);
	}

	/**
	 * Returns particle reference orientation, if it can be derived. Reimplemented by subclasses. Returns
	 * Quaternion.identity by default.
	 */
	public virtual Quaternion GetParticleOrientation(int index){
		return Quaternion.identity;
	}

	public virtual bool GenerateTethers(int maxTethers){
		return true;
	}

	public virtual void OnSolverPreInterpolation(){
	}

	/**
	 * Transforms the position of fixed particles from local space to world space and feeds them
	 * to the solver. This is performed just before performing simulation each frame.
	 */
	public virtual void OnSolverStepBegin(){
		for(int i = 0; i < particleIndices.Count; i++){
			if (!enabled || invMasses[i] == 0){
				Vector4[] worldPosition = {transform.TransformPoint(positions[i])};
				Oni.SetParticlePositions(solver.OniSolver,worldPosition,1,particleIndices[i]);
			}
		}
	}

	public virtual void OnSolverStepEnd(){
	}

	public virtual void OnSolverFrameBegin(){
	}

	public virtual void OnSolverFrameEnd(){	

		// If the object has changed layers, update solver particle phases.
		if (gameObject.layer != oldLayer || selfCollisions != oldSelfCollisions){
			UpdateParticlePhases();
			oldSelfCollisions = selfCollisions;
			oldLayer = gameObject.layer;
		}	

    }
}
}

