using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KMP.Networking
{
    public interface ITransmittable
    {
        void TransmitObject(NetworkMessage message);
        void ReceiveObject(NetworkMessage message);
    }
}
