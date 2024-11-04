using UnityEngine;

namespace Demo
{
    public class ChangeColor : MonoBehaviour
    {
        [SerializeField] private Color _color;
        
        void Start()
        {
            GetComponent<MeshRenderer>().material.color = _color;
        }
    }
}
