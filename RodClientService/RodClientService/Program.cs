using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace RodClientService
{
    class Program
    {
        static void Main(string[] args)
        {
           
            ServiceHost svcHost = new ServiceHost(typeof(RodClient), new Uri("http://localhost:8000/RodClientsERVICE"));
            try
            {
                // Check to see if the service host already has a ServiceMetadataBehavior
                ServiceMetadataBehavior smb = svcHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
                // If not, add one
                if (smb == null)
                    smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                svcHost.Description.Behaviors.Add(smb);
                // Add MEX endpoint
                svcHost.AddServiceEndpoint(
                  ServiceMetadataBehavior.MexContractName,
                  MetadataExchangeBindings.CreateMexHttpBinding(),
                  "mex"
                );
                // Add application endpoint
                svcHost.AddServiceEndpoint(typeof(RodClient), new BasicHttpBinding(), "");
                // Open the service host to accept incoming calls
                svcHost.Open();

                // The service can now be accessed.
                Console.WriteLine("The service is ready.");
                Console.WriteLine("Press <ENTER> to terminate service.");
                Console.WriteLine();
                Console.ReadLine();

                // Close the ServiceHostBase to shutdown the service.
                svcHost.Close();
            }
            catch (CommunicationException commProblem)
            {
                Console.WriteLine("There was a communication problem. " + commProblem.Message);
                Console.Read();
            }
        }
    }


}
