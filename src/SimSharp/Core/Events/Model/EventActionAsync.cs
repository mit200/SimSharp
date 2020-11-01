using System;
using System.Threading.Tasks;

namespace SimSharp {
  public class EventActionAsync : IEventAction<EventActionAsync> {
    public Func<Event, Task> Event { get; }
    
    public EventActionAsync(Func<Event, Task> @event) {
      Event = @event;
    }

    public bool Equals(EventActionAsync other)
    {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Equals(Event, other.Event);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != GetType()) return false;
      return Equals((EventActionAsync) obj);
    }

    public override int GetHashCode()
    {
      return (Event != null ? Event.GetHashCode() : 0);
    }
  }
}