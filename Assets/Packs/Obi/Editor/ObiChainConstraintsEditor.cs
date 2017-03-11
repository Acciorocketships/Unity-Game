using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiRopeConstraints component. 
	 */

	[CustomEditor(typeof(ObiChainConstraints)), CanEditMultipleObjects] 
	public class ObiChainConstraintsEditor : Editor
	{
	
		ObiChainConstraints constraints;
		
		public void OnEnable(){
			constraints = (ObiChainConstraints)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfDirtyOrScript();
			
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				
				serializedObject.ApplyModifiedProperties();
				
				constraints.PushDataToSolver(new ObiSolverData(ObiSolverData.ChainConstraintsData.ALL));
				
			}
			
		}
		
	}

}

