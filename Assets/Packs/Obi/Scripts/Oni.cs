using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

/**
 * Interface for the Oni particle physics library.
 */
public static class Oni {

	public enum ConstraintType
    {
        Tether = 0,
        Pin = 1,
        Volume = 2,
        Bending = 3,
        Distance = 4,
		Chain = 5,
        ParticleCollision = 6,
        Density = 7,
        Collision = 8,
        Skin = 9,
        Aerodynamics = 10,
        ShapeMatching = 11,
    };

	public enum ParticlePhase{
		SelfCollide = 1 << 24,
		Fluid = 1 << 25
	}

	public enum ShapeType{
		Sphere = 0,
		Box = 1,
		Capsule = 2,
		Heightmap = 3,
		TriangleMesh = 4,
		EdgeMesh = 5
	}

	public enum MaterialCombineMode{
		Average = 0,
		Minimium = 1,
		Multiply = 2,
        Maximum = 3
    }

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SolverParameters{

		public enum Interpolation
		{
			None,
			Interpolate,
		};

		public enum Mode
		{
			Mode3D,
			Mode2D,
		};

		[Tooltip("In 2D mode, particles are simulated on the XY plane only. For use in conjunction with Unity's 2D mode.")]
		public Mode mode;

		[Tooltip("Same as Rigidbody.interpolation. Set to INTERPOLATE for cloth that is applied on a main character or closely followed by a camera. NONE for everything else.")]
		public Interpolation interpolation;

		public Vector3 gravity;

		[Tooltip("Percentage of velocity lost per second, between 0% (0) and 100% (1).")]
		[Range(0,1)]
		public float damping; 

		[Tooltip("Radius of diffuse particle advection. Large values yield better quality but are more expensive.")]
		public float advectionRadius; 	

		[Tooltip("Kinetic energy below which particle positions arent updated. Energy values are mass-normalized, so all particles in the solver have the same threshold.")]
		public float sleepThreshold; 		              		              

		public SolverParameters(Interpolation interpolation, Vector4 gravity){
			this.mode = Mode.Mode3D;
			this.gravity = gravity;
			this.interpolation = interpolation;
			damping = 0;
			advectionRadius = 0.5f;
			sleepThreshold = 0.001f;
		}

	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct ConstraintParameters{

		public enum EvaluationOrder
		{
			Sequential,
			Parallel
		};

		[Tooltip("Whether this constraint group is solved or not.")]
		[MarshalAs(UnmanagedType.I1)]
		public bool enabled;

		[Tooltip("Order in which constraints are evaluated. SEQUENTIAL converges faster but is not very stable. PARALLEL is very stable but converges slowly, requiring more iterations to achieve the same result.")]
		public EvaluationOrder evaluationOrder;								/**< Constraint evaluation order.*/
		
		[Tooltip("Number of relaxation iterations performed by the constraint solver. A low number of iterations will perform better, but be less accurate.")]
		public int iterations;												/**< Amount of solver iterations per step for this constraint group.*/
		
		[Tooltip("Over (or under if < 1) relaxation factor used. At 1, no overrelaxation is performed. At 2, constraints double their relaxation rate. High values reduce stability but improve convergence.")]
		[Range(0.1f,2)]
		public float SORFactor;												/**< Sucessive over-relaxation factor for parallel evaluation order.*/
		

		public ConstraintParameters(bool enabled, EvaluationOrder order, int iterations){
			this.enabled = enabled;
			this.iterations = iterations;
			this.evaluationOrder = order;
			this.SORFactor = 1;
		}
		
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Bounds{
		public Vector4 min;
		public Vector4 max;

		public Vector4 Center{
			get{return min + (max-min)*0.5f;}
		}

		public Vector4 Size{
			get{return max-min;}
		}

		public Bounds(Vector4 min, Vector4 max){
			this.min = min;
			this.max = max;
		}
	}

	// In this particular case, size is forced to 80 bytes to ensure 16 byte memory alignment needed by Oni.
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 80)]
	public struct Rigidbody{

		public Vector4 linearVelocity;
		public Vector4 angularVelocity;
		public Vector4 centerOfMass;
		public Vector4 inertiaTensor;
		public float inverseMass;
		
		public Rigidbody(UnityEngine.Rigidbody source){
			linearVelocity = source.velocity;
			angularVelocity = source.angularVelocity;

			// center of mass in unity is affected by local rotation and poistion, but not scale. We need it expressed in world space:
			centerOfMass = source.transform.position + source.transform.rotation * source.centerOfMass;

			Vector3 invTensor = new Vector3((source.constraints & RigidbodyConstraints.FreezeRotationX) != 0?0:1/source.inertiaTensor.x,
											(source.constraints & RigidbodyConstraints.FreezeRotationY) != 0?0:1/source.inertiaTensor.y,
											(source.constraints & RigidbodyConstraints.FreezeRotationZ) != 0?0:1/source.inertiaTensor.z);

			Vector3 invTensorDiagonal = source.inertiaTensorRotation * invTensor;

			// the inertia tensor is a diagonal matrix (Vector3) because it is expressed in the space generated by the principal axes of rotation (inertiaTensorRotation).
			inertiaTensor = source.isKinematic ? Vector4.zero : new Vector4(invTensorDiagonal.x,invTensorDiagonal.y,invTensorDiagonal.z,0);
			inverseMass = source.isKinematic ? 0 : 1/source.mass;
		}

		public Rigidbody(UnityEngine.Rigidbody2D source){

			linearVelocity = source.velocity;

			// For some weird reason, in 2D angular velocity is measured in *degrees* per second, 
			// instead of radians. Seriously Unity, WTF??
			angularVelocity = new Vector4(0,0,source.angularVelocity * Mathf.Deg2Rad,0);

			// center of mass in unity is affected by local rotation and poistion, but not scale. We need it expressed in world space:
			centerOfMass = source.transform.position + source.transform.rotation * source.centerOfMass;

			inertiaTensor = source.isKinematic ? Vector4.zero : new Vector4(0,0,(source.constraints & RigidbodyConstraints2D.FreezeRotation) != 0?0:1/source.inertia,0);
			inverseMass = source.isKinematic ? 0 : 1/source.mass;

		}
	}

	// In this particular case, size is forced to 128 bytes to ensure 16 byte memory alignment needed by Oni.
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 128)]
	public struct Collider{
		public Quaternion rotation;
		public Quaternion inverse_rotation;
		public Oni.Bounds bounds;
		public Vector4 translation;
		public Vector4 scale;
		public float contactOffset;
		public int collisionGroup;
		public ShapeType shapeType;
		public int shapeIndex;
		public int rigidbodyIndex;
		public int materialIndex;

		public Collider(UnityEngine.Collider source, ShapeType shapeType, float thickness, int shapeIndex, int rigidbodyIndex, int materialIndex){
			bounds = new Oni.Bounds(source.bounds.min - Vector3.one*(thickness + source.contactOffset),
									source.bounds.max + Vector3.one*(thickness + source.contactOffset));
			translation = source.transform.position;
			rotation = source.transform.rotation;
			inverse_rotation = Quaternion.Inverse(source.transform.rotation);
			scale = new Vector4(source.transform.lossyScale.x,source.transform.lossyScale.y,source.transform.lossyScale.z,1);
			contactOffset = thickness;
			this.collisionGroup = source.gameObject.layer;
			this.shapeType = shapeType;
			this.shapeIndex = shapeIndex;
			this.rigidbodyIndex = rigidbodyIndex;
			this.materialIndex = materialIndex;
		}

		public Collider(UnityEngine.Collider2D source, ShapeType shapeType, float thickness, int shapeIndex, int rigidbodyIndex, int materialIndex){
			bounds = new Oni.Bounds(source.bounds.min - Vector3.one * (thickness + 0.01f), //allow some room for contacts to be generated before penetration.
									source.bounds.max + Vector3.one * (thickness + 0.01f));
			translation = source.transform.position;
			rotation = source.transform.rotation;
			inverse_rotation = Quaternion.Inverse(source.transform.rotation);
			scale = new Vector4(source.transform.lossyScale.x,source.transform.lossyScale.y,source.transform.lossyScale.z,1);
			contactOffset = thickness;
			this.collisionGroup = source.gameObject.layer;
			this.shapeType = shapeType;
			this.shapeIndex = shapeIndex;
			this.rigidbodyIndex = rigidbodyIndex;
			this.materialIndex = materialIndex;
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct CollisionMaterial{
		public float friction;
		public float stickiness;
		public float stickDistance;
		public MaterialCombineMode frictionCombine;
		public MaterialCombineMode stickinessCombine;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FluidMaterial{
		public float smoothingRadius;
		public float relaxationFactor;
		public float restDensity;
		public float viscosity;
		public float cohesion;
		public float surfaceTension;
		public float buoyancy;
		public float atmosphericDrag;
		public float atmosphericPressure;
		public float vorticity;
		public float elasticRange;
		public float plasticCreep;
		public float plasticThreshold;
	}

	// In this particular case, size is forced to 32 bytes to ensure 16 byte memory alignment needed by Oni.
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
	public struct SphereShape{
		public Vector4 center;	//first 16 bytes
		public float radius;	//next 4 bytes, 12 bytes left unused for padding.

		[MarshalAs(UnmanagedType.I1)]
		public bool is2D;

		public SphereShape(SphereCollider source){
			center = source.center;
			radius = source.radius;
			is2D = false;
		}

		public SphereShape(CircleCollider2D source){
			center = source.offset;
			radius = source.radius;
			is2D = true;
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BoxShape{
		public Vector4 center;	
		public Vector4 size;

		[MarshalAs(UnmanagedType.I1)]
		public bool is2D;
		
		public BoxShape(BoxCollider source){
			center = source.center;
			size = source.size;
			is2D = false;
        }

		public BoxShape(BoxCollider2D source){
			center = source.offset;
			size = source.size;
			is2D = true;
        }
	}
	
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct CapsuleShape{
		public Vector4 center;	
		public float radius;
		public float height;
		public int direction;
		
		public CapsuleShape(CapsuleCollider source){
			center = source.center;
			radius = source.radius;
			height = source.height;
			direction = source.direction;
		}

		public CapsuleShape(CharacterController source){
			center = source.center;
			radius = source.radius;
			height = source.height;
			direction = 1;
		}
	}
	
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct HeightData{

		TerrainCollider source;
		GCHandle dataHandle;
		
		public HeightData(TerrainCollider source){
			this.dataHandle = new GCHandle();
			this.source = source;
			UpdateHeightData();
        }

		/**
		 * Updates the shared memory region between Obi and Oni where terrain height data resides. 
		 */
		public void UpdateHeightData(){

			float[,] heights = source.terrainData.GetHeights(0,0,source.terrainData.heightmapWidth,source.terrainData.heightmapHeight);
			
			float[] buffer = new float[source.terrainData.heightmapWidth * source.terrainData.heightmapHeight];
			for (int y = 0; y < source.terrainData.heightmapHeight; ++y)
				for (int x = 0; x < source.terrainData.heightmapWidth; ++x)
					buffer[y*source.terrainData.heightmapWidth+x] = heights[y,x];
			
			if (dataHandle.IsAllocated)
				UnpinMemory(dataHandle);

			dataHandle = PinMemory(buffer);
		}
        
        public void UnpinData(){
			UnpinMemory(dataHandle);
        }

		public IntPtr AddrOfHeightData(){
			return dataHandle.AddrOfPinnedObject();
		}        

    }
	
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct HeightmapShape{
		public Vector3 size;
		//pointer to a HeightData object. Done this way because we do not know the array size beforehand, and
		//the struct memory layout must be contiguous.
        public IntPtr data;	 
        public int resolutionU;
		public int resolutionV;
		public float sampleWidth;
		public float sampleHeight;
		
		public HeightmapShape(TerrainCollider source, IntPtr data){
			resolutionU = source.terrainData.heightmapWidth;
			resolutionV = source.terrainData.heightmapHeight;
			sampleWidth = source.terrainData.heightmapScale.x;
			sampleHeight = source.terrainData.heightmapScale.z;
			size = source.terrainData.size;
			this.data = data;
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct TriangleMeshData{
	
		MeshCollider source;
		GCHandle verticesHandle;
		GCHandle normalsHandle;
		GCHandle trianglesHandle;
		
		public TriangleMeshData(MeshCollider source){
			this.verticesHandle = new GCHandle();
			this.trianglesHandle = new GCHandle();
			this.normalsHandle = new GCHandle();
			this.source = source;
			UpdateMeshData();
        }

		/**
		 * Updates the shared memory region between Obi and Oni where triangle mesh data resides. 
		 */
		public void UpdateMeshData(){

			Vector3[] vertices = source.sharedMesh.vertices;
			Vector3[] normals = source.sharedMesh.normals;
			int[] triangles = source.sharedMesh.triangles;
			
			if (verticesHandle.IsAllocated)
				UnpinMemory(verticesHandle);

			if (normalsHandle.IsAllocated)
				UnpinMemory(normalsHandle);

			if (trianglesHandle.IsAllocated)
				UnpinMemory(trianglesHandle);

			verticesHandle = PinMemory(vertices);
			normalsHandle = PinMemory(normals);
			trianglesHandle = PinMemory(triangles);

		}
        
        public void UnpinData(){
			UnpinMemory(verticesHandle);
			UnpinMemory(normalsHandle);
			UnpinMemory(trianglesHandle);
        }

		public IntPtr AddrOfVertexData(){
			return verticesHandle.AddrOfPinnedObject();
		} 

		public IntPtr AddrOfNormalsData(){
			return normalsHandle.AddrOfPinnedObject();
		}  

		public IntPtr AddrOfTriangleData(){
			return trianglesHandle.AddrOfPinnedObject();
		}        

    }

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct TriangleMeshShape{

		public enum MeshColliderType
	    {
	        Solid = 0,
	        ThinOneSided = 1,
	        ThinTwoSided = 2
	    };

		public IntPtr vertexPositions;
        public IntPtr triangleIndices;	
        public int numVertices;
		public int numTriangles;
		public float triangleThickness;
		public MeshColliderType type;
		
		public TriangleMeshShape(MeshCollider source, MeshColliderType type, float thickness,IntPtr vertexPositions, IntPtr triangleIndices){
			this.type = type;
			this.triangleThickness = thickness;
			numVertices = source.sharedMesh.vertexCount;
			numTriangles = source.sharedMesh.triangles.Length/3;
			this.vertexPositions = vertexPositions;
			this.triangleIndices = triangleIndices;
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct EdgeMeshData{
	
		EdgeCollider2D source;
		GCHandle verticesHandle;
		GCHandle edgesHandle;
		
		public EdgeMeshData(EdgeCollider2D source){
			this.verticesHandle = new GCHandle();
			this.edgesHandle = new GCHandle();
			this.source = source;
			UpdateMeshData();
        }

		/**
		 * Updates the shared memory region between Obi and Oni where edge mesh data resides. 
		 */
		public void UpdateMeshData(){

			Vector3[] vertices = new Vector3[source.pointCount];
			int[] edges = new int[source.edgeCount*2];

			for (int i = 0; i < source.pointCount; ++i){
				vertices[i] = source.points[i];
			}

			for (int i = 0; i < source.edgeCount; ++i){
				edges[i*2] = i;
				edges[i*2+1] = i+1;
			}
			
			if (verticesHandle.IsAllocated)
				UnpinMemory(verticesHandle);

			if (edgesHandle.IsAllocated)
				UnpinMemory(edgesHandle);

			verticesHandle = PinMemory(vertices);
			edgesHandle = PinMemory(edges);

		}
        
        public void UnpinData(){
			UnpinMemory(verticesHandle);
			UnpinMemory(edgesHandle);
        }

		public IntPtr AddrOfVertexData(){
			return verticesHandle.AddrOfPinnedObject();
		} 

		public IntPtr AddrOfEdgeData(){
			return edgesHandle.AddrOfPinnedObject();
		}        

    }

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct EdgeMeshShape{

		public IntPtr vertexPositions;
        public IntPtr edgeIndices;	
        public int numVertices;
		public int numEdges;
		public float edgeThickness;
		public bool is2D;
		
		public EdgeMeshShape(EdgeCollider2D source,float thickness,IntPtr vertexPositions, IntPtr edgeIndices){
			this.edgeThickness = thickness;
			numVertices = source.pointCount;
			numEdges = source.edgeCount;
			this.vertexPositions = vertexPositions;
			this.edgeIndices = edgeIndices;
			this.is2D = true;
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct HalfEdge{
		public int index;	
		public int indexInFace;
		public int face;
		public int nextHalfEdge;
		public int pair;
		public int endVertex;

		public HalfEdge(int index){
			this.index = index;	
			indexInFace = -1;
			face = -1;
			nextHalfEdge = -1;
			pair = -1;
			endVertex = -1;
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Vertex{
		public int index;	
		public int halfEdge;
		public Vector3 position;

		public Vertex(Vector3 position, int index, int halfEdge){
			this.index = index;
			this.halfEdge = halfEdge;
			this.position = position;
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Face{
		public int index;	
		public int halfEdge;
		public int visualVertex1; //workaround for fixed-size arrays (not supported by C# :()
		public int visualVertex2;
		public int visualVertex3;

		public Face(int index){
			this.index = index;
			halfEdge = -1;
			visualVertex1 = -1;
			visualVertex2 = -1;
			visualVertex3 = -1;
		}
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MeshInformation{
		public float volume;	
		public float area;
		public int borderEdgeCount;

		[MarshalAs(UnmanagedType.I1)]
		public bool closed;
		[MarshalAs(UnmanagedType.I1)]
		public bool nonManifold;
	}

	public static GCHandle PinMemory(object data){
		return GCHandle.Alloc(data, GCHandleType.Pinned);
	}

	public static void UnpinMemory(GCHandle handle){
		handle.Free();
	}

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern IntPtr CreateColliderGroup();
	
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void DestroyColliderGroup(IntPtr group);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetColliders(IntPtr group, Oni.Collider[] colliders, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int RemoveColliders(IntPtr group, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetColliderCount(IntPtr group);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetRigidbodies(IntPtr group, Oni.Rigidbody[] rigidbodies, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetRigidbodies(IntPtr group, [Out] Oni.Rigidbody[] rigidbodies, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int RemoveRigidbodies(IntPtr group, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetRigidbodyCount(IntPtr group);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetShapeCount(IntPtr group, ShapeType shapeType);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetSphereShapes(IntPtr group, Oni.SphereShape[] shapes, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int RemoveSphereShapes(IntPtr group, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetBoxShapes(IntPtr group, Oni.BoxShape[] shapes, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int RemoveBoxShapes(IntPtr group, int num, int sourceOffset);
	
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetCapsuleShapes(IntPtr group, Oni.CapsuleShape[] shapes, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int RemoveCapsuleShapes(IntPtr group, int num, int sourceOffset);
	
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetHeightmapShapes(IntPtr group, Oni.HeightmapShape[] shapes, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int RemoveHeightmapShapes(IntPtr group, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetTriangleMeshShapes(IntPtr group, Oni.TriangleMeshShape[] shapes, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int RemoveTriangleMeshShapes(IntPtr group, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int UpdateTriangleMeshShapes(IntPtr group, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetEdgeMeshShapes(IntPtr group, Oni.EdgeMeshShape[] shapes, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int RemoveEdgeMeshShapes(IntPtr group, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int UpdateEdgeMeshShapes(IntPtr group, int num, int sourceOffset);
        
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern IntPtr CreateSolver(int maxParticles, int maxDiffuseParticles, int maxNeighbours);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void DestroySolver(IntPtr solver);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetBounds(IntPtr solver, ref Vector3 min, ref Vector3 max);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetSolverParameters(IntPtr solver, [MarshalAs(UnmanagedType.Struct)] ref SolverParameters parameters);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetSolverParameters(IntPtr solver, [MarshalAs(UnmanagedType.Struct)] ref SolverParameters parameters);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetActiveParticles(IntPtr solver, int[] active, int num);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void AddSimulationTime(IntPtr solver, float step_dt);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void UpdateSolver(IntPtr solver, float substep_dt);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void ApplyPositionInterpolation(IntPtr solver, float substep_dt);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetConstraintsOrder(IntPtr solver, [Out] int[] order);
	
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetConstraintsOrder(IntPtr solver, int[] order);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetConstraintCount(IntPtr solver, int type);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetActiveConstraintIndices(IntPtr solver, [Out] int[] indices, int num , int type);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetRenderableParticlePositions(IntPtr solver, Vector4[] positions, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetRenderableParticlePositions(IntPtr solver, [Out] Vector4[] positions,int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetParticlePhases(IntPtr solver, int[] phases, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetParticlePositions(IntPtr solver, Vector4[] positions, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetParticlePositions(IntPtr solver, [Out] Vector4[] positions, int num, int sourceOffset);
	
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetParticleInverseMasses(IntPtr solver, float[] invMasses, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetParticleSolidRadii(IntPtr solver, float[] radii, int num, int destOffset);
	
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetParticleVelocities(IntPtr solver, Vector4[] velocities, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetParticleVelocities(IntPtr solver, [Out] Vector4[] velocities, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetParticleVorticities(IntPtr solver, [Out] Vector4[] vorticities, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetParticleVorticities(IntPtr solver, Vector4[] vorticities, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetParticleNormals(IntPtr solver, [Out] Vector4[] normals, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetParticleDensities(IntPtr solver, [Out] float[] densities, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetConstraintGroupParameters(IntPtr solver, int type, [MarshalAs(UnmanagedType.Struct)] ref ConstraintParameters parameters);
	
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetConstraintGroupParameters(IntPtr solver, int type, [MarshalAs(UnmanagedType.Struct)] ref ConstraintParameters parameters);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetColliderGroup(IntPtr solver, IntPtr group);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetCollisionMaterials(IntPtr solver, Oni.CollisionMaterial[] materials, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetMaterialIndices(IntPtr solver, int[] indices, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetIgnoredParticles(IntPtr solver, int[] ignoredParticleCollisions, int num, int particle);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetFluidMaterials(IntPtr solver, Oni.FluidMaterial[] materials, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetFluidMaterialIndices(IntPtr solver, int[] indices, int num, int destOffset);
	
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void ActivateConstraints(IntPtr solver, int type, int[] active, int n);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void DeactivateConstraints(IntPtr solver, int type, int[] inactive, int n);

 	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int RemoveConstraints(IntPtr solver,int type, int num,int destOffset);

    #if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetDistanceConstraints(IntPtr solver, int[] indices,
																	float[] restLengths,
																	Vector2[] stiffnesses,
																	int num,
																	int destOffset);

 	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetDistanceConstraintsStretching(IntPtr solver, 
															   [Out] float[] stretching,
															   int num,
															   int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetBendingConstraints(IntPtr solver, 
													int[] indices,
													float[] restBends,
													Vector2[] bendingStiffnesses,
													int num,
													int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetSkinConstraints(IntPtr solver, 
												 int[] indices,
												 Vector4[] points,
												 Vector4[] normals,
												 float[] radiiBackstops,
												 float[] stiffnesses,
												 int num,
												 int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetAerodynamicConstraints(IntPtr solver, 
														int[] triangleIndices, 
														Vector4[] triangleNormals,
	                                                    Vector4[] wind, 
														float[] aerodynamicCoeffs,
														int num,
														int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int UpdateAerodynamicNormals(IntPtr solver, 
										   			  Vector4[] triangleNormals,
										   			  int num,
										   			  int destOffset);
    
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetVolumeConstraints(IntPtr solver, 
												   int[] triangleIndices,
	                                               int[] firstTriangle,
	                                               int[] numTriangles,
												   float[] restVolumes,
												   Vector2[] pressureStiffnesses,
												   int num,
												   int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetVolumeTriangleCount(IntPtr solver);


	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetChainConstraints(IntPtr solver, 
												 int[] particleIndices,
	                                             int[] firstIndex,
	                                             int[] numIndices,
												 Vector2[] minMaxLenghts,
												 int num,
												 int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetChainParticleCount(IntPtr solver);

	
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetTetherConstraints(IntPtr solver, 
												   int[] indices,
												   Vector2[] maxLenghtsScales,
												   float[] stiffnesses,
												   int num,
												   int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetPinConstraints(IntPtr solver, 
											    int[] indices,
												Vector4[] pinOffsets,
												float[] stiffnesses,
												int num,
												int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetCollisionIndices(IntPtr solver, [Out] int[] indices, int n);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetCollisionDistances(IntPtr solver, [Out] float[] distances, int n);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetCollisionPoints(IntPtr solver, [Out] Vector4[] points, int n);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetCollisionNormals(IntPtr solver,[Out] Vector4[] normals, int n);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetCollisionNormalImpulses(IntPtr solver,[Out] float[] impulses, int n);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetCollisionTangentImpulses(IntPtr solver,[Out] float[] impulses, int n);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetCollisionStickImpulses(IntPtr solver,[Out] float[] impulses, int n);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetActiveDiffuseParticles(IntPtr solver, int[] active, int num);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetDiffuseParticlePositions(IntPtr solver, Vector4[] points, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int SetDiffuseParticleVelocities(IntPtr solver, Vector4[] velocities, int num, int destOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetDiffuseParticleVelocities(IntPtr solver,[Out] Vector4[] velocities, int num, int sourceOffset);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetDiffuseParticleNeighbourCounts(IntPtr solver, IntPtr neighbourCounts);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern IntPtr CreateHalfEdgeMesh();

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void DestroyHalfEdgeMesh(IntPtr mesh);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetVertices(IntPtr mesh, IntPtr vertices, int n);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetHalfEdges(IntPtr mesh, IntPtr halfedges, int n);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void SetFaces(IntPtr mesh, IntPtr faces, int n);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetVertices(IntPtr mesh, IntPtr vertices);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetHalfEdges(IntPtr mesh, IntPtr halfedges);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void GetFaces(IntPtr mesh, IntPtr faces);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetVertexCount(IntPtr mesh);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetHalfEdgeCount(IntPtr mesh);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetFaceCount(IntPtr mesh);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int GetHalfEdgeMeshInfo(IntPtr mesh, [MarshalAs(UnmanagedType.Struct)] ref MeshInformation meshInfo);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void Generate(IntPtr mesh, IntPtr vertices, IntPtr triangles, int vertexCount, int triangleCount, IntPtr scale);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void AreaWeightedNormals(IntPtr mesh, IntPtr mesh_vertices, IntPtr mesh_normals);

	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern void VertexOrientations(IntPtr mesh, IntPtr mesh_vertices, IntPtr mesh_normals, IntPtr vertex_orientations);

    #if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern int MakePhase(int group, ParticlePhase flags);
	
	#if (UNITY_IPHONE && !UNITY_EDITOR)
		[DllImport ("__Internal")]
	#else
		[DllImport ("libOni")] 
	#endif
	public static extern float BendingConstraintRest(float[] constraintCoordinates);

}
