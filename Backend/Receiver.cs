using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Xml.Linq;
using Backend.Events;
using Backend.Model;
using Common;
using System.Net.NetworkInformation;
using Common.Exceptions;
using Backend.Converter;
using Common.Configuration;
using NLog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend {

    public sealed class Receiver : IReceiver, IDisposable {

        private ILogger<Receiver> Logger { get; init; }
        private UdpClient Client { get; set; }
        private ILogConverter Converter { get; set; }

        private IOptionsMonitor<ApplicationConfiguration> ApplicationConfiguration { get; init; }

        public event EventHandler<LogReceivedEventArgs> LogReceived;

        public Receiver(IOptionsMonitor<ApplicationConfiguration> applicationConfiguration, ILogger<Receiver> logger) {
            Logger = logger;
            ApplicationConfiguration = applicationConfiguration;
        }

        public void Initialize(Configuration configuration) {
            Converter = IoC.Get<ILogConverter>(configuration.LogType);
            int port = 0;
            if (configuration.LogType == LogType.Chainsaw) {
                port = configuration.PortChainsaw;
            } else if (configuration.LogType == LogType.Logcat) {
                port = configuration.PortLogcat;
            }
            Dispose();

            bool isPortAlreadyInUse = (from p
                                 in IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners()
                                 where p.Port == port
                                 select p).Count() == 1;

            if (isPortAlreadyInUse) {
                throw new LoginatorException("Port " + port + " is already in use.");
            }

            Client = new UdpClient(port);
            UdpState state = new UdpState(Client, new IPEndPoint(IPAddress.Any, 0));
            Client.BeginReceive(new AsyncCallback(DataReceived), state);
        }

        public void Dispose() {
            Client?.Dispose();
        }

        private void DataReceived(IAsyncResult ar) {

            try {
                UdpClient c = (UdpClient)((UdpState)ar.AsyncState).u;
                IPEndPoint wantedIpEndPoint = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
                IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveBytes = c.EndReceive(ar, ref receivedIpEndPoint);

                // Check sender
                bool isRightHost = (wantedIpEndPoint.Address.Equals(receivedIpEndPoint.Address)
                                   || wantedIpEndPoint.Address.Equals(IPAddress.Any));
                bool isRightPort = (wantedIpEndPoint.Port == receivedIpEndPoint.Port)
                                   || wantedIpEndPoint.Port == 0;
                if (isRightHost && isRightPort) {
                    string receivedText = Encoding.UTF8.GetString(receiveBytes);

                    if (ApplicationConfiguration.CurrentValue.IsMessageTraceEnabled) {
                        Logger.LogTrace(receivedText);
                    }

                    IEnumerable<Log> logs = Converter.Convert(receivedText);
                    if (LogReceived != null) {
                        foreach (Log log in logs) {
                            if (log == Log.DEFAULT) {
                                continue;
                            }
                            LogReceived(this, new LogReceivedEventArgs(log));
                        }
                    }
                }

                // Restart listening for udp data packages
                c.BeginReceive(new AsyncCallback(DataReceived), ar.AsyncState);
            } catch (Exception e) {
                Console.WriteLine("Could not read package: " + e);
            }
        }
    }
}
