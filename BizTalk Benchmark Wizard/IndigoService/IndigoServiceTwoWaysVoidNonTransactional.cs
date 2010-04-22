using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace IndigoService
{
    public class IndigoServiceTwoWaysVoidNonTransactional : IServiceTwoWaysVoidNonTransactional
    {
        // Fields
        private static long msgCount = 0;

        // Methods
        public void ConsumeMessage(Message msg)
        {
            try
            {
                DateTime now = DateTime.Now;
                msgCount++;
                Console.WriteLine("{0}.[{1}] Got a message...", msgCount.ToString("0000"), DateTime.Now.ToLongTimeString());
                
            }
            catch (Exception exception)
            {
                Console.WriteLine("IndigoServiceTwoWaysVoidNonTransactional.ConsumeMessage.Exception!" + Environment.NewLine + exception.ToString());
            }
        }
        public void ConsumeMessage2(SmallMessage msg)
        {
            try
            {

                DateTime now = DateTime.Now;
                msgCount++;
                Console.WriteLine("{0}.[{1}] Got a message...", msgCount.ToString("0000"), DateTime.Now.ToLongTimeString());
            }
            catch (Exception exception)
            {
                Console.WriteLine("IndigoServiceTwoWaysVoidNonTransactional.ConsumeMessage.Exception!" + Environment.NewLine + exception.ToString());
            }
        }

        public void SelfHostService()
        {
            Uri uri = new Uri("net.tcp://localhost:8000/servicemodelsamples/service");
            using (ServiceHost host = new ServiceHost(typeof(IndigoServiceTwoWaysVoidNonTransactional), new Uri[] { uri }))
            {
                try
                {
                    host.Open();
                }
                catch (Exception exception)
                {
                    Console.WriteLine("IndigoServiceTwoWaysVoidNonTransactional.SelfHostService.Exception!" + Environment.NewLine + exception.ToString());
                }
                Console.WriteLine("The [Indigo.TwoWaysVoid.NonTransactional] service is ready to accept messages.");
                Console.WriteLine("Press <ENTER> to terminate service.");
                Console.ReadLine();
                host.Close();
            }
        }
    }


}
