using UnityEngine;

namespace ProceduralEnvironment
{
    public abstract class ProceduralElement : MonoBehaviour
    {
        [Header("Procedural Element")]
        [SerializeField] private bool autoRegenerate = true;
        [SerializeField] private bool needsRegeneration = true;

        public bool AutoRegenerate => autoRegenerate;
        public bool NeedsRegeneration => needsRegeneration;

        public void MarkDirty()
        {
            needsRegeneration = true;

            if (autoRegenerate)
                Regenerate();
        }

        public void RegenerateIfNeeded()
        {
            if (!needsRegeneration)
                return;

            Regenerate();
        }

        public void Regenerate()
        {
            OnRegenerate();
            needsRegeneration = false;
        }

        protected abstract void OnRegenerate();
    }
}