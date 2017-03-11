using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Obi{
	
	/**
	 * Custom inspector for ObiPinConstraints component. 
	 */
	
	[CustomEditor(typeof(ObiPinConstraints)), CanEditMultipleObjects] 
	public class ObiPinConstraintsEditor : Editor
	{
		
		ObiPinConstraints constraints;
		
		public void OnEnable(){
			constraints = (ObiPinConstraints)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfDirtyOrScript();
			
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");

			// Get the particle actor editor to retrieve selected particles:
			ObiParticleActorEditor[] editors = (ObiParticleActorEditor[])Resources.FindObjectsOfTypeAll(typeof(ObiParticleActorEditor));
 			
			// If there's any particle actor editor active, we can show pin constraints:
			if (editors.Length >0)
 			{
			
				List<int> selectedPins = new List<int>();
				List<int> removedPins = new List<int>();
					
				// Get the list of pin constraints from the selected particles:
				for (int i = 0; i < constraints.activeStatus.Count; i++){

					int particleIndex = constraints.pinParticleIndices[i];
					
					if (particleIndex >= 0 && particleIndex < ObiParticleActorEditor.selectionStatus.Length && 
						ObiParticleActorEditor.selectionStatus[particleIndex]){

						selectedPins.Add(i);

					}
				}

				if (selectedPins.Count > 0){
		
					//Iterate over all constraints:
					foreach (int i in selectedPins){
		
						GUILayout.BeginVertical("box");
								
						GUILayout.BeginHorizontal();
						
						EditorGUI.BeginChangeCheck();
						bool allowSceneObjects = !EditorUtility.IsPersistent(target);
						constraints.pinBodies[i] = EditorGUILayout.ObjectField("Pinned to:",constraints.pinBodies[i],typeof(Collider),allowSceneObjects) as Collider;
						
						// Calculate initial pin offset value after changing the rigidbody.
						if (EditorGUI.EndChangeCheck() && constraints.pinBodies[i] != null){
							constraints.pinOffsets[i] = constraints.pinBodies[i].transform.InverseTransformPoint(constraints.Actor.GetParticlePosition(constraints.pinParticleIndices[i]));
						}
						
						Color oldColor = GUI.color;
						GUI.color = Color.red;
						if (GUILayout.Button("X",GUILayout.Width(30))){
							// Mark this constraint to be removed outside of the loop.
							removedPins.Add(i);
							continue;
						}
						GUI.color = oldColor;
						
						GUILayout.EndHorizontal();
						
						constraints.pinOffsets[i] = EditorGUILayout.Vector3Field("Offset:",constraints.pinOffsets[i]);
						
						GUILayout.EndVertical();
		
					}

				}else{
					EditorGUILayout.HelpBox("No pin constraints for the selected particles.",MessageType.Info);
				}

				if (GUILayout.Button("Remove selected")){

					for (int i = 0; i < constraints.activeStatus.Count; i++){
						int particleIndex = constraints.pinParticleIndices[i];
					
						if (particleIndex >= 0 && particleIndex < ObiParticleActorEditor.selectionStatus.Length && 
							ObiParticleActorEditor.selectionStatus[particleIndex]){
	
							removedPins.Add(i);
	
						}
					}		
				}

				if (GUILayout.Button("Add Pin Constraint")){

					Undo.RecordObject(constraints, "Add pin constraints");
	
					bool wasInSolver = constraints.InSolver;
					constraints.RemoveFromSolver(null);
		
					for(int i = 0; i < ObiParticleActorEditor.selectionStatus.Length; i++){
						if (ObiParticleActorEditor.selectionStatus[i]){
							constraints.AddConstraint(true,i,null,Vector3.zero,1);
						}
					}

					if (wasInSolver) 
						constraints.AddToSolver(null);

					EditorUtility.SetDirty(constraints);
				}
				
				// Remove selected constraint outside of constraint listing loop:
				if (removedPins.Count > 0){

					Undo.RecordObject(constraints, "Remove pin constraints");

					bool wasInSolver = constraints.InSolver;
					constraints.RemoveFromSolver(null);

					// Remove from last to first, to avoid throwing off subsequent indices:
					foreach(int i in removedPins.OrderByDescending(i => i)){
						constraints.RemoveConstraint(i);
					}

					if (wasInSolver) 
						constraints.AddToSolver(null);

					EditorUtility.SetDirty(constraints);

				}

			}
		
			// Apply changes to the serializedProperty
			if (GUI.changed){
				
				serializedObject.ApplyModifiedProperties();
				
				constraints.PushDataToSolver(new ObiSolverData(ObiSolverData.PinConstraintsData.ALL));
				
			}
			
		}

		/**
		 * Draws selected pin constraints in the scene view.
		 */
		public void OnSceneGUI(){

			if (Event.current.type != EventType.Repaint || !ObiParticleActorEditor.editMode) return;
					
			// Get the particle actor editor to retrieve selected particles:
			ObiParticleActorEditor[] editors = (ObiParticleActorEditor[])Resources.FindObjectsOfTypeAll(typeof(ObiParticleActorEditor));
 			
			// If there's any particle actor editor active, we can show pin constraints:
			if (editors.Length >0)
 			{

				Handles.color = Color.cyan;
		
				// Get the list of pin constraints from the selected particles:
				for (int i = 0; i < constraints.activeStatus.Count; i++){

					if (!constraints.activeStatus[i]) continue;

					int particleIndex = constraints.pinParticleIndices[i];
					
					if (particleIndex >= 0 && particleIndex < ObiParticleActorEditor.selectionStatus.Length && 
						ObiParticleActorEditor.selectionStatus[particleIndex]){

						if (constraints.pinBodies[i] != null){
							
							Vector3 pinPosition = constraints.pinBodies[i].transform.TransformPoint(constraints.pinOffsets[i]);
							Handles.DrawDottedLine(constraints.Actor.GetParticlePosition(constraints.pinParticleIndices[i]),pinPosition,5);
							Handles.SphereCap(0,pinPosition,Quaternion.identity,HandleUtility.GetHandleSize(pinPosition)*0.1f);

						}

					}
				}
			}	
		}
		
	}
}

