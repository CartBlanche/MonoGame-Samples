using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Net;
using RacingGame.GameLogic;
using RacingGame.GameScreens;

namespace RacingGame.Multiplayer
{
    /// <summary>
    /// The lobby of a LAN race, before it starts: who is in it, which car each player drives,
    /// who is ready, and the track and number of laps the host has chosen. Modelled on ShipGame's
    /// LanLobby.
    ///
    /// The host owns the lobby and sends the whole of it to the clients whenever anything changes
    /// (a client only tells the host about itself). A client appears in the lobby when it has sent
    /// its first update, which it does once it has chosen its car.
    /// </summary>
    public sealed class RaceLobby
    {
        public const int MinLaps = 1;
        public const int MaxLaps = 5;

        readonly RaceNetSession net;
        readonly List<RacePlayerInfo> players = new List<RacePlayerInfo>();
        int track;
        int laps = Player.LapCount;

        // the car and ready state of this machine, kept here too so a client can send them
        // before the host has put it in the lobby
        int localCar;
        int localColor;
        bool localReady;

        public RaceLobby(RaceNetSession net)
        {
            this.net = net;
            track = TrackSelection.SelectedTrackNumber;

            if (net.IsHost)
            {
                net.Register(RacePacketType.LobbyUpdate, PacketAudience.FromClient, OnLobbyUpdate);
                net.GamerLeft += OnGamerLeft;
            }
            else
            {
                net.Register(RacePacketType.LobbySync, PacketAudience.FromHost, OnLobbySync);
                net.Register(RacePacketType.RaceStart, PacketAudience.FromHost, OnRaceStart);
            }
        }

        public bool IsHost { get { return net.IsHost; } }
        public IList<RacePlayerInfo> Players { get { return players; } }
        public int Track { get { return track; } }
        public int Laps { get { return laps; } }
        public bool LocalReady { get { return localReady; } }

        /// <summary>
        /// The race that has been announced (null until it has). A screen reads this and starts it.
        /// </summary>
        public RaceStartPacket PendingStart { get; private set; }

        /// <summary>
        /// This machine's player (null until the host has put it in the lobby)
        /// </summary>
        public RacePlayerInfo LocalPlayer
        {
            get
            {
                string id = net.LocalGamer == null ? null : net.LocalGamer.GamerId;
                return players.Find(p => p.Id == id);
            }
        }

        /// <summary>
        /// The lobby changed (someone joined, left or got ready, or the host chose another track)
        /// </summary>
        public event Action Changed;

        #region Local actions

        /// <summary>
        /// Choose this machine's car. Puts this player in the lobby.
        /// </summary>
        public void SetLocalCar(int car, int color)
        {
            localCar = car;
            localColor = color;
            SendLocal();
        }

        public void SetLocalReady(bool ready)
        {
            localReady = ready;
            SendLocal();
        }

        void SendLocal()
        {
            if (net.IsHost)
            {
                RacePlayerInfo me = LocalPlayer;
                if (me == null)
                {
                    me = new RacePlayerInfo { Id = net.LocalGamer.GamerId, Name = MultiplayerSessionService.LocalName(net.LocalGamer.Gamertag) };
                    players.Insert(0, me);
                }
                me.Name = MultiplayerSessionService.LocalName(net.LocalGamer.Gamertag);
                me.Car = (byte)localCar;
                me.Color = (byte)localColor;
                me.Ready = localReady;
                Sync();
            }
            else
            {
                net.SendToHost(new LobbyUpdatePacket
                {
                    Car = (byte)localCar,
                    Color = (byte)localColor,
                    Ready = localReady,
                    Name = MultiplayerSessionService.LocalName(net.LocalGamer.Gamertag),
                }, true);
            }
        }

        /// <summary>
        /// Host: the next (+1) or previous (-1) track
        /// </summary>
        public void ChangeTrack(int delta)
        {
            if (!net.IsHost)
                return;

            int count = TrackSelection.NumberOfTracks;
            track = (track + count + delta) % count;
            TrackSelection.SelectTrackNumber(track);
            Sync();
        }

        /// <summary>
        /// Host: more (+1) or fewer (-1) laps
        /// </summary>
        public void ChangeLaps(int delta)
        {
            if (!net.IsHost)
                return;

            laps = Math.Max(MinLaps, Math.Min(MaxLaps, laps + delta));
            Sync();
        }

        /// <summary>
        /// Host: can the race start? At least two players, everybody connected is in the lobby,
        /// and everybody is ready.
        /// </summary>
        public bool CanStart
        {
            get
            {
                if (!net.IsHost || players.Count < 2 || players.Count != 1 + net.Session.RemoteGamers.Count)
                    return false;

                foreach (RacePlayerInfo player in players)
                    if (!player.Ready)
                        return false;
                return true;
            }
        }

        /// <summary>
        /// Host: start the race if it can start
        /// </summary>
        public bool TryStart()
        {
            if (!CanStart)
                return false;

            for (int i = 0; i < players.Count; i++)
                players[i].Slot = (byte)i;

            RaceStartPacket start = new RaceStartPacket
            {
                Track = (byte)track,
                Laps = (byte)laps,
                Players = new List<RacePlayerInfo>(players),
            };

            MultiplayerSessionService.MarkInProgress(net.Session);
            net.Broadcast(start, true);
            PendingStart = start;
            return true;
        }

        #endregion

        #region Host

        void OnLobbyUpdate(NetworkGamer sender, System.IO.BinaryReader reader)
        {
            LobbyUpdatePacket update = LobbyUpdatePacket.Deserialize(reader);

            RacePlayerInfo info = players.Find(p => p.Id == sender.GamerId);
            if (info == null)
            {
                if (players.Count >= MultiplayerSessionService.MaxPlayers || PendingStart != null)
                    return;

                info = new RacePlayerInfo { Id = sender.GamerId, Name = MultiplayerSessionService.DisplayName(sender.Gamertag) };
                players.Add(info);
            }

            // the name the player chose (a client's unique gamertag stays the fallback)
            string chosen = update.Name == null ? "" : update.Name.Trim();
            if (chosen.Length > 20)
                chosen = chosen.Substring(0, 20);
            if (chosen.Length > 0)
                info.Name = chosen;

            info.Car = update.Car;
            info.Color = update.Color;
            info.Ready = update.Ready;
            Sync();
        }

        void OnGamerLeft(NetworkGamer gamer)
        {
            if (players.RemoveAll(p => p.Id == gamer.GamerId) > 0)
                Sync();
        }

        void Sync()
        {
            for (int i = 0; i < players.Count; i++)
                players[i].Slot = (byte)i;

            net.Broadcast(new LobbySyncPacket
            {
                Track = (byte)track,
                Laps = (byte)laps,
                Players = new List<RacePlayerInfo>(players),
            }, true);

            RaiseChanged();
        }

        #endregion

        #region Client

        void OnLobbySync(NetworkGamer sender, System.IO.BinaryReader reader)
        {
            LobbySyncPacket sync = LobbySyncPacket.Deserialize(reader);
            track = sync.Track;
            laps = sync.Laps;
            players.Clear();
            players.AddRange(sync.Players);

            RacePlayerInfo me = LocalPlayer;
            if (me != null)
                localReady = me.Ready;

            RaiseChanged();
        }

        void OnRaceStart(NetworkGamer sender, System.IO.BinaryReader reader)
        {
            // reliable packets can be repeated by the library's resends
            if (PendingStart == null)
                PendingStart = RaceStartPacket.Deserialize(reader);
        }

        #endregion

        void RaiseChanged()
        {
            if (Changed != null)
                Changed();
        }

        /// <summary>
        /// Stop listening (the race screen takes the session over)
        /// </summary>
        public void Detach()
        {
            net.Unregister(RacePacketType.LobbyUpdate);
            net.Unregister(RacePacketType.LobbySync);
            net.Unregister(RacePacketType.RaceStart);
            net.GamerLeft -= OnGamerLeft;
            Changed = null;
        }
    }

    /// <summary>
    /// The LAN race this machine is in, if any: the session and its lobby. The screens of the
    /// LAN flow (browser, car selection, lobby, race) share it, as ShipGame's screens share
    /// the game manager's network and lobby.
    /// </summary>
    public static class LanRace
    {
        /// <summary>
        /// Final place (1 = winner) of this machine's car once it has finished, else 0.
        /// Read by the trophy drawing in BaseGame.
        /// </summary>
        public static int LocalPlace;

        public static RaceNetSession Net { get; private set; }
        public static RaceLobby Lobby { get; private set; }

        /// <summary>
        /// Why the last race was left (shown by the browser), or null
        /// </summary>
        public static string Message { get; set; }

        public static bool InRace { get { return Net != null; } }

        public static void Enter(RaceNetSession net)
        {
            Leave(null);
            Net = net;
            Lobby = new RaceLobby(net);
            Message = null;
        }

        /// <summary>
        /// Pump the session. Called every frame.
        /// </summary>
        public static void Update()
        {
            if (Net == null)
                return;

            try
            {
                Net.Update();
            }
            catch (Exception ex)
            {
                NetLog.Write("LAN session error: " + ex);
                Leave("Network error: " + ex.Message);
            }
        }

        /// <summary>
        /// Leave the race: stop listening and disconnect
        /// </summary>
        public static void Leave(string message)
        {
            LocalPlace = 0;
            if (Lobby != null)
                Lobby.Detach();
            if (Net != null)
                Net.Dispose();

            Lobby = null;
            Net = null;
            if (message != null)
                Message = message;
        }
    }
}
