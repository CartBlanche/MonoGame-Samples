using System;

namespace Shooter.Gameplay.Combat
{
    public class EnemyKillEvent
    {
        public object Enemy;
        public int RemainingEnemyCount;
    }

    public class ObjectiveUpdateEvent
    {
        public Objective Objective;
        public string DescriptionText;
        public string CounterText;
        public bool IsComplete;
        public string NotificationText;
    }

    /// <summary>The events of Game/Events.cs that exist so far (Unity's global EventManager, as instance events). Player death / pickup / damage events arrive with 3d.</summary>
    public class GameEvents
    {
        public event Action<EnemyKillEvent> EnemyKilled;
        public event Action<ObjectiveUpdateEvent> ObjectiveUpdated;
        public event Action<Objective> ObjectiveCreated;
        public event Action<Objective> ObjectiveCompleted;
        public event Action AllObjectivesCompleted;

        public void Raise(EnemyKillEvent e) { if (EnemyKilled != null) EnemyKilled(e); }
        public void Raise(ObjectiveUpdateEvent e) { if (ObjectiveUpdated != null) ObjectiveUpdated(e); }
        public void RaiseCreated(Objective o) { if (ObjectiveCreated != null) ObjectiveCreated(o); }
        public void RaiseCompleted(Objective o) { if (ObjectiveCompleted != null) ObjectiveCompleted(o); }
        public void RaiseAllCompleted() { if (AllObjectivesCompleted != null) AllObjectivesCompleted(); }
    }

    /// <summary>Port of Game/Shared/Objective.cs.</summary>
    public abstract class Objective
    {
        public string Title;
        public string Description;
        public bool IsOptional;
        public float DelayVisible;
        public bool IsCompleted { get; private set; }
        public bool IsBlocking() { return !(IsOptional || IsCompleted); }

        protected readonly GameEvents Events;

        protected Objective(GameEvents events)
        {
            Events = events;
        }

        protected virtual void Start()
        {
            Events.RaiseCreated(this);   // the "display message" broadcast of Objective.Start is drawn by the HUD from the title
        }

        public void UpdateObjective(string descriptionText, string counterText, string notificationText)
        {
            Events.Raise(new ObjectiveUpdateEvent { Objective = this, DescriptionText = descriptionText, CounterText = counterText, NotificationText = notificationText, IsComplete = IsCompleted });
        }

        public void CompleteObjective(string descriptionText, string counterText, string notificationText)
        {
            IsCompleted = true;
            Events.Raise(new ObjectiveUpdateEvent { Objective = this, DescriptionText = descriptionText, CounterText = counterText, NotificationText = notificationText, IsComplete = true });
            Events.RaiseCompleted(this);
        }
    }
}
