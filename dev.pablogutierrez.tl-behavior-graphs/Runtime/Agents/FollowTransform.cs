using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLTLUnity.Agents
{
    [ExecuteAlways]
    public class FollowTransform : MonoBehaviour
    {
        public Transform target; // The target game object to follow

        void Update()
        {
            if (target != null)
            {
                // Set the position and rotation of this game object to match the target
                transform.position = target.position;
                transform.rotation = target.rotation;
            }
        }
    } // FollowTransform

} // namespace TLTLUnity.Agents
