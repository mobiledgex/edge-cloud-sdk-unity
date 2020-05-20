
using UnityEngine;
using DistributedMatchEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace MobiledgeX
{
    // Complier Directive if UNITY_EDITOR is used extensively since MobiledgeXSettings Object is used in production 
    // Without this complier directive , Unity wont build the app
#if UNITY_EDITOR

    //using PropertyDrawer interface (used for creating custom views of fields in the inspector)
    //ReadOnly Attribute makes mapped ports' fields immutable in the inspector
    public class ReadOnlyAttribute : PropertyAttribute{  }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property,
                                                GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position,
                                   SerializedProperty property,
                                   GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
#endif
    // MatchingEngine AppPort is not Serializable
    // Protocol enum is used to override LProto easier to read
    [System.Serializable]
    public class Port
    {
        /// <summary>
        /// Tag to label certain tags, you can retrieve ports by tag 
        /// </summary>
        public string tag;

#if UNITY_EDITOR
        [ReadOnly]
#endif
        public Protocol protocol;
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public string fqdn_prefix;
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public string path_prefix;
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public int internal_port;
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public int public_port;
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public int end_port;
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public bool TLS;

        public Port(AppPort port)
        {
            switch (port.proto)
            {
                case LProto.L_PROTO_HTTP:
                case LProto.L_PROTO_TCP:
                    this.protocol = Protocol.TCP;
                    break;

                case LProto.L_PROTO_UDP:
                    this.protocol = Protocol.UDP;
                    break;

                case LProto.L_PROTO_UNKNOWN:
                default:
                    this.protocol = Protocol.Unknown;
                    break;

            }

            this.fqdn_prefix = port.fqdn_prefix;
            this.path_prefix = port.path_prefix;
            this.internal_port = port.internal_port;
            this.public_port = port.public_port;
            this.end_port = port.end_port;
            this.TLS = port.tls;
            this.tag = TLS==true?"Secure ":"" + protocol + " "+port.public_port;
        }
        public override string ToString()
        {
            return TLS == true ?"Secure":"" + protocol + public_port;
        }
    }
    public enum Protocol
    {
        TCP,
        UDP,
        Unknown
    }

     public class MobiledgeXSettings: ScriptableObject
    {
        public string orgName;
        public string appName;
        public string appVers;
        public string authPublicKey;
        /// <summary>
        /// Provide a Dropdown list of TCP ports mapped on the backend
        /// </summary>
        public TCPPorts TCP_Port;
        /// <summary>
        /// Provide a Dropdown list of UDP ports mapped on the backend
        /// </summary>
        public UDPPorts UDP_Port;
        /// <summary>
        /// Mapped Ports on the backend 
        /// </summary>
        public List<Port> mappedPorts;
#if UNITY_EDITOR
        [HideInInspector]
#endif
        public int mappedPortsSize;
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mappedPorts.Count != mappedPortsSize)
            {
                Debug.LogError("MobiledgeX: Please,Don't change the size of mapped ports!, You can add more ports on MobiledgeX Console in the Port Mapping Section in Your Application ");
                throw new System.Exception(" MobiledgeX: Please, use MobiledgeX Setup again, to retrieve mapped ports");
            }
        }
#endif
        /// <summary>
        /// Get Port by tag , tags can be defined in MobiledgeXSettings Inspector
        /// </summary>
        /// <param name="tag">string label for the port</param>
        /// <returns></returns>
        int getPortByTag(string tag)
        {
            try
            {             
                return mappedPorts.Where(i => i.tag == tag).FirstOrDefault().public_port;
            }
            catch (KeyNotFoundException)
            {
                Debug.LogError("MobiledgeX: Couldn't find "+ tag + " tag! , Make sure to tag the desired port in MobiledgeXSettings");
                throw new KeyNotFoundException("Couldn't find any port with the "+ tag + " tag");
            }
        }
    }
}