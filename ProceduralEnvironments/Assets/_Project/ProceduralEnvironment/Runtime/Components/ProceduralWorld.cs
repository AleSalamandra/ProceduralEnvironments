using UnityEngine;

namespace ProceduralEnvironment
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class ProceduralWorld : MonoBehaviour
    {
        [Header("Scene Containers")]
        [SerializeField] private Transform editableRoot;
        [SerializeField] private Transform generatedRoot;
        [SerializeField] private Transform bakedRoot;

        public Transform EditableRoot => editableRoot;
        public Transform GeneratedRoot => generatedRoot;
        public Transform BakedRoot => bakedRoot;

        private void Reset()
        {
            SetupRoots();
        }

        private void OnValidate()
        {
            SetupRoots();
        }

        private void SetupRoots()
        {
            editableRoot = GetOrCreateChild("Editable");
            generatedRoot = GetOrCreateChild("Generated");
            bakedRoot = GetOrCreateChild("Baked");
        }

        private Transform GetOrCreateChild(string childName)
        {
            Transform existingChild = transform.Find(childName);

            if (existingChild != null)
                return existingChild;

            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(transform);
            childObject.transform.localPosition = Vector3.zero;
            childObject.transform.localRotation = Quaternion.identity;
            childObject.transform.localScale = Vector3.one;

            return childObject.transform;
        }
    }
}