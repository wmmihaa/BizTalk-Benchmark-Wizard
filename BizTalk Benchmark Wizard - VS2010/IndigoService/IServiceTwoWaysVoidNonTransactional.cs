using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Runtime.Serialization;

namespace IndigoService
{
    [ServiceContract]
    public interface IServiceTwoWaysVoidNonTransactional
    {
        // Methods
        [OperationContract(Action = "*", IsOneWay = false)]
        void ConsumeMessage(Message msg);

        [OperationContract(IsOneWay = false)]
        void ConsumeMessage2(SmallMessage msg);
    }
    [DataContract]
    public class SmallMessage
    {
        [DataMember]
        public string Name { get; set; }
    }

}
