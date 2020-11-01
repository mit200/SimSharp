using System;

namespace SimSharp {
  public class EventAction : IEventAction<EventAction> {
    public Action<Event> Event { get; }
    
    public EventAction(Action<Event> @event) {
      Event = @event;
    }

    public bool Equals(EventAction other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Equals(Event, other.Event);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != this.GetType()) return false;
      return Equals((EventAction) obj);
    }

    public override int GetHashCode()
    {
      return (Event != null ? Event.GetHashCode() : 0);
    }
  }
}