using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AI
{
    public class NavMesh : MonoBehaviour
    {
        [SerializeField] private ComputeShader _eraseUnnecessaryTriangles;
        [SerializeField] private ComputeShader _splitFloorCeilVoxel;
        
        [SerializeField] private NavMeshAgentSpecs navMeshAgentSpecs;

        [SerializeField] private LayerMask _walkableLayers;

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private Renderer _meshRenderer;

        [SerializeField] private bool _overrideVoxelSize;
        [ConditionalReadOnly("_overrideVoxelSize")]
        [Min(0.1f)]
        [SerializeField] private float _voxelSize;

        private VoxelGrid _voxelGrid;

        private void OnValidate()
        {
            if (navMeshAgentSpecs == null)
            {
                return;
            }

            if (_overrideVoxelSize)
            {
                return;
            }
            
            _voxelSize = navMeshAgentSpecs.radius / 3;
        }

        public void BakeNavMesh()
        {
            /*Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();*/
            
            CreateVoxelGrid();
            
            _voxelGrid.FillWithVoxels();

            Mesh mesh = _meshFilter.sharedMesh;
            
            //_voxelGrid.EraseUnnecessaryTriangles(_eraseUnnecessaryTriangles, mesh.vertices, mesh.normals, mesh.triangles);
            
            //_voxelGrid.IterateOverMeshTriangles(_meshFilter.gameObject.transform, mesh.vertices, mesh.triangles);
            
            _voxelGrid.ModifyVoxelProperties(_walkableLayers, navMeshAgentSpecs);
            
            Debug.Log("Baked");
            /*stopWatch.Stop();
            float elapsedTime = stopWatch.ElapsedMilliseconds;
            Debug.Log(elapsedTime);*/
        }

        private void CreateVoxelGrid()
        {
            Bounds bounds = _meshRenderer.bounds;
            
            Vector3 areaSize = bounds.size * 1.1f;

            Vector3 pivot = bounds.center - (areaSize / 2);
            
            _voxelGrid = new VoxelGrid(pivot, areaSize, _voxelSize);
        }

        public void ClearNavMesh()
        {
            _voxelGrid = null;
        }

        private Vector3 Multiply2VectorsComponents(Vector3 first, Vector3 second)
        {
            return new Vector3(first.x * second.x, first.y * second.y, first.z * second.z);
        }

        private void OnDrawGizmosSelected()
        {
            Bounds bounds = _meshRenderer.bounds;
            
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            Gizmos.color = Color.green;

            List<Vector3> vertices = new List<Vector3>();

            foreach (var vertex in _meshFilter.sharedMesh.vertices)
            {
                vertices.Add(Multiply2VectorsComponents(vertex, _meshFilter.gameObject.transform.lossyScale) + bounds.center);
            }

            List<Vector3> normals = new List<Vector3>();
            
            foreach (var normal in _meshFilter.sharedMesh.normals)
            {
                normals.Add(normal);
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                Gizmos.DrawRay(vertices[i], normals[i] * 3);
            }

            if (_voxelGrid == null)
            {
                return; 
            }

            Gizmos.color = Color.white;

            foreach (Vector3 voxelPosition in _voxelGrid.GetWalkableVoxelPositions())
            {
                Gizmos.DrawLine(voxelPosition, voxelPosition + Vector3.up);
            }

            Gizmos.color = Color.red;

            foreach (Vector3 voxelPosition in _voxelGrid.GetCeilVoxelPositions())
            {
                Gizmos.DrawLine(voxelPosition, voxelPosition + Vector3.down);
            }
        }
        
        internal Quaternion Rotate(Quaternion currentRotation, Vector3 axis, float angle)
        {

            angle /= 2;
            
            //todo: change this so it takes currentRotation, and calculate a new quaternion rotated by an angle "angle" radians along the normalized axis "axis"

            Quaternion quaternionRotationZ = new Quaternion();
            quaternionRotationZ.w = (float)Math.Cos(Deg2Rad(angle) * axis.z);
            quaternionRotationZ.z = (float)Math.Sin(Deg2Rad(angle) * axis.z);
            
            Quaternion quaternionRotationY = new Quaternion();
            quaternionRotationY.w = (float)Math.Cos(Deg2Rad(angle) * axis.y);
            quaternionRotationY.y = (float)Math.Sin(Deg2Rad(angle) * axis.y);
            
            Quaternion quaternionRotationX = new Quaternion();
            quaternionRotationX.w = (float)Math.Cos(Deg2Rad(angle) * axis.x);
            quaternionRotationX.x = (float)Math.Sin(Deg2Rad(angle) * axis.x);

            Quaternion result = quaternionRotationZ * quaternionRotationX * quaternionRotationY;
            
            return currentRotation * result;
        }

        private float Deg2Rad(float angle)
        {
            return angle * ((float)Math.PI / 180f);
        }
    }
}