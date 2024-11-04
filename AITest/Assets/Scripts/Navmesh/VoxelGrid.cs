using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    public class VoxelGrid
    {
        private Voxel[] _voxels;
        private Voxel[] _floorVoxels;
        private Voxel[] _ceilVoxels;

        private List<Vector3> _floorAuxVoxels;
        private List<Vector3> _ceilAuxVoxels;
        
        private Vector3Int _gridDimensions;

        private Vector3 _pivot;
        
        private float _voxelSize;

        public VoxelGrid(Vector3 pivot, Vector3 areaSize, float voxelSize)
        {
            _voxelSize = voxelSize;
            _gridDimensions.x = Mathf.CeilToInt(areaSize.x / this._voxelSize);
            _gridDimensions.y = Mathf.CeilToInt(areaSize.y / this._voxelSize);
            _gridDimensions.z = Mathf.CeilToInt(areaSize.z / this._voxelSize);

            _voxels = new Voxel[_gridDimensions.x * _gridDimensions.y * _gridDimensions.z];

            _pivot = pivot;
        }

        public void FillWithVoxels()
        {
            for (int x = 0; x < _gridDimensions.x; x++)
            {
                for (int y = 0; y < _gridDimensions.y; y++)
                {
                    for (int z = 0; z < _gridDimensions.z; z++)
                    {
                        Vector3 position = new Vector3(x * _voxelSize, y * _voxelSize, z * _voxelSize) + _pivot;
                        _voxels[x * _gridDimensions.y * _gridDimensions.z + y * _gridDimensions.z + z] = new Voxel(position);
                    }
                }
            }
        }

        public void EraseUnnecessaryTriangles(ComputeShader eraseUnnecessaryTriangles, Vector3[] vertices, Vector3[] verticesNormals, int[] triangles)
        {
            Debug.Log(vertices.Length);
            Debug.Log(verticesNormals.Length);
            Debug.Log(triangles.Length);

            ComputeBuffer verticesBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
            verticesBuffer.SetData(vertices);

            ComputeBuffer verticesNormalsBuffer = new ComputeBuffer(verticesNormals.Length, sizeof(float) * 3);
            verticesNormalsBuffer.SetData(verticesNormals);

            ComputeBuffer trianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
            trianglesBuffer.SetData(triangles);
            
            eraseUnnecessaryTriangles.SetBuffer(0, "_Vertices", verticesBuffer);
            eraseUnnecessaryTriangles.SetBuffer(0, "_VerticesNormals", verticesNormalsBuffer);
            eraseUnnecessaryTriangles.SetBuffer(0, "_Triangles", trianglesBuffer);

            eraseUnnecessaryTriangles.Dispatch(0,
                Mathf.CeilToInt((float)_gridDimensions.x / 8), 
                Mathf.CeilToInt((float)_gridDimensions.y / 8), 
                Mathf.CeilToInt((float)_gridDimensions.z / 8));
            
            verticesBuffer.Release();
            verticesNormalsBuffer.Release();
            trianglesBuffer.Release();
            
            SplitFloorCeilVoxels(eraseUnnecessaryTriangles, vertices, verticesNormals, triangles);
        }

        public void SplitFloorCeilVoxels(ComputeShader splitFloorCeilVoxel, Vector3[] vertices, Vector3[] verticesNormals, int[] triangles)
        {
            ComputeBuffer voxelsBuffer = new ComputeBuffer(_voxels.Length, sizeof(float) * 3 + sizeof(int) * 2);
            voxelsBuffer.SetData(_voxels);
            
            ComputeBuffer floorVoxelsBuffer = new ComputeBuffer(_voxels.Length, sizeof(float) * 3 + sizeof(int) * 2);
            ComputeBuffer ceilVoxelsBuffer = new ComputeBuffer(_voxels.Length, sizeof(float) * 3 + sizeof(int) * 2);

            ComputeBuffer verticesBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
            verticesBuffer.SetData(vertices);

            ComputeBuffer verticesNormalsBuffer = new ComputeBuffer(verticesNormals.Length, sizeof(float) * 3);
            verticesNormalsBuffer.SetData(verticesNormals);

            ComputeBuffer trianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(int));
            trianglesBuffer.SetData(triangles);
            
            splitFloorCeilVoxel.SetBuffer(0, "_Voxels", voxelsBuffer);
            splitFloorCeilVoxel.SetBuffer(0, "_FloorVoxels", floorVoxelsBuffer);
            splitFloorCeilVoxel.SetBuffer(0, "_CeilVoxels", ceilVoxelsBuffer);
            splitFloorCeilVoxel.SetBuffer(0, "_Vertices", verticesBuffer);
            splitFloorCeilVoxel.SetBuffer(0, "_VerticesNormals", verticesNormalsBuffer);
            splitFloorCeilVoxel.SetBuffer(0, "_Triangles", trianglesBuffer);
            splitFloorCeilVoxel.SetFloat("_VoxelSize", _voxelSize);
            splitFloorCeilVoxel.SetVector("_Pivot", _pivot);
            splitFloorCeilVoxel.SetVector("_GridDimensions", (Vector3)_gridDimensions / 8);

            splitFloorCeilVoxel.Dispatch(0,
                Mathf.CeilToInt((float)_gridDimensions.x / 8), 
                Mathf.CeilToInt((float)_gridDimensions.y / 8), 
                Mathf.CeilToInt((float)_gridDimensions.z / 8));

            _floorVoxels = new Voxel[_voxels.Length];
            _ceilVoxels = new Voxel[_voxels.Length];
            floorVoxelsBuffer.GetData(_floorVoxels);
            
            voxelsBuffer.Release();
            floorVoxelsBuffer.Release();
            ceilVoxelsBuffer.Release();
            verticesBuffer.Release();
            verticesNormalsBuffer.Release();
            trianglesBuffer.Release();

            //SaveNotEmptyVoxels(notEmptyVoxels);
        }

        private void SaveNotEmptyVoxels(Voxel[] notEmptyVoxels)
        {
            int firstEmptyIndex = 0;

            for (int i = 0; i < notEmptyVoxels.Length; i++)
            {
                Voxel currentVoxel = notEmptyVoxels[i];
                if (currentVoxel.isWalkable == 1 || currentVoxel.isCeil == 1)
                {
                    if (firstEmptyIndex != 0)
                    {
                        notEmptyVoxels[firstEmptyIndex] = currentVoxel;
                        i = firstEmptyIndex;
                        firstEmptyIndex = 0;
                    }
                    continue;
                }

                if (firstEmptyIndex != 0)
                {
                    continue;
                }
                
                firstEmptyIndex = i;
            }

            _voxels = new Voxel[firstEmptyIndex];

            for (int i = 0; i < _voxels.Length; i++)
            {
                _voxels[i] = notEmptyVoxels[i];
            }
        }

        public void ModifyVoxelProperties(int walkableLayerMasks, NavMeshAgentSpecs navMeshAgentSpecs)
        {
            _floorAuxVoxels = new List<Vector3>();
            _ceilAuxVoxels = new List<Vector3>();
            
            for (int x = 0; x < _gridDimensions.x; x++)
            {
                for (int y = 0; y < _gridDimensions.y; y++)
                {
                    for (int z = 0; z < _gridDimensions.z; z++)
                    {
                        Vector3 voxelPosition = _voxels[x * _gridDimensions.y * _gridDimensions.z + y * _gridDimensions.z + z].position;

                        if (!AssertAgentMaxSlope(navMeshAgentSpecs.maxSlope, _voxels[x * _gridDimensions.y * _gridDimensions.z + y * _gridDimensions.z + z], walkableLayerMasks))
                        {
                            continue;
                        }

                        if (!AssertAgentRadius(navMeshAgentSpecs.radius, voxelPosition))
                        {
                            continue;
                        }

                        if (!AssertAgentHeight(navMeshAgentSpecs.height, navMeshAgentSpecs.radius, voxelPosition))
                        {
                            continue;
                        }

                        if (!AssertAgentStepHeight(navMeshAgentSpecs.stepHeight))
                        {
                            continue;
                        }
                        
                        _floorAuxVoxels.Add(_voxels[x * _gridDimensions.y * _gridDimensions.z + y * _gridDimensions.z + z].position);

                        _voxels[x * _gridDimensions.y * _gridDimensions.z + y * _gridDimensions.z + z].isWalkable = 1;
                    }
                }
            }
        }

        private bool AssertAgentMaxSlope(uint agentMaxSlope, Voxel voxel, int walkableLayerMasks)
        {
            Vector3 normal;
            float slope;
            
            RaycastHit hit;
            if (!Physics.Raycast(voxel.position + Vector3.up * (_voxelSize / 2), Vector3.down, out hit, _voxelSize, walkableLayerMasks))
            {
                return AssertCeil(ref voxel);
            }

            normal = hit.normal;

            slope = Vector3.Angle(normal, Vector3.up);

            if (slope <= agentMaxSlope)
            {
                return true;
            }
            
            return false;
        }

        private bool AssertCeil(ref Voxel voxel)
        {
            Vector3 normal;
            float slope;
            
            RaycastHit hit;
            if(!Physics.Raycast(voxel.position + Vector3.down * (_voxelSize / 2), Vector3.up, out hit, _voxelSize))
            {
                return false;    
            }
            normal = hit.normal;

            slope = Vector3.Angle(normal, Vector3.down);

            if (slope <= 90)
            {
                _ceilAuxVoxels.Add(voxel.position);
                //voxel.isCeil = 1;
            }

            return false;
        }

        private bool AssertAgentRadius(float agentRadius, Vector3 voxelPosition)
        {
            return true;
        }

        private bool AssertAgentHeight(float agentHeight, float agentRadius, Vector3 voxelPosition)
        {
            /*Vector3 cylinderCeilPosition = voxelPosition + new Vector3(0, agentHeight, 0);
            Vector3 heightCylinderVector = cylinderCeilPosition - voxelPosition;

            Voxel currentVoxel;
            
            for (int x = 0; x < voxels.GetLength(0); x++)
            {
                for (int y = 0; y < voxels.GetLength(1); y++)
                {
                    for (int z = 0; z < voxels.GetLength(2); z++)
                    {
                        currentVoxel = voxels[x, y, z];

                        Vector3 position = currentVoxel.position;

                        float positionOnSegment = 
                            (position.x * cylinderCeilPosition.x +
                            position.y * cylinderCeilPosition.y +
                            position.z * cylinderCeilPosition.z) /
                            (Mathf.Pow(cylinderCeilPosition.x, 2) + 
                            Mathf.Pow(cylinderCeilPosition.y, 2) +
                            Mathf.Pow(cylinderCeilPosition.z, 2));

                        if (positionOnSegment > 1 || positionOnSegment < 0)
                        {
                            continue;
                        }

                        if (Vector3.Distance(positionOnSegment * heightCylinderVector + voxelPosition, position) > agentRadius)
                        {
                            continue;
                        }

                        if (!currentVoxel.isCeil)
                        {
                            continue;
                        }
                        
                        return false;
                    }
                }
            }*/
            
            return true;
        }

        private bool AssertAgentStepHeight(float agentStepHeight)
        {
            return true;
        }

        public List<Vector3> GetWalkableVoxelPositions()
        {
            return _floorAuxVoxels;
        }

        public List<Vector3> GetCeilVoxelPositions()
        {
            return _ceilAuxVoxels;
        }
    }
}