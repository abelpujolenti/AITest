using AI;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(NavMesh)), CanEditMultipleObjects]
    public class NavMeshButtonsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            NavMesh navMesh = (NavMesh)target;

            if (GUILayout.Button("Bake"))
            {
                navMesh.BakeNavMesh();
            }

            if (GUILayout.Button("Clear"))
            {
                navMesh.ClearNavMesh();
            }
        }
    }
}