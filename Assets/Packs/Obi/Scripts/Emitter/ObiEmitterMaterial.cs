using UnityEngine;
using System.Collections;

namespace Obi{

/**
 * Holds information about the physical properties of the substance emitted by an emitter.
 */
public class ObiEmitterMaterial : ScriptableObject
{

	// fluid parameters:
	public bool isFluid = true;	/**< do the emitter particles generate density constraints?*/

	public float smoothingRadius = 0.2f;
	public float relaxationFactor = 600;	/**< how stiff the density corrections are.*/
	public float restRadius = 0.1f;
	public float restDensity = 1000;		/**< rest density of the fluid particles.*/
	public float viscosity = 0.01f;			/**< viscosity of the fluid particles.*/
	public float cohesion = 0.1f;
	public float surfaceTension = 0.1f;	/**< surface tension of the fluid particles.*/

	// gas parameters:
	public float buoyancy = -1.0f; 						/**< how dense is this material with respect to air?*/
	public float atmosphericDrag = 0;					/**< amount of drag applied by the surrounding air to particles near the surface of the material.*/
	public float atmosphericPressure = 0;				/**< amount of pressure applied by the surrounding air particles.*/
	public float vorticity = 0.0f;						/**< amount of baroclinic vorticity injected.*/
	
	// elastoplastic parameters:
	public float elasticRange; 		/** radius around a particle in which distance constraints are created.*/
	public float plasticCreep;		/**< rate at which a deformed plastic material regains its shape*/
	public float plasticThreshold;	/**< amount of stretching stress that a elastic material must undergo to become plastic.*/

	public Oni.FluidMaterial GetEquivalentOniMaterial()
	{
		Oni.FluidMaterial material = new Oni.FluidMaterial();
		material.smoothingRadius = smoothingRadius;
		material.relaxationFactor = relaxationFactor;
		material.restDensity = restDensity;
		material.viscosity = viscosity;
		material.cohesion = cohesion;
		material.surfaceTension = surfaceTension;
		material.buoyancy = buoyancy;
		material.atmosphericDrag = atmosphericDrag;
		material.atmosphericPressure = atmosphericPressure;
		material.vorticity = vorticity;
		return material;
	}
}
}

