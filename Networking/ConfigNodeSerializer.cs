using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace KMP.Networking
{
    /// <summary>
    /// (De)Serializes ConfigNode objects. 
    /// Better implementations coming in the future
    /// </summary>
    public class ConfigNodeSerializer
    {
        public static byte[] Serialize(ConfigNode node)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, node);
                return ms.ToArray();
            }
        }

        public static ConfigNode Deserialize(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                return new BinaryFormatter().Deserialize(ms) as ConfigNode;
            }
        }
    }
}
