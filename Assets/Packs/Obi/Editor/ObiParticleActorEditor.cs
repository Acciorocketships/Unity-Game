using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Obi{
	
	/**
	 * Custom inspector for ObiCloth components.
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
	[CustomEditor(typeof(ObiActor)), CanEditMultipleObjects] 
	public abstract class ObiParticleActorEditor : Editor
	{
		
		public enum EditionTool{
			SELECT,
			SELECTBRUSH,
			PAINT,
			CUSTOM,
		}
		
		public enum PaintMode{
			PAINT,
			SMOOTH
		}
		
		public enum ParticleProperty{
			MASS,
			RADIUS,
			SKIN_RADIUS,
			SKIN_BACKSTOP
		}
		
		ObiActor actor;
		Mesh particlesMesh;
		protected EditorCoroutine routine;
		
		public static bool editMode = false;
		static EditionTool tool = EditionTool.SELECT;
		static PaintMode paintMode = PaintMode.PAINT;
		static protected ParticleProperty currentProperty = ParticleProperty.MASS;
		
		static Gradient valueGradient = new Gradient();
		
		static protected bool backfaces = false;
		Rect uirect;
		
		//Property edition related:
		static float selectionProperty = 0;
		static float newProperty = 0;
		
		static float maxValue = Single.MinValue;
		static float minValue = Single.MaxValue;
		
		//Brush related:
		static protected float brushRadius = 50;
		static protected float brushOpacity = 0.01f;
		static protected float minBrushValue = 0;
		static protected float maxBrushValue = 10;
		static protected bool selectionMask = false;
		
		//Selection related:
		static protected int selectedCount = 0;
		
		//Editor playback related:
		static protected bool isPlaying = false;
		static protected float lastFrameTime = 0.0f;
		static protected float accumulatedTime = 0.0f;
		
		protected Vector3 camup;
		protected Vector3 camright;
		protected Vector3 camforward;
		
		//Additional GUI styles:
		static protected GUIStyle separatorLine;
		
		//Additional status info for all particles:
		static public bool[] selectionStatus = new bool[0];
		static protected bool[] facingCamera = new bool[0];
		static protected Vector3[] wsPositions = new Vector3[0];
		
		public virtual void OnEnable(){

			actor = (ObiActor)target;

			particlesMesh = new Mesh();
			particlesMesh.hideFlags = HideFlags.HideAndDontSave;
			
			SetupValuesGradient();
			
			separatorLine = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).box);
			separatorLine.normal.background = EditorGUIUtility.Load("SeparatorLine.psd") as Texture2D;
			separatorLine.border = new RectOffset(3,3,0,0);
			separatorLine.fixedHeight = 3;
			separatorLine.stretchWidth = true;

			EditorApplication.update += Update;
			EditorApplication.playmodeStateChanged += OnPlayModeStateChanged;

		}
		
		public virtual void OnDisable(){
			GameObject.DestroyImmediate(particlesMesh);
			EditorApplication.update -= Update;
			EditorApplication.playmodeStateChanged -= OnPlayModeStateChanged;
			EditorUtility.ClearProgressBar();
		}

		public void OnDestroy()
	    {
	         if ( Application.isEditor )
	         {
	             if(target == null)
					actor.DestroyRequiredComponents();
	         }
	    }
		
		private void SetupValuesGradient(){
			
			GradientColorKey[] gck = new GradientColorKey[2];
			gck[0].color = Color.blue;
			gck[0].time = 0.0f;
			gck[1].color = new Color(1,0.7f,0,1);
			gck[1].time = 1.0f;
			
			GradientAlphaKey[] gak = new GradientAlphaKey[2];
			gak[0].alpha = 1.0f;
			gak[0].time = 0.0f;
			gak[1].alpha = 1.0f;
			gak[1].time = 1.0f;
			
			valueGradient.SetKeys(gck,gak);
		}
		
		private void ResizeParticleArrays(){
			
			if (actor.positions != null){
				
				// Reinitialize particle property min/max values if needed:
				if (selectionStatus.Length != actor.positions.Length){
					ParticlePropertyChanged();
				}
				
				Array.Resize(ref selectionStatus,actor.positions.Length);
				Array.Resize(ref facingCamera,actor.positions.Length);
				Array.Resize(ref wsPositions,actor.positions.Length);
			
			}
			
		}
		
		public static Material particleMaterial;
		static void CreateParticleMaterial() {
			if (!particleMaterial) { 
				particleMaterial = EditorGUIUtility.LoadRequired("EditorParticle.mat") as Material;
			}
		}
		
		public void OnSceneGUI(){

			if (!editMode) 
				return;
			
			CreateParticleMaterial();
			particleMaterial.SetPass(0);
			
			ResizeParticleArrays();
			
			if (!actor.Initialized) return;
			
			if (Camera.current != null){
				
				camup = Camera.current.transform.up;
				camright = Camera.current.transform.right;
				camforward = Camera.current.transform.forward;
			}
			
			if (Event.current.type == EventType.Repaint){
				
				// Update camera facing status and world space positions array:
				UpdateParticleEditorInformation();
				
				// Draw 3D stuff: particles, constraints, grid, etc.
				DrawParticles();
				
			}
			
			// Draw tool handles:
			if (Camera.current != null){
				
				switch(tool){
				case EditionTool.SELECT: 
					if (ObiClothParticleHandles.ParticleSelector(wsPositions,selectionStatus,backfaces,facingCamera)){
						SelectionChanged();
					}
					break;
				case EditionTool.SELECTBRUSH: 
					if (ObiClothParticleHandles.ParticleBrush(wsPositions,backfaces,facingCamera,brushRadius,null,
					                                          	(List<ParticleStampInfo> stampInfo,bool modified)=>{
																	foreach(ParticleStampInfo info in stampInfo){
																		if (actor.active[info.index])
																			selectionStatus[info.index] = !modified;
																	}
																},null,
																EditorGUIUtility.Load("BrushHandle.psd") as Texture2D)){
						SelectionChanged();
					}
					break;
				case EditionTool.PAINT: //TODO: select mask (paint on selected)
					if (ObiClothParticleHandles.ParticleBrush(wsPositions,backfaces,facingCamera,brushRadius,
															 	()=>{
																	// As RecordObject diffs with the end of the current frame,
																	// and this is a multi-frame operation, we need to use RegisterCompleteObjectUndo instead.
																	Undo.RegisterCompleteObjectUndo(actor, "Paint particles");
															  	},
					                                          	PaintbrushStampCallback,
															  	()=>{
																	EditorUtility.SetDirty(actor);
															  	},
					                                          	EditorGUIUtility.Load("BrushHandle.psd") as Texture2D)){
						ParticlePropertyChanged();
					}
					break;
				}
			}
			
			// Sceneview GUI:
			Handles.BeginGUI();			

			GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
			
			if (Event.current.type == EventType.Repaint){
				uirect = GUILayout.Window(0,uirect,DrawUIWindow,"Particle editor");
				uirect.x = Screen.width - uirect.width - 10; //10 and 28 are magic values, since Screen size is not exactly right.
				uirect.y = Screen.height - uirect.height - 28;
			}

			GUILayout.Window(0,uirect,DrawUIWindow,"Particle editor");

			Handles.EndGUI();
			
		}
		
		private void ForceWindowRelayout(){
			uirect.Set(0,0,0,0);
		}
		
		private void DrawParticles(){

			//Draw all cloth vertices:		
			particlesMesh.Clear();
			Vector3[] vertices = new Vector3[actor.positions.Length * 4];
			Vector2[] uv = new Vector2[actor.positions.Length * 4];
			Color[] colors = new Color[actor.positions.Length * 4];
			int[] triangles = new int[actor.positions.Length * 6];
			
			for(int i = 0; i < actor.positions.Length; i++)
			{
				// skip particles not facing the camera, or inactive ones:
				if (!actor.active[i] || (!backfaces && !facingCamera[i])) continue;

				int i4 = i*4;
				int i41 = i4+1;
				int i42 = i4+2;
                int i43 = i4+3;
                int i6 = i*6;
				
				// get particle size in screen space:
				float size = HandleUtility.GetHandleSize(wsPositions[i])*0.05f;
				
				// get particle color:
				Color color;
				if (actor.invMasses[i] == 0){
					color = Color.red;
				}else{
					color = GetPropertyValueGradient(GetPropertyValue(currentProperty,i));
				}

				color.a = facingCamera[i] ? 1:0.5f;
				
				uv[i4] = new Vector2(0.5f,1);
				uv[i41] = new Vector2(0,1);
				uv[i42] = Vector3.zero;
				uv[i43] = new Vector2(0.5f,0);

				// highlight the particle if its selected:
				if (selectionStatus[i]){
					uv[i4] = new Vector2(1,1);
					uv[i41] = new Vector2(0.5f,1);
					uv[i42] = new Vector3(0.5f,0);
                    uv[i43] = new Vector2(1,0);
				}
				
				vertices[i4] = wsPositions[i] + camup*size + camright*size;
				vertices[i41] = wsPositions[i] + camup*size - camright*size;
				vertices[i42] = wsPositions[i] - camup*size - camright*size;
				vertices[i43] = wsPositions[i] - camup*size + camright*size;

				colors[i4] = color;
				colors[i41] = color;
				colors[i42] = color;
				colors[i43] = color;
                
                triangles[i6] = i42;
                triangles[i6+1] = i41;
                triangles[i6+2] = i4;
                triangles[i6+3] = i43;
                triangles[i6+4] = i42;
                triangles[i6+5] = i4;
                
                
            }
            particlesMesh.vertices = vertices;
            particlesMesh.triangles = triangles;
            particlesMesh.uv = uv;
			particlesMesh.colors = colors;
            
            Graphics.DrawMeshNow(particlesMesh,Matrix4x4.identity);
        }		

		public virtual void UpdateParticleEditorInformation(){

			for(int i = 0; i < actor.positions.Length; i++)
			{
				if (actor.active[i]){
					wsPositions[i] = actor.transform.TransformPoint(actor.positions[i]);		
					facingCamera[i] = true;
				}
			}

		}
		
		private void SelectionChanged(){
			
			// Find out how many selected particles we have:
			selectedCount = 0;
			for(int i = 0; i < selectionStatus.Length; i++){
				if (actor.active[i] && selectionStatus[i]) selectedCount++;
			}
			
			// Set the initial mass value:
			for(int i = 0; i < selectionStatus.Length; i++){
				if (actor.active[i] && selectionStatus[i]){
					newProperty = selectionProperty = GetPropertyValue(currentProperty,i); 
					break;
				}
			}	
			
			Repaint();	
			
		}
		
		/**
		 * Called when the currenty edited property of any particle as changed.
	 	 */
		protected void ParticlePropertyChanged(){

			maxValue = Single.MinValue;
			minValue = Single.MaxValue;
			
			for(int i = 0; i < actor.invMasses.Length; i++){
				
				//Skip inactive and fixed particles:
				if (!actor.active[i] || actor.invMasses[i] == 0) continue;
				
				float value = GetPropertyValue(currentProperty,i); 
				maxValue = Mathf.Max(maxValue,value);
				minValue = Mathf.Min(minValue,value);
				
			}	

			UpdatePropertyInSolver();	

		}
		
		protected abstract void SetPropertyValue(ParticleProperty property, int index, float value);
			
		protected abstract float GetPropertyValue(ParticleProperty property, int index);

		protected virtual void UpdatePropertyInSolver(){

			switch(currentProperty){
				case ParticleProperty.MASS:
					actor.PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.INV_MASSES));
				break;
				case ParticleProperty.RADIUS:
					actor.PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.SOLID_RADII));
				break;
			}

		}

		private Color GetPropertyValueGradient(float value){
			return valueGradient.Evaluate(Mathf.InverseLerp(minValue,maxValue,value));
		}
		
		protected virtual string GetPropertyName(){
			switch(currentProperty){
			case ParticleProperty.MASS: return "mass";
			case ParticleProperty.RADIUS: return "radius";
			case ParticleProperty.SKIN_RADIUS: return "skin radius";
			case ParticleProperty.SKIN_BACKSTOP: return "skin backstop";
			}
			return "";
		}
		
		/**
	 	* Callback called for each paintbrush stamp (each time the user drags the mouse, and when he first clicks down).
	 	*/ 
		private void PaintbrushStampCallback(List<ParticleStampInfo> stampInfo, bool modified){
			
			// Average and particle count for SMOOTH mode.
			float averageValue = 0;	
			int numParticles = 0;
			
			foreach(ParticleStampInfo info in stampInfo){
				
				// Skip unselected particles, if selection mask is on.
				if (selectionMask && !selectionStatus[info.index]) continue;
				
				switch(paintMode){
				case PaintMode.PAINT: 
					float currentValue = GetPropertyValue(currentProperty,info.index);
					if (modified){
						SetPropertyValue(currentProperty,info.index,Mathf.Max(currentValue - (brushRadius - Mathf.Sqrt(info.sqrDistanceToMouse)) * brushOpacity,minBrushValue));
					}else{
						SetPropertyValue(currentProperty,info.index,Mathf.Min(currentValue + (brushRadius - Mathf.Sqrt(info.sqrDistanceToMouse)) * brushOpacity,maxBrushValue));
					}
					break;
				case PaintMode.SMOOTH:
					averageValue += GetPropertyValue(currentProperty,info.index);
					numParticles++;
					break;
				}
				
			}
			
			if (paintMode == PaintMode.SMOOTH){
				averageValue /= numParticles;
				foreach(ParticleStampInfo info in stampInfo){
					
					// Skip unselected particles, if selection mask is on.
					if (selectionMask && !selectionStatus[info.index]) continue;
					
					float currentValue = GetPropertyValue(currentProperty,info.index);
					if (modified){ //Sharpen
						SetPropertyValue(currentProperty,info.index,Mathf.Clamp(currentValue + (brushRadius - Mathf.Sqrt(info.sqrDistanceToMouse)) * (currentValue - averageValue) * brushOpacity,minBrushValue,maxBrushValue));
					}else{	//Smooth
						SetPropertyValue(currentProperty,info.index,currentValue - (brushRadius - Mathf.Sqrt(info.sqrDistanceToMouse)) * (currentValue - averageValue) * brushOpacity);
					}
				}
			}
			
		}
		
		/**
	 	* Draws a window with cloth tools:
	 	*/
		void DrawUIWindow(int windowID) {
			
			//-------------------------------
			// Visualization options
			//-------------------------------
			GUILayout.BeginHorizontal();
			backfaces = GUILayout.Toggle(backfaces,"backfaces");
			GUILayout.EndHorizontal();
			
			GUILayout.Box("",separatorLine);
			
			//-------------------------------
			// Tools
			//-------------------------------
			bool customMenu = (CustomUIName() != null);

			GUILayout.BeginHorizontal();
			if (GUILayout.Toggle(tool == EditionTool.SELECT,"Select",GUI.skin.FindStyle("ButtonLeft")) && tool != EditionTool.SELECT){
				tool = EditionTool.SELECT;
				ForceWindowRelayout();
			}
			if (GUILayout.Toggle(tool == EditionTool.SELECTBRUSH,"Brush",GUI.skin.FindStyle("ButtonMid")) && tool != EditionTool.SELECTBRUSH){
				tool = EditionTool.SELECTBRUSH;
				ForceWindowRelayout();
			}
			if (GUILayout.Toggle(tool == EditionTool.PAINT,"Paint",customMenu? GUI.skin.FindStyle("ButtonMid"):GUI.skin.FindStyle("ButtonRight")) && tool != EditionTool.PAINT){
				tool = EditionTool.PAINT;
				ForceWindowRelayout();
			}
			if (customMenu){
				if (GUILayout.Toggle(tool == EditionTool.CUSTOM,CustomUIName(),GUI.skin.FindStyle("ButtonRight")) && tool != EditionTool.CUSTOM){
					tool = EditionTool.CUSTOM;
					ForceWindowRelayout();
				}
			}
			GUILayout.EndHorizontal();
			
			EditorGUI.BeginChangeCheck();
			currentProperty = (ParticleProperty) EditorGUILayout.EnumPopup(currentProperty,GUI.skin.FindStyle("DropDown"));
			if (EditorGUI.EndChangeCheck()){
				ParticlePropertyChanged();
			}
			
			switch(tool){
			case EditionTool.SELECT:
				DrawSelectionToolUI();
				break;
			case EditionTool.SELECTBRUSH:
				GUILayout.BeginHorizontal();
				GUILayout.Label("Radius");
				brushRadius = EditorGUILayout.Slider(brushRadius,5,200);
				GUILayout.EndHorizontal();
				DrawSelectionToolUI();
				break;
			case EditionTool.PAINT:
				DrawPaintToolUI();
				break;
			case EditionTool.CUSTOM:
				DrawCustomUI();
				break;
			}
			
			//-------------------------------
			//Playback functions
			//-------------------------------
			GUILayout.Box("",separatorLine);
						
			GUILayout.BeginHorizontal();
			
			GUI.enabled = !EditorApplication.isPlaying;
			
			if (GUILayout.Button(EditorGUIUtility.Load("RewindButton.psd") as Texture2D,GUILayout.MaxHeight(24),GUILayout.Width(42))){
				actor.ResetActor();
				if (actor.InSolver) 
					actor.RemoveFromSolver(null);
				accumulatedTime = 0;
			}
			
			if (GUILayout.Button(EditorGUIUtility.Load("StopButton.psd") as Texture2D,GUILayout.MaxHeight(24),GUILayout.Width(42))){
				isPlaying = false;
			}
			
			if (GUILayout.Button(EditorGUIUtility.Load("PlayButton.psd") as Texture2D,GUILayout.MaxHeight(24),GUILayout.Width(42))){

				if (!actor.InSolver) 
					actor.AddToSolver(null);

				lastFrameTime = Time.realtimeSinceStartup;
				isPlaying = true;

			}
			
			if (GUILayout.Button(EditorGUIUtility.Load("StepButton.psd") as Texture2D,GUILayout.MaxHeight(24),GUILayout.Width(42))){

				isPlaying = false;

				if (!actor.InSolver) 
					actor.AddToSolver(null);

				if (actor.InSolver){
					actor.Solver.AccumulateSimulationTime(Time.fixedDeltaTime);
					actor.Solver.SimulateStep(Time.fixedDeltaTime);
					actor.Solver.EndFrame(Time.fixedDeltaTime);
				}

			}
			
			GUI.enabled = true;
			
			GUILayout.EndHorizontal();
			
		}
		
		void DrawSelectionToolUI(){
			
			GUILayout.Label(selectedCount+" particle(s) selected");
			
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Invert",GUILayout.Width(88))){
				for(int i = 0; i < selectionStatus.Length; i++){
					if (actor.active[i])
						selectionStatus[i] = !selectionStatus[i];
				}
				SelectionChanged();
			}
			GUI.enabled = selectedCount > 0;
			if (GUILayout.Button("Clear",GUILayout.Width(88))){
				for(int i = 0; i < selectionStatus.Length; i++)
					selectionStatus[i] = false;
				SelectionChanged();
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Select fixed",GUILayout.Width(88))){
				for(int i = 0; i < actor.invMasses.Length; i++){
					if (actor.active[i] && actor.invMasses[i] == 0)
						selectionStatus[i] = true;
				}
				SelectionChanged();
			}
			GUI.enabled = selectedCount > 0;
			if (GUILayout.Button("Unselect fixed",GUILayout.Width(88))){
				for(int i = 0; i < actor.invMasses.Length; i++){
					if (actor.active[i] && actor.invMasses[i] == 0)
						selectionStatus[i] = false;
				}
				SelectionChanged();
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal();
			
			GUI.enabled = selectedCount > 0;		
			GUILayout.BeginHorizontal();

			if (GUILayout.Button(new GUIContent("Fix",EditorGUIUtility.Load("PinIcon.psd") as Texture2D),GUILayout.MaxHeight(18),GUILayout.Width(88))){
				Undo.RecordObject(actor, "Fix particles");
				for(int i = 0; i < selectionStatus.Length; i++){
					if (selectionStatus[i]){
						if (actor.invMasses[i] != 0){	
							SetPropertyValue(ParticleProperty.MASS,i,Mathf.Infinity);
							newProperty = GetPropertyValue(ParticleProperty.MASS,i);
							actor.velocities[i] = Vector3.zero;
						}
					}
				}
				actor.PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.INV_MASSES | ObiSolverData.ParticleData.VELOCITIES));
				EditorUtility.SetDirty(actor);
			}

			if (GUILayout.Button(new GUIContent("Unfix",EditorGUIUtility.Load("UnpinIcon.psd") as Texture2D),GUILayout.MaxHeight(18),GUILayout.Width(88))){
				Undo.RecordObject(actor, "Unfix particles");
				for(int i = 0; i < selectionStatus.Length; i++){
					if (selectionStatus[i]){
						if (actor.invMasses[i] == 0){	
							SetPropertyValue(ParticleProperty.MASS,i,1);
						}
					}
				}
				actor.PushDataToSolver(new ObiSolverData(ObiSolverData.ParticleData.INV_MASSES));
				EditorUtility.SetDirty(actor);
			}

			/*if (GUILayout.Button("CUT")){
				ObiCloth mesh = ((ObiCloth)actor);
				mesh.DistanceConstraints.RemoveFromSolver(null);
				mesh.AerodynamicConstraints.RemoveFromSolver(null);
				MeshBuffer buf = new MeshBuffer(mesh.clothMesh);

				int[] sel = new int[2];
				int k = 0;
				for(int i = 0; i < selectionStatus.Length; i++){
					if (selectionStatus[i]){
						sel[k] = i;
						k++;
						if (k == 2) break;
					}
				}

				int cindex = -1;
				for (int j = 0; j < mesh.DistanceConstraints.restLengths.Count; j++){
					if ((mesh.DistanceConstraints.springIndices[j*2] == sel[0] && mesh.DistanceConstraints.springIndices[j*2+1] == sel[1]) ||
					    (mesh.DistanceConstraints.springIndices[j*2] == sel[1] && mesh.DistanceConstraints.springIndices[j*2+1] == sel[0])){
						cindex = j;
						break;
					}
				}
				if (cindex >= 0)
					mesh.Tear(cindex,buf);
					
				
				mesh.DistanceConstraints.AddToSolver(mesh);
				mesh.AerodynamicConstraints.AddToSolver(mesh);
				buf.Apply();

				mesh.GetMeshDataArrays(mesh.clothMesh);
			}*/

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUI.enabled = selectedCount > 0;
			if (GUILayout.Button("Create Handle",GUILayout.Width(180))){

				// Create the handle:
				GameObject c = new GameObject("Obi Handle");
				Undo.RegisterCreatedObjectUndo(c,"Create Obi Particle Handle");
				ObiParticleHandle handle = c.AddComponent<ObiParticleHandle>();
				handle.Actor = actor;

				// Calculate position of handle from average of particle positions:
				Vector3 average = Vector3.zero;
				for(int i = 0; i < selectionStatus.Length; i++){
					if (selectionStatus[i]){
						average += wsPositions[i];
					}
				}

				c.transform.position = average / selectedCount;

				// Add the selected particles to the handle:
				for(int i = 0; i < selectionStatus.Length; i++){
					if (selectionStatus[i]){
						handle.AddParticle(i,wsPositions[i],actor.invMasses[i]);
					}
				}

			}
			GUI.enabled = true;
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();		
			
			EditorGUI.showMixedValue = false;
			for(int i = 0; i < selectionStatus.Length; i++){
				if (selectionStatus[i] && !Mathf.Approximately(GetPropertyValue(currentProperty,i), selectionProperty)){
					EditorGUI.showMixedValue = true;
				}	
			}
			
			newProperty = EditorGUILayout.FloatField(newProperty,GUILayout.Width(88));
			EditorGUI.showMixedValue = false;
			
			if (GUILayout.Button("Set "+GetPropertyName(),GUILayout.Width(88))){
				Undo.RecordObject(actor, "Set particle property");
				selectionProperty = newProperty;
				for(int i = 0; i < selectionStatus.Length; i++){
					if (selectionStatus[i]){
						SetPropertyValue(currentProperty,i,selectionProperty);
					}
				}
				ParticlePropertyChanged();
				EditorUtility.SetDirty(actor);
			}
			
			GUILayout.EndHorizontal();
			GUI.enabled = true;
		}
		
		void DrawPaintToolUI(){
			
			GUILayout.BeginHorizontal();
			if (GUILayout.Toggle(paintMode == PaintMode.PAINT,EditorGUIUtility.Load("Paint_brush_icon.psd") as Texture2D,GUI.skin.FindStyle("ButtonLeft"),GUILayout.MaxHeight(28)))
				paintMode = PaintMode.PAINT;
			if (GUILayout.Toggle(paintMode == PaintMode.SMOOTH,EditorGUIUtility.Load("Smooth_brush_icon.psd") as Texture2D,GUI.skin.FindStyle("ButtonRight"),GUILayout.MaxHeight(28)))
				paintMode = PaintMode.SMOOTH;
			GUILayout.EndHorizontal();
			
			selectionMask = GUILayout.Toggle(selectionMask,"Selection mask");
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Radius");
			brushRadius = EditorGUILayout.Slider(brushRadius,5,200);
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Opacity");
			brushOpacity = EditorGUILayout.Slider(brushOpacity*20,0,1)/20f;
			GUILayout.EndHorizontal();
			
			GUI.enabled = paintMode == PaintMode.PAINT;
			GUILayout.BeginHorizontal();
			GUILayout.Label("Min value");
			GUILayout.FlexibleSpace();
			minBrushValue = EditorGUILayout.FloatField(minBrushValue,GUILayout.Width(EditorGUIUtility.fieldWidth));
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Max value");
			GUILayout.FlexibleSpace();
			maxBrushValue = EditorGUILayout.FloatField(maxBrushValue,GUILayout.Width(EditorGUIUtility.fieldWidth));
			GUILayout.EndHorizontal();
			GUI.enabled = true;
			
		}

		protected virtual void DrawCustomUI(){
		}

		protected virtual string CustomUIName(){
			return null;
		}
		
		void OnPlayModeStateChanged()
		{
			//Prevent the user from going into play mode while we are doing stuff:
			if (routine != null && !routine.IsDone && EditorApplication.isPlayingOrWillChangePlaymode)
			{
				EditorApplication.isPlaying = false;
			}
		}
		
		void Update () {

			if (isPlaying && actor.InSolver){
				
				float deltaTime = Mathf.Min(Time.realtimeSinceStartup - lastFrameTime, Time.maximumDeltaTime);

				accumulatedTime += deltaTime;
				actor.Solver.AccumulateSimulationTime(deltaTime);

				while (accumulatedTime >= Time.fixedDeltaTime){
					actor.Solver.SimulateStep(Time.fixedDeltaTime);
					accumulatedTime -= Time.fixedDeltaTime;
				}

				actor.Solver.EndFrame(Time.fixedDeltaTime);

				lastFrameTime = Time.realtimeSinceStartup;
			}

		}


			
		[DrawGizmo (GizmoType.Selected | GizmoType.Active)]
		static void DrawGizmoForMyScript (ObiActor actor, GizmoType gizmoType) {
			
			if (!ObiParticleActorEditor.editMode)
				return;

			// Get the particle actor editor to retrieve selected particles:
			ObiParticleActorEditor[] editors = (ObiParticleActorEditor[])Resources.FindObjectsOfTypeAll(typeof(ObiParticleActorEditor));

			// If there's any particle actor editor active, we can show pin constraints:
			if (editors.Length >0)
 			{

				Gizmos.color = new Color(1,1,1,0.75f);

				for(int i = 0; i < actor.positions.Length; i++)
				{
					// skip particles not facing the camera, or inactive ones:
					if (!actor.active[i] || (!ObiParticleActorEditor.backfaces && !ObiParticleActorEditor.facingCamera[i])) continue;
	
					// draw particle radiuses if needed
					if (ObiParticleActorEditor.selectionStatus[i] && currentProperty == ParticleProperty.RADIUS){

						Gizmos.DrawSphere(wsPositions[i],actor.solidRadii[i]);	

					}
					
	                
	            }
			}
			
		}
	  
		
	}
}

