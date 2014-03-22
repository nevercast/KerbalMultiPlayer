using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Runtime.Serialization;
using KMP.Networking;
using KMP.Networking.Conversion;

namespace KMP
{
    public class KMPScenarioUpdate : ITransmittable
    {
		/// <summary>
		/// The Scenario name
		/// </summary>
        public String name;

		public ConfigNode scenarioNode = null;
		public double tick = 0d;

        private KMPScenarioUpdate() { } // Used by ITransmittable

		public KMPScenarioUpdate(string _name, ScenarioModule _module = null, double _tick = 0d)
        {
			scenarioNode = new ConfigNode();
			if (_module != null) _module.Save(scenarioNode);
			tick = _tick;
			name = _name;
        }
		
		public ConfigNode getScenarioNode()
		{
            return scenarioNode;
		}
		
		public void setScenarioNode(ConfigNode node)
		{
			scenarioNode = node;
		}

        public void TransmitObject(NetworkMessage message)
        {
            message.WriteString(name);
            message.WriteDouble(tick);
            message.WriteBoolean(scenarioNode != null);
            if (scenarioNode != null)
            {
                message.WriteConfigNode(scenarioNode);
            }
        }

        public void ReceiveObject(NetworkMessage message)
        {
            name = message.ReadString();
            tick = message.ReadDouble();
            if (message.ReadBoolean())
            {
                scenarioNode = message.ReadConfigNode();
            }
        }
    }

}
