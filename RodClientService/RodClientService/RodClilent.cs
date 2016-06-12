using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace RodClientService
{       
    [ServiceContract]
    public class RodClient
    {
        [OperationContract]
        public bool WriteMessage(string message)
        {
            Console.WriteLine(message);
            return true;
        }
        
    }
}
