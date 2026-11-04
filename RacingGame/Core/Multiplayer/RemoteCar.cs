using Microsoft.Xna.Framework;

namespace RacingGame.Multiplayer
{
    /// <summary>
    /// A puppet: a car driven by another machine. It never goes through CarPhysics; it is drawn
    /// from the states received over the network, smoothed (as ShipGame's RemoteShipInterpolator
    /// does) by showing them a short delay behind real time and blending between two states.
    /// </summary>
    public sealed class RemoteCar
    {
        // how far behind the newest state the car is shown, in seconds
        public const double Delay = 0.1;
        // a jump bigger than this is a restart, not movement: snap to it
        public const float TeleportDistance = 100f;

        const int Capacity = 16;

        struct Snapshot
        {
            public double Time;             // local time the state was received
            public Vector3 Position;
            public Quaternion Rotation;
            public float WheelPos;
            public float Speed;
        }

        readonly Snapshot[] buffer = new Snapshot[Capacity];
        int count;                          // snapshots held, oldest first
        ushort lastSequence;
        bool hasSequence;

        public RemoteCar(RacePlayerInfo info)
        {
            Info = info;
        }

        public RacePlayerInfo Info { get; private set; }

        /// <summary>
        /// Laps completed and progress in the current lap, as of the newest state
        /// </summary>
        public int Lap { get; private set; }
        public float Progress { get; private set; }

        /// <summary>
        /// The newest known position, for placing a name tag
        /// </summary>
        public Vector3 LatestPosition { get { return count > 0 ? buffer[count - 1].Position : Vector3.Zero; } }

        /// <summary>
        /// True once at least one state has arrived
        /// </summary>
        public bool HasState { get { return count > 0; } }

        /// <summary>
        /// True if sequence a is newer than b (handles wraparound)
        /// </summary>
        public static bool IsNewer(ushort a, ushort b)
        {
            return a != b && (ushort)(a - b) < 0x8000;
        }

        /// <summary>
        /// Add a received state. Old or duplicate packets are ignored.
        /// </summary>
        public bool Add(CarStatePacket state, double now)
        {
            if (hasSequence && !IsNewer(state.Sequence, lastSequence))
                return false;

            hasSequence = true;
            lastSequence = state.Sequence;
            Lap = state.Lap;
            Progress = state.Progress;

            if (count > 0 &&
                Vector3.DistanceSquared(buffer[count - 1].Position, state.Position) > TeleportDistance * TeleportDistance)
                count = 0;

            if (count == Capacity)
            {
                for (int i = 1; i < Capacity; i++)
                    buffer[i - 1] = buffer[i];
                count--;
            }

            buffer[count++] = new Snapshot
            {
                Time = now,
                Position = state.Position,
                Rotation = Quaternion.Normalize(state.Rotation),
                WheelPos = state.WheelPos,
                Speed = state.Speed,
            };
            return true;
        }

        /// <summary>
        /// The render matrix and wheel position for the given local time (false while no state has arrived)
        /// </summary>
        public bool Sample(double now, out Matrix renderMatrix, out float wheelPos)
        {
            renderMatrix = Matrix.Identity;
            wheelPos = 0;
            if (count == 0)
                return false;

            double t = now - Delay;
            Snapshot newest = buffer[count - 1];
            Vector3 position;
            Quaternion rotation;

            if (t <= buffer[0].Time)
            {
                // older than everything we have: hold the oldest state
                position = buffer[0].Position;
                rotation = buffer[0].Rotation;
                wheelPos = buffer[0].WheelPos;
            }
            else if (t >= newest.Time)
            {
                // newer than everything we have: the car stays where it was last seen (a guess
                // at its direction would be wrong in every bend)
                position = newest.Position;
                rotation = newest.Rotation;
                wheelPos = newest.WheelPos;
            }
            else
            {
                // between two states: blend
                int i = count - 2;
                while (i > 0 && buffer[i].Time > t)
                    i--;

                Snapshot a = buffer[i];
                Snapshot b = buffer[i + 1];
                float span = (float)(b.Time - a.Time);
                float amount = span > 1e-6f ? (float)((t - a.Time) / span) : 1f;

                position = Vector3.Lerp(a.Position, b.Position, amount);
                rotation = Quaternion.Slerp(a.Rotation, b.Rotation, amount);
                wheelPos = MathHelper.Lerp(a.WheelPos, b.WheelPos, amount);
            }

            renderMatrix = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position);
            return true;
        }
    }
}
