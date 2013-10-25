using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using System.Xml;
using BrokerLib;
using BrokerTicketingExample.Models;
using Microsoft.AspNet.SignalR;

namespace BrokerTicketingExample
{
    public class PrintProcessor
    {
        const int max_threads = 1;

        static bool         _stopping = false;
        static List<Thread> _threads = new List<Thread>();

        public static void Start()
        {
            for (int i = 0; i < max_threads; i++) {
                _threads.Add(new Thread(monitorPaymentQueue));
                _threads[i].Start();
            }
        }

        public static void Stop()
        {
            _stopping = true;
            foreach (var t in _threads) {
                t.Join();
            }
        }

        static void monitorPaymentQueue()
        {
            using (var broker = new Broker(ConfigurationManager.ConnectionStrings["TicketMaster"].ConnectionString)) {
                while (!_stopping) {
                    string message, messageType;
                    Guid dialogHandle, serviceInstance;
                    broker.BeginTransaction();
                    broker.Receive("PrintTargetQueue", out messageType, out message, out serviceInstance, out dialogHandle);
                    if (message != null) {
                        switch (messageType) {
                            case "PrintRequest": {
                                    var xml = new XmlDocument();
                                    xml.LoadXml(message);
                                    int BookingId = int.Parse(xml.DocumentElement.InnerText);
                                    Trace.Write(string.Format("Printing Tickets For Order : {0}... ", BookingId));
                                    Thread.Sleep(3000); /******CODE TO PRINT WOULD GO HERE*****/
                                    Trace.WriteLine("Printed");
                                    broker.Send(dialogHandle, "<Print><BookingId>" + BookingId + "</BookingId><PrintStatus>2</PrintStatus></Print>", "PrintResponse");

                                    GlobalHost.ConnectionManager.GetHubContext<PrintHub>().Clients.All.printed(BookingId);

                                    broker.EndDialog(dialogHandle);
                                    break;
                                }
                        }
                    }
                    broker.Commit();
                }
            }
        }
    }
}