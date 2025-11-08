using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    public class CustomSocketMarker : MonoBehaviour
    {
        [SerializeField] private string customSocketName = string.Empty;

        [SerializeField] private Transform? originParent;
        [SerializeField] private Vector3? _socketOffset;
        [SerializeField] private Vector3? _socketRotation;
        [SerializeField] private Vector3? _socketScale;

        public string CustomSocketName
        {
            get => customSocketName;
            set => customSocketName = value;
        }

        public Transform? OriginParent
        {
            get => originParent;
            set => originParent = value;
        }

        public Vector3? SocketOffset
        {
            get => _socketOffset;
            set => _socketOffset = value;
        }

        public Vector3? SocketRotation
        {
            get => _socketRotation;
            set => _socketRotation = value;
        }

        public Vector3? SocketScale
        {
            get => _socketScale;
            set => _socketScale = value;
        }
    }
}