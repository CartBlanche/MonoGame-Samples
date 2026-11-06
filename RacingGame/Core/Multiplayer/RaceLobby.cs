using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Net;
using RacingGame.GameLogic;
using RacingGame.GameScreens;
using RacingGame.Tracks;

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

        /// <summary>
        /// The track number that means "a custom track" (0 to 2 are the built-in tracks)
        /// </summary>
        public const byte CustomTrackNumber = 255;

        readonly RaceNetSession net;
        readonly List<RacePlayerInfo> players = new List<RacePlayerInfo>();
        int track;
        int laps = Player.LapCount;

        // the car and ready state of this machine, kept here too so a client can send them
        // before the host has put it in the lobby
        int localCar;
        int localColor;
        bool localReady;

        // the custom track the host has chosen (null: a built-in track)
        string customId;
        string customName = "";
        string customAuthor = "";
        bool localHasTrack = true;          // a client: the custom track is here (loaded or received)
        string trackStatus;                 // what to tell the player while it is not
        readonly HashSet<string> failedTracks = new HashSet<string>();
        bool synced;                        // a client: the first sync has been handled

        public RaceLobby(RaceNetSession net)
        {
            this.net = net;
            track = TrackSelection.SelectedTrackNumber;

            if (net.IsHost)
            {
                net.Register(RacePacketType.LobbyUpdate, PacketAudience.FromClient, OnLobbyUpdate);
                net.Register(RacePacketType.TrackRequest, PacketAudience.FromClient, OnTrackRequest);
                net.GamerLeft += OnGamerLeft;
            }
            else
            {
                net.Register(RacePacketType.LobbySync, PacketAudience.FromHost, OnLobbySync);
                net.Register(RacePacketType.RaceStart, PacketAudience.FromHost, OnRaceStart);
                net.Register(RacePacketType.TrackFile, PacketAudience.FromHost, OnTrackFile);
            }
        }

        public bool IsHost { get { return net.IsHost; } }
        public IList<RacePlayerInfo> Players { get { return players; } }
        public int Track { get { return track; } }
        public int Laps { get { return laps; } }
        public bool LocalReady { get { return localReady; } }

        /// <summary>
        /// Has the host chosen a custom track? Then its id, name and author.
        /// </summary>
        public bool IsCustomTrack { get { return customId != null; } }
        public string CustomTrackId { get { return customId; } }
        public string CustomTrackName { get { return customName; } }
        public string CustomTrackAuthor { get { return customAuthor; } }

        /// <summary>
        /// A client: does this machine have the track (always true for built-in tracks)?
        /// </summary>
        public bool LocalHasTrack { get { return localHasTrack; } }

        /// <summary>
        /// A client without the track yet: what is happening ("Downloading track...", or why it failed)
        /// </summary>
        public string TrackStatus { get { return trackStatus; } }

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
            // not ready to race without the track
            localReady = ready && localHasTrack;
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
                me.HasTrack = true;
                Sync();
            }
            else
            {
                net.SendToHost(new LobbyUpdatePacket
                {
                    Car = (byte)localCar,
                    Color = (byte)localColor,
                    Ready = localReady && localHasTrack,
                    HasTrack = localHasTrack,
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
            if (customId != null)
            {
                // leaving a custom track: the first (or last) built-in one
                track = delta > 0 ? 0 : count - 1;
                customId = null;
                customName = customAuthor = "";
            }
            else
                track = (track + count + delta) % count;
            TrackSelection.SelectTrackNumber(track);
            UnreadyEverybody();
            Sync();
        }

        /// <summary>
        /// Host: race on a custom track from the Tracks folder. The others get it from this machine
        /// if they do not have it. Returns null, or why the track can't be used.
        /// </summary>
        public string ChooseCustomTrack(CustomTrackInfo info)
        {
            if (!net.IsHost)
                return "Only the host chooses the track.";
            if (info == null || !info.IsPlayable)
                return "That track can't be played.";

            SharedTrack shared = SharedTracks.FromInfo(info);
            if (shared.Bytes == null)
                return "The track file can't be read.";
            if (shared.Bytes.Length > CustomTracks.MaxSharedFileBytes)
                return "That track is too big to share (more than " + (CustomTracks.MaxSharedFileBytes / 1024) + " KB).";

            track = CustomTrackNumber;
            customId = info.Id;
            customName = CustomTracks.CleanText(info.Name, 40);
            customAuthor = info.Author;
            TrackSelection.CustomTrack = info;
            UnreadyEverybody();
            Sync();
            return null;
        }

        // another track: everybody confirms again
        void UnreadyEverybody()
        {
            localReady = false;
            foreach (RacePlayerInfo player in players)
                player.Ready = false;
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
                    if (!player.Ready || !player.HasTrack)
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
                CustomId = customId,
                CustomName = customName,
                CustomAuthor = customAuthor,
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
            info.HasTrack = update.HasTrack;
            info.Ready = update.Ready && update.HasTrack;
            Sync();
        }

        // a client asks for the custom track it does not have: send the file (only the one chosen now)
        void OnTrackRequest(NetworkGamer sender, System.IO.BinaryReader reader)
        {
            TrackRequestPacket request = TrackRequestPacket.Deserialize(reader);
            if (customId == null || request.Id != customId)
                return;

            SharedTrack shared = SharedTracks.Get(customId);
            if (shared == null || shared.Bytes == null)
                return;

            NetLog.Write("Sending track " + customName + " (" + shared.Bytes.Length + " bytes) to " + sender.Gamertag);
            net.Send(sender, new TrackFilePacket { Id = customId, Data = shared.Bytes }, true);
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
                CustomId = customId,
                CustomName = customName,
                CustomAuthor = customAuthor,
                Players = new List<RacePlayerInfo>(players),
            }, true);

            RaiseChanged();
        }

        #endregion

        #region Client

        void OnLobbySync(NetworkGamer sender, System.IO.BinaryReader reader)
        {
            LobbySyncPacket sync = LobbySyncPacket.Deserialize(reader);
            bool trackChanged = !synced || sync.Track != track || sync.CustomId != customId;
            track = sync.Track;
            laps = sync.Laps;
            customId = sync.Track == CustomTrackNumber ? sync.CustomId : null;
            customName = sync.CustomName ?? "";
            customAuthor = sync.CustomAuthor ?? "";
            players.Clear();
            players.AddRange(sync.Players);
            synced = true;

            RacePlayerInfo me = LocalPlayer;
            if (me != null)
                localReady = me.Ready;

            if (trackChanged)
                ResolveTrack();

            RaiseChanged();
        }

        // a client: the host has chosen a track. Is it here? If not, ask the host for the file.
        void ResolveTrack()
        {
            if (customId == null)
            {
                localHasTrack = true;
                trackStatus = null;
            }
            else
            {
                SharedTrack shared = SharedTracks.Resolve(customId);
                if (shared != null)
                {
                    localHasTrack = true;
                    trackStatus = null;
                    TrackSelection.CustomTrack = shared.Info;
                }
                else if (failedTracks.Contains(customId))
                {
                    localHasTrack = false;
                }
                else
                {
                    localHasTrack = false;
                    trackStatus = "Downloading the track...";
                    net.SendToHost(new TrackRequestPacket { Id = customId }, true);
                }
            }

            // (a track the player is still waiting for, or has just changed, is not "ready")
            localReady = false;
            if (LocalPlayer != null)
                SendLocal();
        }

        // a client: the file of the custom track has arrived (it is not trusted)
        void OnTrackFile(NetworkGamer sender, System.IO.BinaryReader reader)
        {
            TrackFilePacket file = TrackFilePacket.Deserialize(reader);
            if (customId == null || file.Id != customId || localHasTrack)
                return;     // not the track we are waiting for (or a repeat)

            string problem = null;
            CustomTrackInfo info = null;
            if (file.Data == null)
                problem = "The track is too big.";
            else
            {
                info = CustomTracks.FromBytes(file.Data, customName);
                if (info.Id != customId)
                    problem = "The track file does not match what the host announced.";
                else if (!info.IsPlayable)
                    problem = "The track can't be played: " + info.Error;
            }

            if (problem != null)
            {
                NetLog.Write("Track from the host rejected: " + problem);
                failedTracks.Add(customId);
                trackStatus = problem;
                localHasTrack = false;
            }
            else
            {
                SharedTrack shared = SharedTracks.AddReceived(file.Data, info);
                localHasTrack = true;
                trackStatus = null;
                TrackSelection.CustomTrack = shared.Info;
            }

            localReady = false;
            if (LocalPlayer != null)
                SendLocal();    // (a client joins the lobby when it has chosen its car, not before)
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
            net.Unregister(RacePacketType.TrackRequest);
            net.Unregister(RacePacketType.TrackFile);
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
