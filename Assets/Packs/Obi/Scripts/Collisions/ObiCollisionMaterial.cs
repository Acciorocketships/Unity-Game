using UnityEngine;
using System.Collections;

namespace Obi{

/**
 * Holds information about the physics properties of a particle or collider, and how it should react to collisions.
 */
public class ObiCollisionMaterial : ScriptableObject
{

	public float friction;
	public float stickiness;
	public float stickDistance;
	
	public Oni.MaterialCombineMode frictionCombine;
	public Oni.MaterialCombineMode stickinessCombine;

	public Oni.CollisionMaterial GetEquivalentOniMaterial()
	{
		Oni.CollisionMaterial material = new Oni.CollisionMaterial();
		material.friction = friction;
		material.stickiness = stickiness;
		material.stickDistance = stickDistance;
		material.frictionCombine = frictionCombine;
		material.stickinessCombine = stickinessCombine;
		return material;
	}
}
}

