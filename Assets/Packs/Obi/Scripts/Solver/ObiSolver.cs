/**
\mainpage ObiCloth documentation
 
Introduction:
------------- 

ObiCloth is a position-based dynamics solver for cloth. It is meant to bring back and extend upon Unity's 4.x
cloth, which had two-way rigidbody coupling. 
 
Features:
-------------------

- Cloth particles can be pinned both in local space and to rigidbodies (kinematic or not).
- Cloth can be teared.
- Realistic wind forces.
- Rigidbodies react to cloth dynamics, and cloth reacts to rigidbodies too.
- Easy prefab instantiation, cloth can be translated, scaled and rotated.
- Simulation can be warm-started in the editor, then all simulation state gets serialized with the object. This means
  your cloth prefabs can be stored at any point in the simulation, and they will resume it when instantiated.

*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

namespace Obi
{

/**
 * An ObiSolver component simulates particles and their interactions using the Oni unified physics library.
 * Several kinds of constraint types and their parameters are exposed, and several Obi components can
 * be used to feed particles and constraints to the solver.
 */
[ExecuteInEditMode]
[AddComponentMenu("Physics/Obi/Obi Solver")]
[DisallowMultipleComponent]
public sealed class ObiSolver : MonoBehaviour
{

	public enum ClothInterpolation{
		NONE,
		INTERPOLATE
	}

	public class ObiCollisionEventArgs : EventArgs{

		public int[] indices;			/**< collision indices. Even positions in the array are particle indices, odd positions are collider indices.*/
		public float[] distances;
		public Vector4[] points;
		public Vector4[] normals;

		public float[] normalImpulses;
		public float[] tangentImpulses;
		public float[] stickImpulses;

		public ObiCollisionEventArgs(int[] indices, 
									 float[] distances, 
									 Vector4[] points, 
									 Vector4[] normals,
									 float[] normalImpulses,
									 float[] tangentImpulses,
								     float[] stickImpulses){
			this.indices = indices;
			this.distances = distances;
			this.points = points;
			this.normals = normals;

			this.normalImpulses = normalImpulses;
			this.tangentImpulses = tangentImpulses;
			this.stickImpulses = stickImpulses;
		}
	}

	public class ObiFluidEventArgs : EventArgs{

		public int[] indices;			/**< fluid particle indices.*/
		public Vector4[] vorticities;
		public Vector4[] normals;
		public float[] densities;

		public ObiFluidEventArgs(int[] indices, 
								 Vector4[] vorticities,
								 Vector4[] normals,
								 float[] densities){
			this.indices = indices;
			this.vorticities = vorticities;
			this.normals = normals;
			this.densities = densities;
		}
	}

	public event EventHandler OnFrameBegin;
	public event EventHandler OnStepBegin;
	public event EventHandler OnFixedParticlesUpdated;
	public event EventHandler OnStepEnd;
	public event EventHandler OnBeforePositionInterpolation;
	public event EventHandler OnBeforeActorsFrameEnd;
	public event EventHandler OnFrameEnd;
	public event EventHandler<ObiCollisionEventArgs> OnCollision;
	public event EventHandler<ObiFluidEventArgs> OnFluidUpdated;
	
	public int maxParticles = 5000;
	public int maxDiffuseParticles = 5000;

	[HideInInspector] [NonSerialized] public bool simulate = true;

	[Tooltip("If enabled, will force the solver to keep simulating even when not visible from any camera.")]
	public bool simulateWhenInvisible = true; 			/**< Whether to keep simulating the cloth when its not visible by any camera.*/
	public ObiColliderGroup colliderGroup;
	public Oni.SolverParameters parameters = new Oni.SolverParameters(Oni.SolverParameters.Interpolation.None,
	                                                                  new Vector4(0,-9.81f,0,0));

	[HideInInspector] [NonSerialized] public List<ObiActor> actors = new List<ObiActor>();
	[HideInInspector] [NonSerialized] public HashSet<int> allocatedParticles;
	[HideInInspector] [NonSerialized] public HashSet<int> activeParticles;

	[HideInInspector] [NonSerialized] public int[] materialIndices;
	[HideInInspector] [NonSerialized] public int[] fluidMaterialIndices;

	[HideInInspector] [NonSerialized] public Vector4[] renderablePositions;	/**< renderable particle positions.*/

	// constraint groups:
	[HideInInspector] public int[] constraintsOrder;
	
	// constraint parameters:
	public Oni.ConstraintParameters distanceConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Sequential,3);
	public Oni.ConstraintParameters bendingConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters particleCollisionConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters collisionConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters skinConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Sequential,3);
	public Oni.ConstraintParameters volumeConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters tetherConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters pinConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters densityConstraintParameters = new Oni.ConstraintParameters(false,Oni.ConstraintParameters.EvaluationOrder.Parallel,2);
	public Oni.ConstraintParameters chainConstraintParameters = new Oni.ConstraintParameters(false,Oni.ConstraintParameters.EvaluationOrder.Parallel,10);

	private IntPtr oniSolver;
	private ObiCollisionMaterial defaultMaterial;
	private ObiEmitterMaterial defaultFluidMaterial;
	private UnityEngine.Bounds bounds = new UnityEngine.Bounds();
 
 	private bool initialized;
	private bool isVisible = true;
 
	public struct BodyInformation{
		public float mass;
		public Vector3 centerOfMass;
		public Vector3 centerOfMassVelocity;
	}

	public IntPtr OniSolver
	{
		get{return oniSolver;}
	}

	public UnityEngine.Bounds Bounds
	{
		get{return bounds;}
	}

	public bool IsVisible
	{
		get{return isVisible;}
	}

	public bool IsUpdating{
		get{return (simulate && (simulateWhenInvisible || IsVisible));}
	}

	void Start(){
		if (colliderGroup != null)
			Oni.SetColliderGroup(oniSolver,colliderGroup.oniColliderGroup);
	}

	void Awake(){
		if (Application.isPlaying) //only during game.
			Initialize();
	}

	void OnDestroy(){
		if (Application.isPlaying) //only during game.
			Teardown();
	}

	void OnEnable(){

		constraintsOrder = new int[]{0,1,2,3,4,5,6,7,8,9,10};

		if (!Application.isPlaying) //only in editor.
			Initialize();
		StartCoroutine("RunLateFixedUpdate");
	}
	
	void OnDisable(){
		if (!Application.isPlaying) //only in editor.
			Teardown();
		StopCoroutine("RunLateFixedUpdate");
	}
	
	public void Initialize(){

		// Tear everything down first:
		Teardown();
			
		try{

			// Create a default material (TODO: maybe expose this to the user?)
			defaultMaterial = ScriptableObject.CreateInstance<ObiCollisionMaterial>();
			defaultMaterial.hideFlags = HideFlags.HideAndDontSave;

			defaultFluidMaterial = ScriptableObject.CreateInstance<ObiEmitterMaterial>();
			defaultFluidMaterial.hideFlags = HideFlags.HideAndDontSave;
	
			// Create the Oni solver:
			oniSolver = Oni.CreateSolver(maxParticles,maxDiffuseParticles,92);
			
			actors = new List<ObiActor>();
			allocatedParticles = new HashSet<int>();
			activeParticles = new HashSet<int>();
			materialIndices = new int[maxParticles];
			fluidMaterialIndices = new int[maxParticles];
			renderablePositions = new Vector4[maxParticles];
			
			// Initialize materials:
			UpdateSolverMaterials();
			UpdateFluidMaterials();
			
			// Initialize parameters:
			UpdateParameters();
			
		}catch (Exception exception){
			Debug.LogException(exception);
		}finally{
			initialized = true;
		};

	}

	private void Teardown(){
	
		if (!initialized) return;
		
		try{

			while (actors.Count > 0){
				actors[actors.Count-1].RemoveFromSolver(null);
			}
				
			Oni.DestroySolver(oniSolver);
			
			GameObject.DestroyImmediate(defaultMaterial);
			GameObject.DestroyImmediate(defaultFluidMaterial);
		
		}catch (Exception exception){
			Debug.LogException(exception);
		}finally{
			initialized = false;
		}
	}

	/**
	 * Adds a new transform to the solver and returns its ID.
	 */
	public int SetActor(int ID, ObiActor actor)
	{

		// Add the transform, as its new.
		if (ID < 0 || ID >= actors.Count){
	
			int index = actors.Count;

            // Use the free slot to insert the transform:
			actors.Add(actor);

			// Update materials, in case the actor has a new one.
			UpdateSolverMaterials();
			UpdateFluidMaterials();

			// Return the transform index as its ID
			return index;

		}
		// The transform is already there.
		else{

			actors[ID] = actor;
			UpdateSolverMaterials();
			UpdateFluidMaterials();
			return ID;

		}

	}

	/**
 	 * Removes an actor from the solver and returns its ID.
	 */
	public void RemoveActor(int ID){
		
		if (ID < 0 || ID >= actors.Count) return;

		// Update actor ID for affected actors:
		for (int i = ID+1; i < actors.Count; i++){
			actors[i].actorID--;
		}

		actors.RemoveAt(ID); 

		// Update materials, in case the actor had one.
		UpdateSolverMaterials();
		UpdateFluidMaterials();
	}

	/**
	 * Reserves a certain amount of particles and returns their indices in the 
	 * solver arrays.
	 */
	public List<int> AllocateParticles(int numParticles){

		if (allocatedParticles == null)
			return null;

		List<int> allocated = new List<int>();
		for (int i = 0; i < maxParticles && allocated.Count < numParticles; i++){
			if (!allocatedParticles.Contains(i)){
				allocated.Add(i);
			}
		}

		// could not allocate enough particles.
		if (allocated.Count < numParticles){
			return null; 
		}
   
        // allocation was successful:
		allocatedParticles.UnionWith(allocated);
		activeParticles.UnionWith(allocated);
		UpdateActiveParticles();          
		return allocated;

	}

	/**
	 * Frees a list of particles.
	 */
	public void FreeParticles(List<int> indices){
		
		if (allocatedParticles == null || indices == null)
			return;
		
		allocatedParticles.ExceptWith(indices);
		activeParticles.ExceptWith(indices);

		UpdateActiveParticles(); 
		
	}

	/**
	 * Updates solver parameters, sending them to the Oni library.
	 */
	public void UpdateParameters(){

		Oni.SetSolverParameters(oniSolver,ref parameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Distance,ref distanceConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Bending,ref bendingConstraintParameters);
	
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.ParticleCollision,ref particleCollisionConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Collision,ref collisionConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Density,ref densityConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Skin,ref skinConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Volume,ref volumeConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Tether,ref tetherConstraintParameters);
	
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Pin,ref pinConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Chain,ref chainConstraintParameters);

		Oni.SetConstraintsOrder(oniSolver,constraintsOrder);
    }

	/**
	 * Updates the active particles array.
	 */
	public void UpdateActiveParticles(){

		// Get allocated particles and remove the inactive ones:
		int[] activeArray = new int[activeParticles.Count];
		activeParticles.CopyTo(activeArray);
		Oni.SetActiveParticles(oniSolver,activeArray,activeArray.Length);

	}

	public void UpdateFluidMaterials(){

		/*HashSet<ObiEmitterMaterial> materialsSet = new HashSet<ObiEmitterMaterial>();
		List<ObiEmitterMaterial> materials = new List<ObiEmitterMaterial>();

		// The default material must always be present.
		materialsSet.Add (defaultFluidMaterial);		
		materials.Add(defaultFluidMaterial);

		// Setup all materials used by particle actors:
		foreach (ObiActor actor in actors){
			
			ObiEmitter em = actor as ObiEmitter;
				if (em == null) continue;

			int materialIndex = 0;

			if (!materialsSet.Contains(em.emitterMaterial)){
				materialIndex = materials.Count;
				materials.Add(em.emitterMaterial);
				materialsSet.Add(em.emitterMaterial);
			}else{
				materialIndex = materials.IndexOf(em.emitterMaterial);
			}
			
			// Update material index for all actor particles:
			for(int i = 0; i < actor.particleIndices.Count; i++){
				fluidMaterialIndices[actor.particleIndices[i]] = materialIndex;
			}
		}

		Oni.SetFluidMaterialIndices(oniSolver,fluidMaterialIndices,fluidMaterialIndices.Length,0);
		Oni.FluidMaterial[] mArray = materials.ConvertAll<Oni.FluidMaterial>(a => a.GetEquivalentOniMaterial()).ToArray();
		Oni.SetFluidMaterials(oniSolver,mArray,mArray.Length,0);*/
	}

	public void UpdateSolverMaterials(){

		HashSet<ObiCollisionMaterial> materialsSet = new HashSet<ObiCollisionMaterial>();
		List<ObiCollisionMaterial> materials = new List<ObiCollisionMaterial>();

		// The default material must always be present.
		materialsSet.Add (defaultMaterial);		
		materials.Add(defaultMaterial);

		// Setup all materials used by particle actors:
		foreach (ObiActor actor in actors){
			
			int materialIndex = 0;

			if (actor.material != null){
				if (!materialsSet.Contains(actor.material)){
					materialIndex = materials.Count;
					materials.Add(actor.material);
					materialsSet.Add(actor.material);
				}else{
					materialIndex = materials.IndexOf(actor.material);
				}
			}

			// Update material index for all actor particles:
			for(int i = 0; i < actor.particleIndices.Count; i++){
				materialIndices[actor.particleIndices[i]] = materialIndex;
			}
		}

		// Setup all materials used by colliders:
		if (colliderGroup != null){
			foreach (Collider c in colliderGroup.colliders){
			
				if (c == null) continue;

				ObiCollider oc = c.GetComponent<ObiCollider>();
	
				if (oc == null) continue;
					
				oc.materialIndex = 0;
				
				if (oc.material == null) continue;
	
				if (!materialsSet.Contains(oc.material)){
					oc.materialIndex = materials.Count;
					materials.Add(oc.material);
					materialsSet.Add(oc.material);
				}else{
					oc.materialIndex = materials.IndexOf(oc.material);
				}
			}
		}

		Oni.SetMaterialIndices(oniSolver,materialIndices,materialIndices.Length,0);
		Oni.CollisionMaterial[] mArray = materials.ConvertAll<Oni.CollisionMaterial>(a => a.GetEquivalentOniMaterial()).ToArray();
		Oni.SetCollisionMaterials(oniSolver,mArray,mArray.Length,0);
	}

	public void AccumulateSimulationTime(float dt){

		Oni.AddSimulationTime(oniSolver,dt);

	}

	public void SimulateStep(float stepTime){

		foreach(ObiActor actor in actors)
            actor.OnSolverStepBegin();

		// Trigger event right after actors have fixed their particles in OnSolverStepBegin.
		if (OnFixedParticlesUpdated != null)
			OnFixedParticlesUpdated(this,null);

		// Update all collider and rigidbody information, so that the solver works with up-to-date stuff:
		if (colliderGroup != null)
			colliderGroup.UpdateBodiesInfo();

		// Update the solver:
		Oni.UpdateSolver(oniSolver, stepTime);

		// Apply modified rigidbody velocities and torques back:
		if (colliderGroup != null)
			colliderGroup.UpdateVelocities();

		// Trigger solver events:
		TriggerCollisionEvents();
		
		foreach(ObiActor actor in actors)
            actor.OnSolverStepEnd();

	} 

	public void EndFrame(float frameDelta){

		foreach(ObiActor actor in actors)
            actor.OnSolverPreInterpolation();

		if (OnBeforePositionInterpolation != null)
			OnBeforePositionInterpolation(this,null);

		Oni.ApplyPositionInterpolation(oniSolver, frameDelta);

		Oni.GetRenderableParticlePositions(oniSolver, renderablePositions, renderablePositions.Length,0);

		// Trigger fluid update:
		TriggerFluidUpdateEvents();

		CheckVisibility();

		if (OnBeforeActorsFrameEnd != null)
			OnBeforeActorsFrameEnd(this,null);
		
		foreach(ObiActor actor in actors)
            actor.OnSolverFrameEnd();

	}

	private void TriggerFluidUpdateEvents(){

		int numFluidParticles = Oni.GetConstraintCount(oniSolver,(int)Oni.ConstraintType.Density);
		
		if (numFluidParticles > 0 && OnFluidUpdated != null){

			int[] indices = new int[numFluidParticles];
			Vector4[] vorticities = new Vector4[maxParticles];
			Vector4[] normals = new Vector4[maxParticles];
			float[] densities = new float[maxParticles];

			Oni.GetActiveConstraintIndices(oniSolver,indices,numFluidParticles,(int)Oni.ConstraintType.Density);
			Oni.GetParticleVorticities(oniSolver,vorticities,maxParticles,0);
			Oni.GetParticleNormals(oniSolver,normals,maxParticles,0);
			Oni.GetParticleDensities(oniSolver,densities,maxParticles,0);

			OnFluidUpdated(this,new ObiFluidEventArgs(indices,vorticities,normals,densities));
		}
	}

	private void TriggerCollisionEvents(){
	
		int numCollisions = Oni.GetConstraintCount(oniSolver,(int)Oni.ConstraintType.Collision);

		if (OnCollision != null){

			int[] indices = new int[numCollisions*2];
			float[] distances = new float[numCollisions];
			Vector4[] points = new Vector4[numCollisions];
			Vector4[] normals = new Vector4[numCollisions];
			float[] normalImpulses = new float[numCollisions];
			float[] tangentImpulses = new float[numCollisions];
			float[] stickImpulses = new float[numCollisions];

			if (numCollisions > 0)
			{
				Oni.GetCollisionIndices(oniSolver,indices,numCollisions);
				Oni.GetCollisionDistances(oniSolver,distances,numCollisions);
				Oni.GetCollisionPoints(oniSolver,points,numCollisions);
				Oni.GetCollisionNormals(oniSolver,normals,numCollisions);
				Oni.GetCollisionNormalImpulses(oniSolver,normalImpulses,numCollisions);
				Oni.GetCollisionTangentImpulses(oniSolver,tangentImpulses,numCollisions);
				Oni.GetCollisionStickImpulses(oniSolver,stickImpulses,numCollisions);
			}
	
			OnCollision(this,new ObiCollisionEventArgs(indices,distances,points,normals,normalImpulses,tangentImpulses,stickImpulses));

		}
	}

	/**
	 * Checks if any particle in the solver is visible from at least one camera. If so, sets isVisible to true, false otherwise.
	 */
	private void CheckVisibility(){

		Vector3 min = Vector3.zero, max = Vector3.zero;
		Oni.GetBounds(oniSolver,ref min, ref max);
		bounds.SetMinMax(min,max);

		isVisible = false;

		if (!float.IsNaN(bounds.center.x) && 
			!float.IsNaN(bounds.center.y) && 
			!float.IsNaN(bounds.center.z)){

			foreach (Camera cam in Camera.allCameras){
	        	Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
	       		if (GeometryUtility.TestPlanesAABB(planes, bounds)){
					isVisible = true;
					return;
				}
			}
		}
	}
    
    void Update(){

		if (OnFrameBegin != null)
			OnFrameBegin(this,null);

		foreach(ObiActor actor in actors)
            actor.OnSolverFrameBegin();

		if (IsUpdating){
			AccumulateSimulationTime(Time.deltaTime);
		}
	}

	IEnumerator RunLateFixedUpdate() {
         while (true) {
             yield return new WaitForFixedUpdate();
             LateFixedUpdate();
         }
     }

     void LateFixedUpdate()
     {
        if (IsUpdating){

			if (OnStepBegin != null)
				OnStepBegin(this,null);

			SimulateStep(Time.fixedDeltaTime);

			if (OnStepEnd != null)
				OnStepEnd(this,null);
		}
     }

	private void LateUpdate(){
   
		EndFrame (Time.fixedDeltaTime);

		if (OnFrameEnd != null)
			OnFrameEnd(this,null);
	}

}

}
