
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peer2PeerMasterServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Server Started");
            var registeredHosts = new Dictionary<IPEndPoint, AvailableGame>();
            int port = 6000;
            using var udp = new UdpClient(port);
            var cts = new CancellationTokenSource();

            Console.WriteLine($"Listening on UDP port {port}");
            Console.WriteLine("Press Ctrl+C to quit");

            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var receiveTask = udp.ReceiveAsync();
                    var completedTask = await Task.WhenAny(receiveTask, Task.Delay(10, cts.Token));
                    if (completedTask == receiveTask)
                    {
                        var result = receiveTask.Result;
                        await HandleMessageAsync(result.Buffer, result.RemoteEndPoint, udp, registeredHosts);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                udp.Close();
                Console.WriteLine("Server shutting down");
            }
        }

        static async Task HandleMessageAsync(byte[] buffer, IPEndPoint sender, UdpClient udp, Dictionary<IPEndPoint, AvailableGame> registeredHosts)
        {
            if (buffer.Length == 0) return;
            var action = buffer[0];
            var ms = new System.IO.MemoryStream(buffer, 1, buffer.Length - 1);
            var reader = new System.IO.BinaryReader(ms, Encoding.UTF8);
            switch (action)
            {
                case 0: // Register new game
                    if (!registeredHosts.ContainsKey(sender))
                    {
                        var game = new AvailableGame
                        {
                            Count = reader.ReadInt32(),
                            GamerTag = reader.ReadString(),
                            PrivateGamerSlots = reader.ReadInt32(),
                            MaxGamers = reader.ReadInt32(),
                            IsHost = reader.ReadBoolean(),
                            InternalIP = ReadIPEndPoint(reader),
                            ExternalIP = sender,
                            Game = reader.ReadString()
                        };
                        registeredHosts.Add(game.ExternalIP, game);
                        Console.WriteLine($"Got registration for host {game}");
                    }
                    break;
                case 1: // Client wants list of registered hosts
                    string appid = reader.ReadString();
                    Console.WriteLine($"Sending list of {registeredHosts.Count} hosts to client {sender}");
                    foreach (var g1 in registeredHosts.Values)
                    {
                        if (g1.Game == appid)
                        {
                            var om = new System.IO.MemoryStream();
                            var w = new System.IO.BinaryWriter(om, Encoding.UTF8);
                            w.Write((byte)1); // response type
                            w.Write(g1.Count);
                            w.Write(g1.GamerTag);
                            w.Write(g1.PrivateGamerSlots);
                            w.Write(g1.MaxGamers);
                            w.Write(g1.IsHost);
                            WriteIPEndPoint(w, g1.InternalIP);
                            WriteIPEndPoint(w, g1.ExternalIP);
                            await udp.SendAsync(om.ToArray(), (int)om.Length, sender);
                        }
                    }
                    break;
                case 2: // Client wants to connect to a specific host
                    var clientInternal = ReadIPEndPoint(reader);
                    var hostExternal = ReadIPEndPoint(reader);
                    var token = reader.ReadString();
                    Console.WriteLine($"{sender} requesting introduction to {hostExternal} (token {token})");
                    foreach (var elist in registeredHosts.Values)
                    {
                        if (elist.ExternalIP.Equals(hostExternal))
                        {
                            Console.WriteLine("Sending introduction...");
                            // Send introduction to both client and host
                            var om = new System.IO.MemoryStream();
                            var w = new System.IO.BinaryWriter(om, Encoding.UTF8);
                            w.Write((byte)2); // response type
                            WriteIPEndPoint(w, elist.InternalIP);
                            WriteIPEndPoint(w, elist.ExternalIP);
                            WriteIPEndPoint(w, clientInternal);
                            WriteIPEndPoint(w, sender);
                            w.Write(token);
                            await udp.SendAsync(om.ToArray(), (int)om.Length, sender);
                        }
                    }
                    break;
                case 3: // Remove host
                    if (registeredHosts.ContainsKey(sender))
                    {
                        var game = registeredHosts[sender];
                        var tag = reader.ReadString();
                        var gamename = reader.ReadString();
                        if (game.GamerTag == tag)
                        {
                            Console.WriteLine($"Remove for host {game.ExternalIP}");
                            registeredHosts.Remove(game.ExternalIP);
                        }
                    }
                    break;
                case 4: // Update host
                    if (registeredHosts.ContainsKey(sender))
                    {
                        var game = registeredHosts[sender];
                        var count = reader.ReadInt32();
                        var tag = reader.ReadString();
                        if (game.GamerTag == tag)
                        {
                            Console.WriteLine($"Update for host {game.ExternalIP}");
                            game.Count = count;
                            game.PrivateGamerSlots = reader.ReadInt32();
                            game.MaxGamers = reader.ReadInt32();
                            game.IsHost = reader.ReadBoolean();
                            game.InternalIP = ReadIPEndPoint(reader);
                            game.Game = reader.ReadString();
                        }
                    }
                    break;
            }
        }

        static IPEndPoint ReadIPEndPoint(System.IO.BinaryReader reader)
        {
            var ipLen = reader.ReadInt32();
            var ipBytes = reader.ReadBytes(ipLen);
            var ip = new IPAddress(ipBytes);
            var port = reader.ReadInt32();
            return new IPEndPoint(ip, port);
        }

        static void WriteIPEndPoint(System.IO.BinaryWriter writer, IPEndPoint ep)
        {
            var ipBytes = ep.Address.GetAddressBytes();
            writer.Write(ipBytes.Length);
            writer.Write(ipBytes);
            writer.Write(ep.Port);
        }
    }

    class AvailableGame
    {
        public IPEndPoint ExternalIP { get; set; }
        public IPEndPoint InternalIP { get; set; }
        public int Count { get; set; }
        public string GamerTag { get; set; }
        public int PrivateGamerSlots { get; set; }
        public int MaxGamers { get; set; }
        public bool IsHost { get; set; }
        public string Game { get; set; }

        public override string ToString()
        {
            return $"External {ExternalIP}\n Internal {InternalIP} GamerTag {GamerTag}\n";
        }
    }
}
