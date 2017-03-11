using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiRope components.
	 * Allows particle selection and constraint edition. 
	 * 
	 * Selection:
	 * 
	 * - To select a particle, left-click on it. 
	 * - You can select multiple particles by holding shift while clicking.
	 * - To deselect all particles, click anywhere on the object except a particle.
	 * 
	 * Constraints:
	 * 
	 * - To edit particle constraints, select the particles you wish to edit.
	 * - Constraints affecting any of the selected particles will appear in the inspector.
	 * - To add a new pin constraint to the selected particle(s), click on "Add Pin Constraint".
	 * 
	 */
	[CustomEditor(typeof(ObiRope)), CanEditMultipleObjects] 
	public class ObiRopeEditor : ObiParticleActorEditor
	{
		
		[MenuItem("Component/Physics/Obi/Obi Rope",false,0)]
		static void AddObiRope()
		{
			foreach(Transform t in Selection.transforms)
				Undo.AddComponent<ObiRope>(t.gameObject);
		}

		[MenuItem("Assets/Create/Obi/Obi Rope Section")]
		public static void CreateObiRopeSection ()
		{
			ObiEditorUtils.CreateAsset<ObiRopeSection> ();
		}

		[MenuItem("GameObject/3D Object/Obi/Obi Rope (fully set up)",false,4)]
		static void CreateObiRope()
		{
			GameObject c = new GameObject("Obi Rope");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Rope");
			GameObject p = new GameObject("Rope path");
			Undo.RegisterCreatedObjectUndo(p,"Create Obi Rope path");
			ObiRope rope = c.AddComponent<ObiRope>();
			ObiCatmullRomCurve path = p.AddComponent<ObiCatmullRomCurve>();
			ObiSolver solver = c.AddComponent<ObiSolver>();
			ObiColliderGroup group = c.AddComponent<ObiColliderGroup>();
			
			rope.Solver = solver;
			rope.Section = Resources.Load("DefaultRopeSection") as ObiRopeSection;
			rope.ropePath = path;
			solver.colliderGroup = group;
		}
		
		ObiRope rope;
		SerializedProperty chainLinks;
		
		public override void OnEnable(){
			base.OnEnable();
			rope = (ObiRope)target;
			chainLinks = serializedObject.FindProperty("chainLinks");
		}
		
		public override void OnDisable(){
			base.OnDisable();
			EditorUtility.ClearProgressBar();
		}

		public override void UpdateParticleEditorInformation(){
			
			for(int i = 0; i < rope.positions.Length; i++)
			{
				wsPositions[i] = rope.GetParticlePosition(i);		
			}

			for(int i = 0; i < rope.positions.Length; i++)
			{
				facingCamera[i] = IsParticleFacingCamera(Camera.current, i);
			}
			
		}

		public bool IsParticleFacingCamera(Camera cam, int particleIndex){

			return true;

		}
		
		protected override void SetPropertyValue(ParticleProperty property,int index, float value){
			
			switch(property){
			case ParticleProperty.MASS: 
				rope.mass[index] = value;
				float areaMass = rope.mass[index];
				if (areaMass > 0){
					rope.invMasses[index] = 1 / areaMass;
				}else{
					rope.invMasses[index] = 0;
				}
				break; 
			}
			
		}
		
		protected override float GetPropertyValue(ParticleProperty property, int index){
			switch(property){
				case ParticleProperty.MASS:{
					return rope.mass[index];
				}
			}
			return 0;
		}

		public override void OnInspectorGUI() {
			
			serializedObject.Update();

			GUI.enabled = rope.Initialized;
			EditorGUI.BeginChangeCheck();
			editMode = GUILayout.Toggle(editMode,new GUIContent("Edit particles",EditorGUIUtility.Load("EditParticles.psd") as Texture2D),"LargeButton");
			if (EditorGUI.EndChangeCheck()){
				SceneView.RepaintAll();
			}
			GUI.enabled = true;			

			EditorGUILayout.LabelField("Status: "+ (rope.Initialized ? "Initialized":"Not initialized"));

			GUI.enabled = (rope.ropePath != null && rope.Section != null);
			if (GUILayout.Button("Initialize")){
				if (!rope.Initialized){
					CoroutineJob job = new CoroutineJob();
					routine = EditorCoroutine.StartCoroutine(job.Start(rope.GeneratePhysicRepresentationForMesh()));
				}else{
					if (EditorUtility.DisplayDialog("Actor initialization","Are you sure you want to re-initialize this actor?","Ok","Cancel")){
						CoroutineJob job = new CoroutineJob();
						routine = EditorCoroutine.StartCoroutine(job.Start(rope.GeneratePhysicRepresentationForMesh()));
					}
				}
			}
			GUI.enabled = true;

			GUI.enabled = rope.Initialized;
			if (GUILayout.Button("Set Rest State")){
				rope.PullDataFromSolver(new ObiSolverData(ObiSolverData.ParticleData.POSITIONS | ObiSolverData.ParticleData.VELOCITIES));
			}
			GUI.enabled = true;	
			
			if (rope.ropePath == null){
				EditorGUILayout.HelpBox("Rope path spline is missing.",MessageType.Info);
			}
			if (rope.Section == null){
				EditorGUILayout.HelpBox("Rope section is missing.",MessageType.Info);
			}

			rope.Solver = EditorGUILayout.ObjectField("Solver",rope.Solver, typeof(ObiSolver), true) as ObiSolver;

			Editor.DrawPropertiesExcluding(serializedObject,"m_Script","chainLinks");

			float newTwist = EditorGUILayout.FloatField(new GUIContent("Section twist","Amount of twist applied to each section, in degrees."),rope.SectionTwist);
			if (rope.SectionTwist != newTwist){
				Undo.RecordObject(rope, "Set section twist");
				rope.SectionTwist = newTwist;
			}
			
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);

			ObiRope.RenderingMode newRenderMode = (ObiRope.RenderingMode) EditorGUILayout.EnumPopup(rope.RenderMode);
			if (rope.RenderMode != newRenderMode){
				Undo.RecordObject(rope, "Set rope render mode");
				rope.RenderMode = newRenderMode;
			}

			// Render-mode specific stuff:
			if (rope.RenderMode == ObiRope.RenderingMode.ProceduralRope)
			{
				ObiRopeSection newSection = EditorGUILayout.ObjectField(new GUIContent("Section","Section asset to be extruded along the rope path.")
																	,rope.Section, typeof(ObiRopeSection), false) as ObiRopeSection;
				if (rope.Section != newSection){
					Undo.RecordObject(rope, "Set rope section");
					rope.Section = newSection;
				}
	
				float newThickness = EditorGUILayout.FloatField(new GUIContent("Thickness","Thickness of the rope."),rope.Thickness);
				if (rope.Thickness != newThickness){
					Undo.RecordObject(rope, "Set rope thickness");
					rope.Thickness = newThickness;
				}
	
				int newCapSections = EditorGUILayout.IntField(new GUIContent("Cap sections","Amount of subdivisions for the end caps. Use 0 if you want to leave ends uncapped."),rope.CapSections);
				if (rope.CapSections != newCapSections){
					Undo.RecordObject(rope, "Set rope cap sections");
					rope.CapSections = newCapSections;
				}

				Vector2 newUVScale = EditorGUILayout.Vector2Field(new GUIContent("UV scale","Scaling of the uv coordinates generated for the rope. The u coordinate wraps around the whole rope section, and the v spans the full length of the rope."),rope.UVScale);
				if (rope.UVScale != newUVScale){
					Undo.RecordObject(rope, "Set chain uv scale");
					rope.UVScale = newUVScale;
				}

				bool newNormalizeV = EditorGUILayout.Toggle(new GUIContent("Normalize V","Scaling of the uv coordinates generated for the rope. The u coordinate wraps around the whole rope section, and the v spans the full length of the rope."),rope.NormalizeV);
				if (rope.NormalizeV != newNormalizeV){
					Undo.RecordObject(rope, "Set normalize v");
					rope.NormalizeV = newNormalizeV;
				}

			}else{

				Vector3 newLinkScale = EditorGUILayout.Vector3Field(new GUIContent("Link scale","Scale applied to each chain link."),rope.LinkScale);
				if (rope.LinkScale != newLinkScale){
					Undo.RecordObject(rope, "Set chain link scale");
					rope.LinkScale = newLinkScale;
				}

				bool newRandomizeLinks = EditorGUILayout.Toggle(new GUIContent("Randomize links","Toggling this on this causes each chain link to be selected at random from the set of provided links."),rope.RandomizeLinks);
				if (rope.RandomizeLinks != newRandomizeLinks){
					Undo.RecordObject(rope, "Set randomize links");
					rope.RandomizeLinks = newRandomizeLinks;
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(chainLinks, true);	
				if (EditorGUI.EndChangeCheck()){
					// update the chain representation in response to a change in available link templates:
					serializedObject.ApplyModifiedProperties();	
					rope.GenerateProceduralChainLinks();
				}
			}

			// Progress bar:
			EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",routine);
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
			}
			
		}
		
	}
}


