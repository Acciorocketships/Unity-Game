using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
	
	/**
	 * Rope made of Obi particles. No mesh or topology is needed to generate a physic representation from,
	 * since the mesh is generated procedurally.
	 */
	[ExecuteInEditMode]
	[AddComponentMenu("Physics/Obi/Obi Rope")]
	[RequireComponent(typeof (MeshRenderer))]
	[RequireComponent(typeof (MeshFilter))]
	[RequireComponent(typeof (ObiDistanceConstraints))]
	[RequireComponent(typeof (ObiBendingConstraints))]
	[RequireComponent(typeof (ObiTetherConstraints))]
	[RequireComponent(typeof (ObiPinConstraints))]
	[RequireComponent(typeof (ObiChainConstraints))]
	public class ObiRope : ObiActor
	{
		/**
		 * How to render the rope.
		 */
		public enum RenderingMode
		{
			ProceduralRope,
			Chain
		}


		[Tooltip("Amount of additional particles in this rope's pool that can be used to extend its lenght.")]
		public int pooledParticles = 200;

		[Tooltip("Path used to generate the rope.")]
		public ObiCurve ropePath = null;

		[HideInInspector][SerializeField] private  ObiRopeSection section = null;		/**< Section asset to be extruded along the rope.*/

		[HideInInspector][SerializeField] private  int capSections = 2;

		[HideInInspector][SerializeField] private  float sectionTwist = 0;				/**< Amount of twist applied to each section, in degrees.*/

		[HideInInspector][SerializeField] private  float thickness = 0.05f;				/**< Thickness of the rope.*/

		[HideInInspector][SerializeField] private Vector2 uvScale = Vector3.one;

		[HideInInspector][SerializeField] private bool normalizeV = true;

		[Tooltip("Modulates the amount of particles per lenght unit. 1 means as many particles as needed for the given length/thickness will be used, which"+
				 "can be a lot in very thin and long ropes. Setting values between 0 and 1 allows you to override the amount of particles used.")]
		[Range(0,1)]
		public float resolution = 1;													/**< modulates resolution of particle representation.*/

		[HideInInspector][SerializeField] private RenderingMode renderMode = RenderingMode.ProceduralRope;

		public List<GameObject> chainLinks = new List<GameObject>();

		[HideInInspector][SerializeField] private Vector3 linkScale = Vector3.one;				/**< Scale of chain links..*/

		[HideInInspector][SerializeField] private bool randomizeLinks = false;

		[HideInInspector] public Mesh ropeMesh;
		[HideInInspector][SerializeField] private List<GameObject> linkInstances;

		[HideInInspector][SerializeField] private bool closed = false;
		[HideInInspector][SerializeField] private float particleDistance = -1;
		[HideInInspector][SerializeField] private float restLength = -1;
		[HideInInspector][SerializeField] private int numParticles = -1;
		[HideInInspector][SerializeField] private float defaultParticleMass = 1;
		private MeshRenderer meshRenderer;
		private MeshFilter meshFilter;

		private ObiDistanceConstraints distanceConstraints;
		private ObiBendingConstraints bendingConstraints;
		private ObiTetherConstraints tetherConstraints;
		private ObiPinConstraints pinConstraints;
		private ObiChainConstraints chainConstraints;

		[HideInInspector] public float[] mass;						/**< Per particle mass.*/

		public ObiDistanceConstraints DistanceConstraints{
			get{return distanceConstraints;}
		}
		public ObiBendingConstraints BendingConstraints{
			get{return bendingConstraints;}
		}
		public ObiTetherConstraints TetherConstraints{
			get{return tetherConstraints;}
		}
		public ObiPinConstraints PinConstraints{
			get{return pinConstraints;}
		}
		public ObiChainConstraints RopeConstraints{
			get{return chainConstraints;}
		}

		public override int MaxTethers{ 
			get{return 2;}
		}

		public RenderingMode RenderMode{
			set{
				if (value != renderMode){
					renderMode = value;

					ClearChainLinkInstances();	
					GameObject.DestroyImmediate(ropeMesh);

					GenerateVisualRepresentation();
				}	
			}
			get{return renderMode;}
		} 

		public ObiRopeSection Section{
			set{
				if (value != section){
					section = value;
					GenerateProceduralRopeMesh();
				}	
			}
			get{return section;}
		} 

		public float Thickness{
			set{
				if (value != thickness){
					thickness = Mathf.Max(0,value);
					UpdateProceduralRopeMesh();
				}	
			}
			get{return thickness;}
		} 

		public float SectionTwist{
			set{
				if (value != sectionTwist){
					sectionTwist = value;
					UpdateVisualRepresentation();
				}	
			}
			get{return sectionTwist;}
		}

		public int CapSections{
			set{
				if (value != capSections){
					capSections = Mathf.Max(0,value);
					GenerateProceduralRopeMesh();
				}	
			}
			get{return capSections;}
		}

		public Vector3 LinkScale{
			set{
				if (value != linkScale){
					linkScale = value;
					UpdateProceduralChainLinks();
				}	
			}
			get{return linkScale;}
		}

		public Vector2 UVScale{
			set{
				if (value != uvScale){
					uvScale = value;
					UpdateProceduralRopeMesh();
				}	
			}
			get{return uvScale;}
		}

		public bool NormalizeV{
			set{
				if (value != normalizeV){
					normalizeV = value;
					UpdateProceduralRopeMesh();
				}	
			}
			get{return normalizeV;}
		}

		public bool RandomizeLinks{
			set{
				if (value != randomizeLinks){
					randomizeLinks = value;
					GenerateProceduralChainLinks();
				}	
			}
			get{return randomizeLinks;}
		}

		public override void Awake()
		{
			base.Awake();

			// Create a new chain liks list. When duplicating a chain, we don't want to
			// use references to the original chain's links!
			linkInstances = new List<GameObject>();

			distanceConstraints = GetComponent<ObiDistanceConstraints>();
			chainConstraints = GetComponent<ObiChainConstraints>();
			bendingConstraints = GetComponent<ObiBendingConstraints>();
			tetherConstraints = GetComponent<ObiTetherConstraints>();
			pinConstraints = GetComponent<ObiPinConstraints>();
			
			meshRenderer = GetComponent<MeshRenderer>();
			meshFilter = GetComponent<MeshFilter>();
		}
	     
		public void OnValidate(){
			thickness = Mathf.Max(0.0001f,thickness);
			capSections = Mathf.Max(0,capSections);
	    }

		public override void OnEnable(){
			
			base.OnEnable();

			GenerateVisualRepresentation();

			// Enable constraints affecting this rope:
			distanceConstraints.OnEnable();
			chainConstraints.OnEnable();
			bendingConstraints.OnEnable();
			tetherConstraints.OnEnable();
			pinConstraints.OnEnable();

		}
		
		public override void OnDisable(){
			
			base.OnDisable();

			GameObject.DestroyImmediate(ropeMesh);
			ClearChainLinkInstances();

			// Disable constraints affecting this cloth:
			distanceConstraints.OnDisable();
			chainConstraints.OnDisable();
			bendingConstraints.OnDisable();
			tetherConstraints.OnDisable();
			pinConstraints.OnDisable();
			
		}

		public override void OnSolverFrameEnd(){
			
			base.OnSolverFrameEnd();

			UpdateVisualRepresentation();
			
		}
		
		public override void OnDestroy(){
			base.OnDestroy();
		}

		public override void DestroyRequiredComponents(){
			#if UNITY_EDITOR
				GameObject.DestroyImmediate(meshRenderer);
				GameObject.DestroyImmediate(meshFilter);
				GameObject.DestroyImmediate(distanceConstraints);
				GameObject.DestroyImmediate(chainConstraints);
				GameObject.DestroyImmediate(bendingConstraints);
				GameObject.DestroyImmediate(tetherConstraints);
				GameObject.DestroyImmediate(pinConstraints);
			#endif
		}
		
		public override bool AddToSolver(object info){
			
			if (Initialized && base.AddToSolver(info)){
				distanceConstraints.AddToSolver(this);
				chainConstraints.AddToSolver(this);
				bendingConstraints.AddToSolver(this);
				tetherConstraints.AddToSolver(this);
				pinConstraints.AddToSolver(this);
				return true;
			}
			return false;
		}
		
		public override bool RemoveFromSolver(object info){
			
			bool removed = false;

			try{
				if (distanceConstraints != null)
					distanceConstraints.RemoveFromSolver(null);
				if (chainConstraints != null)
					chainConstraints.RemoveFromSolver(null);
				if (bendingConstraints != null)
					bendingConstraints.RemoveFromSolver(null);
				if (tetherConstraints != null)
					tetherConstraints.RemoveFromSolver(null);
				if (pinConstraints != null)
					pinConstraints.RemoveFromSolver(null);
			}catch(Exception e){
				Debug.LogException(e);
			}finally{
				removed = base.RemoveFromSolver(info);
			}
			return removed;
		}

		
		/**
	 	* Generates the particle based physical representation of the rope. This is the initialization method for the rope object
		* and should not be called directly once the object has been created.
	 	*/
		public IEnumerator GeneratePhysicRepresentationForMesh()
		{		
			initialized = false;			
			initializing = true;	
			particleDistance = -1;

			RemoveFromSolver(null);

			ropePath.RecalculateSplineLenght(0.00001f,7);
			closed = ropePath.Closed;
			restLength = ropePath.Length;

			numParticles = Mathf.CeilToInt(restLength/thickness * resolution) + (closed ? 0:1);
			int totalParticles = numParticles + pooledParticles; //allocate extra particles to allow for lenght change.

			active = new bool[totalParticles];
			positions = new Vector3[totalParticles];
			velocities = new Vector3[totalParticles];
			vorticities = new Vector3[totalParticles];
			invMasses  = new float[totalParticles];
			solidRadii = new float[totalParticles];
			phases = new int[totalParticles];
			mass = new float[totalParticles];
			ignoredCollisions = new CollisionIgnoreList[totalParticles];
			
			particleDistance = restLength/(float)(numParticles - (closed ? 0:1));
			for (int i = 0; i < numParticles; i++){

				active[i] = true;
				mass[i] = 1;
				invMasses[i] = 1;
				float mu = ropePath.GetMuAtLenght(particleDistance*i);
				positions[i] = transform.InverseTransformPoint(ropePath.transform.TransformPoint(ropePath.GetPositionAt(mu)));
				solidRadii[i] = particleDistance * resolution;
				phases[i] = Oni.MakePhase(gameObject.layer,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
				ignoredCollisions[i] = new CollisionIgnoreList(new int[2]{i-1,i+1});

				if (i % 100 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiRope: generating particles...",i/(float)numParticles);

			}

			// Initialize basic data for pooled particles:
			for (int i = numParticles; i < pooledParticles; i++){

				active[i] = false;
				mass[i] = defaultParticleMass;
				invMasses[i] = 1/mass[i];
				solidRadii[i] = particleDistance * resolution;
				phases[i] = Oni.MakePhase(gameObject.layer,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
				ignoredCollisions[i] = new CollisionIgnoreList(new int[2]{i-1,i+1});

				if (i % 100 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiRope: generating particles...",i/(float)numParticles);

			}

			int numConstraints = numParticles-(closed ? 0:1);
			distanceConstraints.Initialize();
			for (int i = 0; i < numConstraints; i++){

				distanceConstraints.AddConstraint(true,i,(i+1) % (ropePath.Closed ? numParticles:numParticles+1),particleDistance,1,1);
			
				if (i % 500 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiRope: generating structural constraints...",i/(float)numConstraints);

			}

			chainConstraints.Initialize();
			int[] indices = new int[numParticles + (closed ? 1:0)];

			for (int i = 0; i < numParticles; ++i)
				indices[i] = i;

			// Add the first particle as the last index of the chain, if closed.
			if (closed)
				indices[numParticles] = 0;
			
			chainConstraints.AddConstraint(true,indices,particleDistance,1,1);

			bendingConstraints.Initialize();
			for (int i = 0; i < numParticles; i++){

				// skip first and last particles if the rope is not closed.
				if (!closed && (i == 0 || i == numParticles-1)) continue;

				int prev = closed ? ((i-1 < 0) ? numParticles - 1 :i-1 ) : i-1;
				int next = closed ? (i+1) % numParticles : i+1;
	
				// rope bending constraints always try to keep it completely straight:
				bendingConstraints.AddConstraint(true,prev,next,i,0,0,1);
			
				if (i % 500 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiRope: adding bend constraints...",i/(float)numConstraints);

			}
			

			// Initialize tether constraints:
			tetherConstraints.Initialize();
			
			AddToSolver(null);

			initializing = false;
			initialized = true;

			GenerateVisualRepresentation();
		}

		/**
		 * Generates any precomputable data for the current visual representation.
		 */
		public void GenerateVisualRepresentation(){
			if (renderMode == RenderingMode.ProceduralRope)
				GenerateProceduralRopeMesh();
			else
				GenerateProceduralChainLinks();
		}

		/**
		 * Updates the current visual representation.
		 */
		public void UpdateVisualRepresentation(){
			if (renderMode == RenderingMode.ProceduralRope)
				UpdateProceduralRopeMesh();
			else
				UpdateProceduralChainLinks();
		}	

		private void GenerateProceduralRopeMesh(){

			ropeMesh = new Mesh();
			ropeMesh.MarkDynamic();

			UpdateProceduralRopeMesh();

			if (section == null){
				return;
			}

			int triangleIndicesPerSection = section.Segments * 6;
			int verticesPerSection = section.Segments + 1; // the last vertex in each section must be duplicated, due to uv wraparound.
			int numSections = numParticles + (closed ? 0 : capSections * 2 - 1);
			int numTriangleIndices = numSections * triangleIndicesPerSection;

			int[] tris = new int[numTriangleIndices];

			for (int i = 0; i < numSections; ++i){
				for (int j = 0; j < section.Segments; ++j){
					tris[i*triangleIndicesPerSection + j*6] = i*verticesPerSection + j;
					tris[i*triangleIndicesPerSection + j*6+1] = i*verticesPerSection + (j+1);
					tris[i*triangleIndicesPerSection + j*6+2] = (i+1)*verticesPerSection + j;

					tris[i*triangleIndicesPerSection + j*6+3] = i*verticesPerSection + (j+1);
					tris[i*triangleIndicesPerSection + j*6+4] = (i+1)*verticesPerSection + (j+1);
					tris[i*triangleIndicesPerSection + j*6+5] = (i+1)*verticesPerSection + j;
				}
			}

			ropeMesh.triangles = tris;

			meshFilter.mesh = ropeMesh;
		}

		/**
		 * Destroys all chain link instances. Used when the chain must be re-created from scratch, and when the actor is disabled/destroyed.
		 */
		private void ClearChainLinkInstances(){
			for (int i = 0; i < linkInstances.Count; ++i){
				if (linkInstances[i] != null)
					GameObject.DestroyImmediate(linkInstances[i]);
			}
			linkInstances.Clear();
		}
		
		/**
		 * Analogous to what generate GenerateProceduralRopeMesh does, generates the links used in the chain.
		 */
		public void GenerateProceduralChainLinks(){

			ClearChainLinkInstances();
			
			if (chainLinks.Count > 0){

				int numLinks = numParticles-(closed ? 0:1);
				for (int i = 0; i < numLinks; ++i){
	
					int index = randomizeLinks ? UnityEngine.Random.Range(0,chainLinks.Count) : i % chainLinks.Count;
	
					GameObject linkInstance = null;

					if (chainLinks[index] != null){
						linkInstance = GameObject.Instantiate(chainLinks[index]);
						linkInstance.hideFlags = HideFlags.HideAndDontSave;
					}
	
					linkInstances.Add(linkInstance);
				}
	
			}

			UpdateProceduralChainLinks();
		}

		/**
 	 	 * Applies changes in physics model to the rope mesh.
 	 	 */
		public void UpdateProceduralRopeMesh()
		{
			if (ropeMesh == null || section == null) return;

			// Calculate amount of sections and vertices, prepare data arrays:
			int numSections = numParticles + (closed ? 1 : capSections * 2);
			int numVertices = numSections * (section.Segments + 1); // + 1 section because we need to have different uvs at the wraparound.
			Vector3[] vertices = new Vector3[numVertices];
			Vector3[] normals = new Vector3[numVertices];
			Vector4[] tangents = new Vector4[numVertices];
			Vector2[] uvs = new Vector2[numVertices];

			// Initial frame, world-aligned axes. Use parallel transport method to calculate frames:
			Vector3 pTangent = Vector3.forward;
			Vector3 pNormal = Vector3.up;
			Vector3 pBinormal = Vector3.left;

			float vCoord = 0;	// v texture coordinate.
			int index = 0;		// vertex index.
			int i = 0;			
			for (int m = 0; m < numParticles + (closed ? 1:0); ++m,++i){

				if (closed && m == numParticles) 
					i = 0;

				int nextIndex = closed ? (i+1) % numParticles : Mathf.Min(i+1,numParticles-1);
				int prevIndex = closed ? ((i-1 < 0) ? numParticles - 1 :i-1 ) : Mathf.Max(i-1,0);

				// Calculate current tangent as the vector between previous and next particle (taking care of begin and end)
				Vector3 origin = transform.InverseTransformPoint(GetParticlePosition(i));
				Vector3 nextV = transform.InverseTransformPoint(GetParticlePosition(nextIndex)) - origin;
				Vector3 prevV = origin - transform.InverseTransformPoint(GetParticlePosition(prevIndex));
				Vector3 tangent = (nextV + prevV).normalized;

				// Calculate delta rotation from previous frame:
				Quaternion rot = Quaternion.AngleAxis(Vector3.Angle(pTangent,tangent),Vector3.Cross(pTangent,tangent));
				Quaternion twist = Quaternion.AngleAxis(sectionTwist,tangent);
				
				// Rotate previous frame axes to obtain the new ones:
				Vector3 normal = twist*rot*pNormal;
				Vector3 binormal = twist*rot*pBinormal;

				// Save current frame for next particle:
				pTangent = tangent;
				pNormal = normal;
				pBinormal = binormal;

				// Start cap (special case)
				if (!closed && i == 0){
					for (int k = 0; k < capSections; ++k){

						float capThickness = Mathf.Sqrt(1-Mathf.Pow(k/(float)capSections-1,2)) * thickness;

						for (int j = 0; j <= section.Segments; ++j){

							Vector3 sectionVertex = section.vertices[j].x*normal + section.vertices[j].y*binormal;
							Vector3 sectionWidth = -(thickness * tangent) / capSections * (capSections-k);

							vertices[index] = origin + sectionWidth + sectionVertex * capThickness;

							normals[index] = (sectionWidth + sectionVertex * (capThickness + 0.01f)).normalized;
		
							Vector3 texTangent = -Vector3.Cross(normals[index],tangent);
							tangents[index] = new Vector4(texTangent.x,texTangent.y,texTangent.z,1);
		
							uvs[index] = new Vector2((j/(float)section.Segments)*uvScale.x,vCoord);
		
							index++;
						}
						vCoord += uvScale.y * (thickness/restLength) / (float)capSections;
					}
				}

				// Regular section:
				for (int j = 0; j <= section.Segments; ++j){

					vertices[index] = origin + (section.vertices[j].x*normal + section.vertices[j].y*binormal) * thickness;
					normals[index] = (vertices[index] - origin).normalized;

					Vector3 texTangent = -Vector3.Cross(normals[index],tangent);
					tangents[index] = new Vector4(texTangent.x,texTangent.y,texTangent.z,1);

					uvs[index] = new Vector2((j/(float)section.Segments)*uvScale.x,vCoord);

					index++;
				}
				

				// End cap (special case)
				if (!closed && i == numParticles-1){

					for (int k = 0; k < capSections; ++k){

						vCoord += uvScale.y * (thickness/restLength) / (float)capSections;
						float capThickness = Mathf.Sqrt(1-Mathf.Pow((k+1)/(float)capSections,2)) * thickness;

						for (int j = 0; j <= section.Segments; ++j){

							Vector3 sectionVertex = section.vertices[j].x*normal + section.vertices[j].y*binormal;
							Vector3 sectionWidth = (thickness * tangent) / capSections * (k+1);
	
							vertices[index] = origin + sectionWidth + sectionVertex * capThickness;

							normals[index] = (sectionWidth + sectionVertex * (capThickness + 0.01f)).normalized;
		
							Vector3 texTangent = Vector3.Cross(normals[index],tangent);
							tangents[index] = new Vector4(texTangent.x,texTangent.y,texTangent.z,1);
		
							uvs[index] = new Vector2((j/(float)section.Segments)*uvScale.x,vCoord);
		
							index++;
						}
					}
				}else{
					vCoord += uvScale.y / ((normalizeV) ? restLength/particleDistance : 1);
				}
				
			}

			ropeMesh.vertices = vertices;
			ropeMesh.normals = normals;
			ropeMesh.tangents = tangents;
			ropeMesh.uv = uvs;

			ropeMesh.RecalculateBounds();
			
		}


		/**
		 * Updates chain link positions.
		 */
		public void UpdateProceduralChainLinks(){

			if (linkInstances.Count == 0)
				return;

			// Initial frame, world-aligned axes. Use parallel transport method to calculate frames:
			Vector3 pTangent = Vector3.forward;
			Vector3 pNormal = Vector3.up;
			Vector3 pBinormal = Vector3.left;

			for (int i = 0; i < numParticles + (closed ? 0:-1); ++i){

				int nextIndex = closed ? (i+1) % numParticles : Mathf.Min(i+1,numParticles-1);

				Vector3 pos = GetParticlePosition(i);
				Vector3 nextPos = GetParticlePosition(nextIndex);
				Vector3 linkVector = nextPos-pos;

				Vector3 tangent = linkVector.normalized;

				// Calculate delta rotation from previous frame:
				Quaternion rot = Quaternion.AngleAxis(Vector3.Angle(pTangent,tangent),Vector3.Cross(pTangent,tangent));
				Quaternion twist = Quaternion.AngleAxis(sectionTwist,tangent);
				
				// Rotate previous frame axes to obtain the new ones:
				Vector3 normal = twist*rot*pNormal;
				Vector3 binormal = twist*rot*pBinormal;

				// Save current frame for next particle:
				pTangent = tangent;
				pNormal = normal;
				pBinormal = binormal;

				if (linkInstances[i] != null){
					Transform linkTransform = linkInstances[i].transform;
					linkTransform.position = pos + linkVector * 0.5f;
					linkTransform.localScale = linkScale;
					linkTransform.rotation = Quaternion.LookRotation(tangent,normal);
				}

			}				
			
		}
		
		/**
 		* Resets mesh to its original state.
 		*/
		public override void ResetActor(){
	
			PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.POSITIONS | ObiSolverData.ParticleData.VELOCITIES));
			
			if (particleIndices != null){
				for(int i = 0; i < particleIndices.Count; ++i){
					solver.renderablePositions[particleIndices[i]] = positions[i];
				}
			}

			UpdateVisualRepresentation();

		}

		/*public void Update(){
			if (Input.GetKey(KeyCode.Space)){
				ChangeLength(restLength + 1.0f * Time.deltaTime);
			}

			if (Input.GetKey(KeyCode.B)){
				ChangeLength(restLength - 1.0f * Time.deltaTime);
			}
		}

		private void AddParticles(int amount)
		{
			// Make sure we don't add more particles than available:
			amount = Mathf.Min(amount,pooledParticles);

			// Get current solver positions:
			Vector4[] solverPositions = new Vector4[positions.Length];
			Oni.GetParticlePositions(solver.OniSolver,solverPositions,solverPositions.Length,particleIndices[0]);

			// Get rope end vector:
			Vector4 vector = (solverPositions[numParticles-1] - solverPositions[numParticles-2]).normalized;

			// Activate and set position of particles:
			for (int i = 0 ; i < amount; ++i){
				int index = numParticles + i;
				active[index] = true;
				positions[index] = positions[numParticles-1];
				solverPositions[index] = solverPositions[numParticles-1];
			}

			Oni.SetParticlePositions(solver.OniSolver,solverPositions,solverPositions.Length,particleIndices[0]);
			this.PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.ACTIVE_STATUS ));

			// Add distance constraints:
			distanceConstraints.RemoveFromSolver(null);
			for (int i = 0; i < amount; i++){
				int index = numParticles + i;
				distanceConstraints.AddConstraint(true,index-1,index,particleDistance,1,1);
			}
			distanceConstraints.AddToSolver(null);

			// Add bending constraints:
			bendingConstraints.RemoveFromSolver(null);
			for (int i = 0; i < amount; i++){
				int index = numParticles - 1 + i; // start at the last regular particle, not the first pooled one.
				int prev = closed ? ((index-1 < 0) ? numParticles - 1 :i-1 ) : index-1;
				int next = closed ? (index+1) % numParticles : index+1;
				bendingConstraints.AddConstraint(true,prev,next,index,0,0,1);
			}
			bendingConstraints.AddToSolver(null);

			pooledParticles -= amount;
			numParticles += amount;
		}

		private void RemoveParticles(int amount)
		{
			// Make sure we don't remove more particles than available:
			amount = Mathf.Min(amount,numParticles-2); 

			// Deactivate particles
			for (int i = 0 ; i < amount; ++i){
				active[numParticles-1 - i] = false;
			}

			// Remove distance constraints:
			distanceConstraints.RemoveFromSolver(null);
			for (int i = 0; i < amount; i++){
				distanceConstraints.RemoveConstraint(distanceConstraints.ConstraintCount-1 - i);
			}
			distanceConstraints.AddToSolver(null);

			// Add bending constraints:
			bendingConstraints.RemoveFromSolver(null);
			for (int i = 0; i < amount; i++){
				bendingConstraints.RemoveConstraint(bendingConstraints.ConstraintCount-1 - i);
			}
			bendingConstraints.AddToSolver(null);

			pooledParticles += amount;
			numParticles -= amount;
		}*/

		/**
		 * Changes the length of the rope, adding or removing particles from its end as needed (as long as there are enough pooled particles
		 * left). Since particles are added/removed to/from the end only, any existing particle data (masses, editor selection data) will
		 * be preserved for existing particles when adding new ones.
		 */
		/*public void ChangeLength(float length){

			// Clamp length to sane limits:
			length = Mathf.Clamp(length,0,(numParticles+pooledParticles-1) * particleDistance);

			// figure out how many particles we need to add (or remove) and what amount of lenght will remain after that:
			int particleChange = Mathf.CeilToInt(length / particleDistance) + 1 - numParticles;
			float remainingLenght = length % particleDistance;

			if (particleChange > 0){
				AddParticles(particleChange);
			}else if (particleChange < 0){
				RemoveParticles(-particleChange);
			}

			//Add remaining lenght to the last edge:
			for (int i = 0; i < distanceConstraints.ConstraintCount; ++i)
				distanceConstraints.restLengths[i] = particleDistance;
			distanceConstraints.restLengths[distanceConstraints.ConstraintCount-1] = remainingLenght;	
			distanceConstraints.PushDataToSolver(new ObiSolverData(ObiSolverData.DistanceConstraintsData.DISTANCE_REST_LENGHTS));

			//Update last particle mass according to remaining distance, to change rope mass in a continous way:
			for (int i = 0; i < numParticles; ++i){
				mass[i] = defaultParticleMass;
				invMasses[i] = 1/mass[i];
			}
			mass[numParticles-1] = Mathf.Lerp(0.001f,defaultParticleMass,remainingLenght/particleDistance);
			invMasses[numParticles-1] = 1/mass[numParticles-1];
			PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.INV_MASSES));

			// update rest length:
			restLength = length;

			// Trigger mesh re-generation:
			GenerateVisualRepresentation();
		}*/

		/**
		 * Automatically generates tether constraints for the cloth.
		 * Partitions fixed particles into "islands", then generates up to maxTethers constraints for each 
		 * particle, linking it to the closest point in each island.
		 */
		public override bool GenerateTethers(int maxTethers){
			
			if (!Initialized) return false;
			if (tetherConstraints == null) return false;
	
			tetherConstraints.Initialize();
			
			if (maxTethers > 0){
				
				List<HashSet<int>> islands = new List<HashSet<int>>();
				
				// Partition fixed particles into islands:
				for (int i = 0; i < numParticles; i++){
					
					if (invMasses[i] > 0) continue;
					
					bool inExistingIsland = false;
						
					// If any of the adjacent particles is in an island, this one is in the same island.
					int prev = Mathf.Max(i-1,0);
					int next = Mathf.Min(i+1,numParticles-1);

					foreach(HashSet<int> island in islands){
                    	if (island.Contains(prev) || island.Contains(next)){
							inExistingIsland = true;
                            island.Add(i);
                    		break;
                    	}	
                    }
					
					// If no adjacent particle is in an island, create a new one:
					if (!inExistingIsland){
						islands.Add(new HashSet<int>(){i});
					}
				}	
				
				// Generate tether constraints:
				for (int i = 0; i < numParticles; ++i){
				
					if (invMasses[i] == 0) continue;
					
					List<KeyValuePair<float,int>> tethers = new List<KeyValuePair<float,int>>(islands.Count);
					
					// Find the closest particle in each island, and add it to tethers.
					foreach(HashSet<int> island in islands){
						int closest = -1;
						float minDistance = Mathf.Infinity;
						foreach (int j in island){

							// TODO: Use linear distance along the rope in a more efficient way. precalculate it on generation!
							int min = Mathf.Min(i,j);
							int max = Mathf.Max(i,j);
							float distance = 0;
							for (int k = min; k < max; ++k)
								distance += Vector3.Distance(positions[k],
															 positions[k+1]);

							if (distance < minDistance){
								minDistance = distance;
								closest = j;
							}
						}
						if (closest >= 0)
							tethers.Add(new KeyValuePair<float,int>(minDistance, closest));
					}
					
					// Sort tether indices by distance:
					tethers.Sort(
					delegate(KeyValuePair<float,int> x, KeyValuePair<float,int> y)
					{
						return x.Key.CompareTo(y.Key);
					}
					);
					
					// Create constraints for "maxTethers" closest anchor particles:
					for (int k = 0; k < Mathf.Min(maxTethers,tethers.Count); ++k){
						tetherConstraints.AddConstraint(true,i,tethers[k].Value,tethers[k].Key,1,1);
					}
				}
	            
	        }
	        
	        return true;
	        
		}
		
	}
}



