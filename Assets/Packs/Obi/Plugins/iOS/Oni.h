/*
 *  Oni.h
 *  Oni
 *
 *  Created by José María Méndez González on 21/9/15.
 *  Copyright (c) 2015 ArK. All rights reserved.
 *
 */

#ifndef Oni_
#define Oni_

#include "Solver.h"
#include "HalfEdgeMesh.h"

#if defined(__APPLE__)
    #define EXPORT __attribute__((visibility("default")))
#else
    #define EXPORT __declspec(dllexport)
#endif

namespace Oni
{
    
    struct ConstraintGroupParameters;
    class Collider;
    class Rigidbody;
    struct SphereShape;
    struct BoxShape;
    struct CapsuleShape;
    struct HeightmapShape;
    struct TriangleMeshShape;
    struct CollisionMaterial;

    extern "C"
    {
        
        // Collider Group ********************:
        
		EXPORT ColliderGroup* CreateColliderGroup();
		EXPORT void DestroyColliderGroup(ColliderGroup* group);
        
		EXPORT void SetColliders(ColliderGroup* group, const Collider* colliders, int num, int dest_offset);
        EXPORT int RemoveColliders(ColliderGroup* group, int num, int source_offset);
        EXPORT int GetColliderCount(ColliderGroup* group);
        
		EXPORT void SetRigidbodies(ColliderGroup* group, const Rigidbody* rigidbodies, int num, int dest_offset);
        EXPORT int GetRigidbodies(ColliderGroup* group, Rigidbody* rigidbodies, int num, int dest_offset);
        EXPORT int RemoveRigidbodies(ColliderGroup* group, int num, int source_offset);
        EXPORT int GetRigidbodyCount(ColliderGroup* group);
        
        EXPORT int GetShapeCount(ColliderGroup* group,ShapeType shape);
        
		EXPORT void SetSphereShapes(ColliderGroup* group, const SphereShape* shapes, int num, int dest_offset);
        EXPORT int RemoveSphereShapes(ColliderGroup* group, int num, int source_offset);
        
		EXPORT void SetBoxShapes(ColliderGroup* group, const BoxShape* shapes, int num, int dest_offset);
        EXPORT int RemoveBoxShapes(ColliderGroup* group, int num, int source_offset);
        
		EXPORT void SetCapsuleShapes(ColliderGroup* group, const CapsuleShape* shapes, int num, int dest_offset);
        EXPORT int RemoveCapsuleShapes(ColliderGroup* group, int num, int source_offset);
        
		EXPORT void SetHeightmapShapes(ColliderGroup* group, const HeightmapShape* shapes, int num, int dest_offset);
        EXPORT int RemoveHeightmapShapes(ColliderGroup* group, int num, int source_offset);
        
        EXPORT void SetTriangleMeshShapes(ColliderGroup* group, const TriangleMeshShape* shapes, int num, int dest_offset);
        EXPORT int RemoveTriangleMeshShapes(ColliderGroup* group, int num, int source_offset);
        EXPORT int UpdateTriangleMeshShapes(ColliderGroup* group, int num, int source_offset);
        
        EXPORT void SetEdgeMeshShapes(ColliderGroup* group, const EdgeMeshShape* shapes, int num, int dest_offset);
        EXPORT int RemoveEdgeMeshShapes(ColliderGroup* group, int num, int source_offset);
        EXPORT int UpdateEdgeMeshShapes(ColliderGroup* group, int num, int source_offset);
        
        // Solver ********************:
        
		EXPORT Solver* CreateSolver(int max_particles, int max_diffuse_particles, int max_neighbours);
		EXPORT void DestroySolver(Solver* solver);
        
		EXPORT void GetBounds(Solver* solver, Eigen::Vector3f& min, Eigen::Vector3f& max);
        
		EXPORT void SetSolverParameters(Solver* solver, const SolverParameters* parameters);
		EXPORT void GetSolverParameters(Solver* solver, SolverParameters* parameters);
        
		EXPORT void AddSimulationTime(Solver* solver, const float step_seconds);
		EXPORT void UpdateSolver(Solver* solver, const float substep_seconds);
		EXPORT void ApplyPositionInterpolation(Solver* solver,const float substep_seconds);
        
		EXPORT void SetConstraintsOrder(Solver* solver, const int* order);
		EXPORT void GetConstraintsOrder(Solver* solver, int* order);
		EXPORT int GetConstraintCount(Solver* solver, const Solver::ConstraintType type);
        EXPORT void GetActiveConstraintIndices(Solver* solver, int* indices, int num, const Solver::ConstraintType type);
        
		EXPORT int SetActiveParticles(Solver* solver, const int* active, int num);
        
		EXPORT int SetParticlePhases(Solver* solver,const int* phases, int num, int dest_offset);
        
		EXPORT int SetParticlePositions(Solver* solver,const float* positions, int num, int dest_offset);
        
        EXPORT int GetParticlePositions(Solver* solver, float* positions, int num, int source_offset);
        
		EXPORT int SetRenderableParticlePositions(Solver* solver,const float* positions, int num, int dest_offset);
        
        EXPORT int GetRenderableParticlePositions(Solver* solver, float* positions, int num, int source_offset);
        
		EXPORT int SetParticleInverseMasses(Solver* solver, const float* inv_masses,int num, int dest_offset);
        
		EXPORT int SetParticleSolidRadii(Solver* solver, const float* radii,int num, int dest_offset);
        
		EXPORT int SetParticleVelocities(Solver* solver,const float* velocities, int num, int dest_offset);
        
        EXPORT int GetParticleVelocities(Solver* solver, float* velocities, int num, int source_offset);
    
        EXPORT int SetParticleVorticities(Solver* solver, const float* vorticities, int num, int dest_offset);
        
        EXPORT int GetParticleVorticities(Solver* solver, float* vorticities, int num, int source_offset);
        
		EXPORT void SetConstraintGroupParameters(Solver* solver, const Solver::ConstraintType type, const ConstraintGroupParameters* parameters);
        
		EXPORT void GetConstraintGroupParameters(Solver* solver, const Solver::ConstraintType type, ConstraintGroupParameters* parameters);
        
		EXPORT void SetColliderGroup(Solver* solver, ColliderGroup* group);
        
		EXPORT void SetCollisionMaterials(Solver* solver, const CollisionMaterial* materials, int num, int dest_offset);
        
		EXPORT int SetMaterialIndices(Solver* solver, const int* indices, int num, int dest_offset);
        
        EXPORT void SetIgnoredParticles(Solver* solver, const int* ignored_particle_collisions, int num, int particle);
        
		EXPORT void SetFluidMaterials(Solver* solver, FluidMaterial* materials, int num, int dest_offset);
        
		EXPORT int SetFluidMaterialIndices(Solver* solver, const int* indices, int num, int dest_offset);
        
        // Constraints ********************:
        
        EXPORT void ActivateConstraints(Solver* solver, const Solver::ConstraintType type, const int* active, int num);
        
        EXPORT void DeactivateConstraints(Solver* solver, const Solver::ConstraintType type, const int* inactive, int num);
        
        EXPORT int RemoveConstraints(Solver* solver, const Solver::ConstraintType type, int num, int source_offset);
        
		EXPORT void SetDistanceConstraints(Solver* solver,
                                     const int* indices,
                                     const float* restLengths,
                                     const float* stiffnesses,
                                     int num,
                                     int dest_offset);
        
        EXPORT int GetDistanceConstraintsStretching(Solver* solver,
                                                    float* stretching,
                                                    int num,
                                                    int source_offset);
        
		EXPORT void SetBendingConstraints(Solver* solver,const int* indices,
                                    const float* rest_bends,
                                    const float* bending_stiffnesses,
                                    int num,
                                    int dest_offset);
        
		EXPORT void SetSkinConstraints(Solver* solver,
                                 const int* indices,
                                 const Vector4f* skin_points,
                                 const Vector4f* skin_normals,
                                 const float* radii_backstops,
                                 const float* stiffnesses,
                                 int num,
                                 int dest_offset);
        
		EXPORT void SetAerodynamicConstraints(Solver* solver,
                                       const int* triangle_indices,
                                       const Vector4f* triangle_normals,
                                       const Vector4f* wind,
                                       const float* aerodynamic_coeffs,
                                       int num,
                                       int dest_offset);
        
        EXPORT int UpdateAerodynamicNormals(Solver* solver,
                                            const Vector4f* triangle_normals,
                                            int num,
                                            int dest_offset);
        
		EXPORT  void SetVolumeConstraints(Solver* solver,
                                   const int* triangle_indices,
                                   const int* first_triangle,
                                   const int* num_triangles,
                                   const float* rest_volumes,
                                   const float* pressure_stiffnesses,
                                   int num,
                                   int dest_offset);
        
        EXPORT int GetVolumeTriangleCount(Solver* solver);
        
        EXPORT void SetChainConstraints(Solver* solver,
                                       const int* indices,
                                       const int* first_index,
                                       const int* num_indices,
                                       const float* lenghts,
                                       int num,
                                       int dest_offset);
        
        EXPORT int GetChainParticleCount(Solver* solver);
        
		EXPORT void SetTetherConstraints(Solver* solver,
                                   const int* indices,
                                   const float* max_lenght_scales,
                                   const float* stiffnesses,
                                   int num,
                                   int dest_offset);
        
		EXPORT void SetPinConstraints(Solver* solver,
                                const int* indices,
                                const Vector4f* pin_offsets,
                                const float* stiffnesses,
                                int num,
                                int dest_offset);
        
        // Collision data ********************:
        
		EXPORT void GetCollisionIndices(Solver* solver,int* indices, int num);
		EXPORT void GetCollisionDistances(Solver* solver,float* collision_distances, int num);
		EXPORT void GetCollisionPoints(Solver* solver,Vector4f* collision_points, int num);
		EXPORT void GetCollisionNormals(Solver* solver,Vector4f* collision_normals, int num);
		EXPORT void GetCollisionNormalImpulses(Solver* solver,float* normal_impulses, int num);
		EXPORT void GetCollisionTangentImpulses(Solver* solver,float* tangent_impulses, int num);
		EXPORT void GetCollisionStickImpulses(Solver* solver,float* stick_impulses, int num);
        
        // Diffuse particles ********************:
        
        EXPORT int SetActiveDiffuseParticles(Solver* solver, const int* active, int num);
        
        EXPORT int SetDiffuseParticlePositions(Solver* solver, const float* positions, int num, int dest_offset);
        
        EXPORT int SetDiffuseParticleVelocities(Solver* solver, const float* velocities, int num, int dest_offset);
        
        EXPORT int GetDiffuseParticleVelocities(Solver* solver, float* velocities, int num, int source_offset);
        
		EXPORT void SetDiffuseParticleNeighbourCounts(Solver* solver,int* neighbour_counts);

    }
    
}

#endif
