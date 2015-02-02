using System;
using System.Net;

namespace DnnPackager
{
    public class ExtendedWebClient : WebClient
    {
        public int Timeout { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request != null)
                request.Timeout = Timeout;
            return request;
        }

        public ExtendedWebClient()
        {
            Timeout = 100000; // the standard HTTP Request Timeout default
        }
    }
}