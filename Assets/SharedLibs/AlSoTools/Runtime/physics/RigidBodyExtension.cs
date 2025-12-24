using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlSo
{
    public static class RigidBodyExtension
    {
        public static void Disable(this Rigidbody rb)
        {
            rb.useGravity = false;
            rb.detectCollisions = false;

            //rb.linearVelocity = Vector3.zero;
            throw new Exception("we need to uncomment line above");
            rb.angularVelocity = Vector3.zero;
        }

        public static void Enable(this Rigidbody rb)
        {
            rb.useGravity = true;
            rb.detectCollisions = true;
            //rb.constraints = originalConstraints;
        }
    }

    public static class JointExtension
    {
        private static Dictionary<ConfigurableJoint, Rigidbody> Connected { get; } = new Dictionary<ConfigurableJoint, Rigidbody>();

        public static void Disable(this ConfigurableJoint joint)
        {
            Connected.Add(joint, joint.connectedBody);
            joint.connectedBody = null;
        }

        public static void Enable(this ConfigurableJoint joint)
        {
            joint.connectedBody = Connected[joint];
            Connected.Remove(joint);
        }
    }
}
