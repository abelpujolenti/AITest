using UnityEngine;
using UnityEngine.Serialization;

namespace Demo
{
    public class ChangeColor : MonoBehaviour
    {
        [FormerlySerializedAs("_color")] public Color color;
        
        void Start()
        {
            GetComponent<MeshRenderer>().material.color = color;
        }
    }
}
