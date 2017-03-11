using UnityEngine;
using System;
using System.Collections;

namespace Obi{
	public class ObiSolverData{
		
		[Flags]
		public enum ParticleData{
			NONE = 0,
			ACTIVE_STATUS = 1 << 0,
			ACTOR_ID = 1 << 1,
			POSITIONS = 1 << 2,
			VELOCITIES = 1 << 3,
			INV_MASSES = 1 << 4,
			VORTICITIES = 1 << 5,
			SOLID_RADII = 1 << 6,
			PHASES = 1 << 7,
			ALL = ~0
		}
		
		[Flags]
		public enum DistanceConstraintsData{
			NONE = 0,
			ACTIVE_STATUS = 1 << 0,
			DISTANCE_INDICES = 1 << 1,
			DISTANCE_REST_LENGHTS = 1 << 2,
			DISTANCE_STIFFNESSES = 1 << 3,
			DISTANCE_STRETCH = 1 << 4,
			ALL = ~0
		}
		
		[Flags]
		public enum SkinConstraintsData{
			NONE = 0,
			ACTIVE_STATUS = 1 << 0,
			SKIN_INDICES = 1 << 1,
			SKIN_POINTS = 1 << 2,
			SKIN_NORMALS = 1 << 3,
			SKIN_RADII_BACKSTOP = 1 << 4,
			SKIN_STIFFNESSES = 1 << 5,
			ALL = ~0
		}
		
		[Flags]
		public enum AerodynamicConstraintsData{
			NONE = 0,
			ACTIVE_STATUS = 1 << 0,
			AERODYNAMIC_INDICES = 1 << 1,
			AERODYNAMIC_NORMALS = 1 << 2,
			WIND = 1 << 3,
			AERODYNAMIC_COEFFS = 1 << 4,
			ALL = ~0
		}
		
		[Flags]
		public enum VolumeConstraintsData{
			NONE = 0,
			ACTIVE_STATUS = 1 << 0,
			VOLUME_INDICES = 1 << 1,
			VOLUME_FIRST_TRIANGLES = 1 << 2,
			VOLUME_NUM_TRIANGLES = 1 << 3,
			VOLUME_REST_VOLUMES = 1 << 4,
			VOLUME_PRESSURE_STIFFNESSES = 1 << 5,
			ALL = ~0
		}

		[Flags]
		public enum ChainConstraintsData{
			NONE = 0,
			ACTIVE_STATUS = 1 << 0,
			CHAIN_INDICES = 1 << 1,
			CHAIN_FIRST_INDEX = 1 << 2,
			CHAIN_NUM_INDICES = 1 << 3,
			CHAIN_LENGTHS = 1 << 4,
			ALL = ~0
		}
		
		[Flags]
		public enum BendingConstraintsData{
			NONE = 0,
			ACTIVE_STATUS = 1 << 0,
			BENDING_INDICES = 1 << 1,
			BENDING_STIFFNESSES = 1 << 2,
			ALL = ~0
		}
		
		[Flags]
		public enum TetherConstraintsData{
			NONE = 0,
			ACTIVE_STATUS = 1 << 0,
			TETHER_INDICES = 1 << 1,
			TETHER_MAX_LENGHTS_SCALES = 1 << 2,
			TETHER_STIFFNESSES = 1 << 3,
			ALL = ~0
		}

		[Flags]
		public enum PinConstraintsData{
			NONE = 0,
			ACTIVE_STATUS = 1 << 0,
			PIN_INDICES = 1 << 1,
			PIN_OFFSETS = 1 << 2,
			PIN_STIFFNESSES = 1 << 3,
			ALL = ~0
		}
		
		public ParticleData particleData = ParticleData.NONE;
		public DistanceConstraintsData distanceConstraintsData = DistanceConstraintsData.NONE;
		public SkinConstraintsData skinConstraintsData = SkinConstraintsData.NONE;
		public AerodynamicConstraintsData aerodynamicConstraintsData = AerodynamicConstraintsData.NONE;
		public VolumeConstraintsData volumeConstraintsData = VolumeConstraintsData.NONE;
		public BendingConstraintsData bendingConstraintsData = BendingConstraintsData.NONE;
		public TetherConstraintsData tetherConstraintsData = TetherConstraintsData.NONE;
		public PinConstraintsData pinConstraintsData = PinConstraintsData.NONE;
		public ChainConstraintsData chainConstraintsData = ChainConstraintsData.NONE;
	
		public ObiSolverData(ParticleData particleData){
			this.particleData = particleData;
		}
		public ObiSolverData(DistanceConstraintsData distanceConstraintsData){
			this.distanceConstraintsData = distanceConstraintsData;
        }
		public ObiSolverData(SkinConstraintsData skinConstraintsData){
			this.skinConstraintsData = skinConstraintsData;
        }
		public ObiSolverData(AerodynamicConstraintsData aerodynamicConstraintsData){
			this.aerodynamicConstraintsData = aerodynamicConstraintsData;
        }
		public ObiSolverData(VolumeConstraintsData volumeConstraintsData){
			this.volumeConstraintsData = volumeConstraintsData;
        }
		public ObiSolverData(BendingConstraintsData bendingConstraintsData){
			this.bendingConstraintsData = bendingConstraintsData;
		}
		public ObiSolverData(TetherConstraintsData tetherConstraintsData){
			this.tetherConstraintsData = tetherConstraintsData;
		}
		public ObiSolverData(PinConstraintsData pinConstraintsData){
			this.pinConstraintsData = pinConstraintsData;
		}
		public ObiSolverData(ChainConstraintsData chainConstraintsData){
			this.chainConstraintsData = chainConstraintsData;
		}
    }
}

