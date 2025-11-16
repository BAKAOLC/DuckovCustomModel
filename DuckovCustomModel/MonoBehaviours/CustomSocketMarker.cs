using System.Collections.Generic;
using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    public class CustomSocketMarker : MonoBehaviour
    {
        [SerializeField] private List<string> customSocketNames = [];
        [SerializeField] private Transform? originParent;

        [SerializeField] private Vector3? _socketOffset;
        [SerializeField] private Quaternion? _socketRotation;
        [SerializeField] private Vector3? _socketScale;

        public string[] CustomSocketNames => customSocketNames.ToArray();

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

        public Quaternion? SocketRotation
        {
            get => _socketRotation;
            set => _socketRotation = value;
        }

        public Vector3? SocketScale
        {
            get => _socketScale;
            set => _socketScale = value;
        }

        public void AddCustomSocketName(string socketName)
        {
            if (!customSocketNames.Contains(socketName)) customSocketNames.Add(socketName);
        }

        public void RemoveCustomSocketName(string socketName)
        {
            if (customSocketNames.Contains(socketName)) customSocketNames.Remove(socketName);
        }
    }
}
