using UnityEngine;

namespace ECS.Components.AI.Combat
{
    public class DamageFeedbackComponent
    {
        private MeshRenderer _meshRenderer;

        private float _flashTime;

        private Color _originalColor;
        private Color _flashColor;

        public DamageFeedbackComponent(MeshRenderer meshRenderer, float flashTime, Color flashColor)
        {
            _meshRenderer = meshRenderer;
            _flashTime = flashTime;
            _originalColor = _meshRenderer.material.color;
            _flashColor = flashColor;
        }

        public MeshRenderer GetMeshRenderer()
        {
            return _meshRenderer;
        }

        public float GetFlashTime()
        {
            return _flashTime;
        }

        public Color GetOriginalColor()
        {
            return _originalColor;
        }

        public Color GetFlashColor()
        {
            return _flashColor;
        }
    }
}
