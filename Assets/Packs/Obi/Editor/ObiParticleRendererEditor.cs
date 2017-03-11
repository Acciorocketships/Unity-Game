using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiParticleRenderer component. 
	 */

	[CustomEditor(typeof(ObiParticleRenderer)), CanEditMultipleObjects] 
	public class ObiParticleHandleEditor : Editor
	{

		[MenuItem("Component/Physics/Obi/Obi Particle Renderer",false,0)]
		static void AddObiParticleRenderer()
		{
			foreach(Transform t in Selection.transforms)
				Undo.AddComponent<ObiParticleRenderer>(t.gameObject);
		}

		[MenuItem("GameObject/3D Object/Obi/Utils/Obi Particle Renderer",false)]
		static void CreateObiParticleRenderer()
		{
			GameObject c = new GameObject("Obi Particle Renderer");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Particle Renderer");
			c.AddComponent<ObiParticleRenderer>();
		}
	
		ObiParticleRenderer renderer;
		
		public void OnEnable(){
			renderer = (ObiParticleRenderer)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfDirtyOrScript();

			renderer.Actor = EditorGUILayout.ObjectField("Actor",renderer.Actor, typeof(ObiActor), true) as ObiActor;
			
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				
				serializedObject.ApplyModifiedProperties();
				
			}
			
		}
		
	}

}

