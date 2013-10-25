using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace BrokerTicketingExample.Models
{
    public class PrintHub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }
    }
}