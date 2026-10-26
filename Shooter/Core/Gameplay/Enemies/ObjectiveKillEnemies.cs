using System.Text.Json;
using Shooter.Gameplay.Combat;
using Shooter.Scene;

namespace Shooter.Gameplay.Enemies
{
    /// <summary>Port of Gameplay/Objectives/ObjectiveKillEnemies.cs with the scene's Inspector values.</summary>
    public class ObjectiveKillEnemies : Objective
    {
        public bool MustKillAllEnemies = true;
        public int KillsToCompleteObjective = 5;
        public int NotificationEnemiesRemainingThreshold = 3;

        private int _killTotal;

        public int KillTotal { get { return _killTotal; } }

        public ObjectiveKillEnemies(ObjectiveDto data, GameEvents events) : base(events)
        {
            JsonElement f = data.Fields != null ? JsonSerializer.SerializeToElement(data.Fields) : default(JsonElement);
            Title = FieldString(data, "Title");
            Description = FieldString(data, "Description");
            IsOptional = JsonFields.Bool(f, "IsOptional", false);
            DelayVisible = JsonFields.Float(f, "DelayVisible", 0f);
            MustKillAllEnemies = JsonFields.Bool(f, "MustKillAllEnemies", MustKillAllEnemies);
            KillsToCompleteObjective = JsonFields.Int(f, "KillsToCompleteObjective", KillsToCompleteObjective);
            NotificationEnemiesRemainingThreshold = JsonFields.Int(f, "NotificationEnemiesRemainingThreshold", NotificationEnemiesRemainingThreshold);
        }

        private static string FieldString(ObjectiveDto d, string name)
        {
            return d.Fields != null && d.Fields.TryGetValue(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        }

        public void Initialize()
        {
            Start();
        }

        protected override void Start()
        {
            // Objective.Start broadcasts the created event, then this script fills in a title / description when they are empty
            base.Start();
            Events.EnemyKilled += OnEnemyKilled;
            if (string.IsNullOrEmpty(Title))
            {
                Title = "Eliminate " + (MustKillAllEnemies ? "all the" : KillsToCompleteObjective.ToString()) + " enemies";
            }
            if (string.IsNullOrEmpty(Description))
            {
                Description = GetUpdatedCounterAmount();
            }
        }

        private void OnEnemyKilled(EnemyKillEvent evt)
        {
            if (IsCompleted)
            {
                return;
            }
            _killTotal++;
            if (MustKillAllEnemies)
            {
                KillsToCompleteObjective = evt.RemainingEnemyCount + _killTotal;
            }
            int targetRemaining = MustKillAllEnemies ? evt.RemainingEnemyCount : KillsToCompleteObjective - _killTotal;

            if (targetRemaining == 0)
            {
                CompleteObjective(string.Empty, GetUpdatedCounterAmount(), "Objective complete : " + Title);
            }
            else if (targetRemaining == 1)
            {
                string notificationText = NotificationEnemiesRemainingThreshold >= targetRemaining ? "One enemy left" : string.Empty;
                UpdateObjective(string.Empty, GetUpdatedCounterAmount(), notificationText);
            }
            else
            {
                string notificationText = NotificationEnemiesRemainingThreshold >= targetRemaining ? targetRemaining + " enemies to kill left" : string.Empty;
                UpdateObjective(string.Empty, GetUpdatedCounterAmount(), notificationText);
            }
        }

        public string GetUpdatedCounterAmount()
        {
            return _killTotal + " / " + KillsToCompleteObjective;
        }

        public void Detach()
        {
            Events.EnemyKilled -= OnEnemyKilled;
        }
    }
}
