/*
 *  OniHelpers.h
 *  Oni
 *
 *  Created by José María Méndez González on 21/9/15.
 *  Copyright (c) 2015 ArK. All rights reserved.
 *
 */

#ifndef OniHelpers_
#define OniHelpers_

#include "Dense.h"
#include "HalfEdgeMesh.h"

#if defined(__APPLE__)
    #define EXPORT __attribute__((visibility("default")))
#else
    #define EXPORT __declspec(dllexport)
#endif

namespace Oni
{
    extern "C"
    {
        
		EXPORT int MakePhase(int group, int flags);
        
        /**
         * Calculates the rest bend factor for a bending constraint between 3 particles.
         * @param coordinates an array of 9 floats: x,y,z of the first particle, x,y,z of the second particle, x,y,z of the third (central) particle.
         */
		EXPORT float BendingConstraintRest(float* coordinates);
        
		EXPORT HalfEdgeMesh* CreateHalfEdgeMesh();
        
		EXPORT void DestroyHalfEdgeMesh(HalfEdgeMesh* mesh);
		EXPORT void GetHalfEdgeMeshInfo(HalfEdgeMesh* mesh, HalfEdgeMesh::MeshInformation* mesh_info);
		EXPORT void Generate(HalfEdgeMesh* mesh,Eigen::Vector3f* vertices, int* triangles, int vertex_count, int triangle_count, float* scale);
        
		EXPORT void SetHalfEdges(HalfEdgeMesh* mesh,HalfEdgeMesh::HalfEdge* half_edges, int count);
		EXPORT void SetVertices(HalfEdgeMesh* mesh,HalfEdgeMesh::Vertex* vertices, int count);
		EXPORT void SetFaces(HalfEdgeMesh* mesh,HalfEdgeMesh::Face* faces, int count);
        
		EXPORT void GetHalfEdges(HalfEdgeMesh* mesh,HalfEdgeMesh::HalfEdge* half_edges);
		EXPORT void GetVertices(HalfEdgeMesh* mesh,HalfEdgeMesh::Vertex* vertices);
		EXPORT void GetFaces(HalfEdgeMesh* mesh,HalfEdgeMesh::Face* faces);
        
		EXPORT int GetHalfEdgeCount(HalfEdgeMesh* mesh);
		EXPORT int GetVertexCount(HalfEdgeMesh* mesh);
		EXPORT int GetFaceCount(HalfEdgeMesh* mesh);
        
		EXPORT void AreaWeightedNormals(HalfEdgeMesh* mesh,Eigen::Vector3f* mesh_vertices, Eigen::Vector3f* mesh_normals);
		EXPORT void VertexOrientations(HalfEdgeMesh* mesh, Eigen::Vector3f* mesh_vertices, Eigen::Vector3f* mesh_normals, Eigen::Quaternionf* vertex_orientations);
    }
    
}

#endif
