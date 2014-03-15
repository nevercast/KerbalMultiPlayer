using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KMP.Networking
{
    public interface ITransmittable
    {
        public void TransmitObject(NetworkMessage message);
        public void ReceiveObject(NetworkMessage message);
    }
}
