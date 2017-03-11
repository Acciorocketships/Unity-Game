using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiTetherConstraints component. 
	 */
	
	[CustomEditor(typeof(ObiTetherConstraints)), CanEditMultipleObjects] 
	public class ObiTetherConstraintsEditor : Editor
	{
		
		ObiTetherConstraints constraints;
		
		public void OnEnable(){
			constraints = (ObiTetherConstraints)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfDirtyOrScript();
			
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			GUI.enabled = (constraints.Actor != null && constraints.Actor.Initialized);
			
			if (GUILayout.Button("Generate Tether Constraints")){

				if (constraints.Actor != null){

					Undo.RegisterCompleteObjectUndo(constraints, "Generate tethers");

					constraints.RemoveFromSolver(null);
					if (!constraints.Actor.GenerateTethers(constraints.Actor.MaxTethers)){
						Debug.LogWarning("Could not generate tethers. Make sure the actor has been properly initialized.");
					}
					constraints.AddToSolver(null);

					EditorUtility.SetDirty(constraints);
				}
			}
			
			GUI.enabled = true;
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				
				serializedObject.ApplyModifiedProperties();
				
				constraints.PushDataToSolver(new ObiSolverData(ObiSolverData.TetherConstraintsData.ALL));
				
			}
			
		}
		
	}
}

