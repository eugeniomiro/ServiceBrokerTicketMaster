using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using System.Xml;
using BrokerLib;

namespace BrokerTicketingExample
{
    public class PaymentProcessor
    {
        const int   max_threads = 1;

        static bool         _stopping   = false;
        static List<Thread> _threads    = new List<Thread>();

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
                    Guid dialogHandle,serviceInstance;
                    broker.BeginTransaction();
                    broker.Receive("ProcessPaymentTargetQueue", out messageType, out message, out serviceInstance, out dialogHandle);
                    if (message != null) {
                        switch (messageType) {
                            case "ProcessPaymentRequest": {
                                    var xml = new XmlDocument();
                                    xml.LoadXml(message);
                                    int BookingId = int.Parse(xml.DocumentElement["BookingId"].InnerText);
                                    string CreditCard = xml.DocumentElement["CreditCard"].InnerText;
                                    decimal BillAmount = decimal.Parse(xml.DocumentElement["BillAmount"].InnerText);
                                    Trace.Write(string.Format("Processing Order : {0} For £{1} Card : {2}... ", BookingId, BillAmount, CreditCard));
                                    Thread.Sleep(3000); /******CODE TO PROCESS PAYMENT WOULD GO HERE*****/
                                    Trace.WriteLine("Processed");
                                    broker.Send(dialogHandle, "<Payment><BookingId>" + BookingId + "</BookingId><PaymentStatus>2</PaymentStatus></Payment>", "ProcessPaymentResponse");
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