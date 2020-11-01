using System;
using System.Threading.Tasks;

namespace SimSharp {
  /// <summary>
  /// A Process handles the iteration of events. Processes may define steps that
  /// a certain entity in the simulation has to perform. Each time the process
  /// should wait it yields an event and will be resumed when that event is processed.
  /// </summary>
  /// <remarks>
  /// Since an iterator method does not have access to its process, the method can
  /// retrieve the associated Process through the ActiveProcess property of the
  /// environment. Each Process sets and resets that property during Resume.
  /// </remarks>
  public abstract class ProcessBase : Event {
    private Event target;
    /// <summary>
    /// Target is the event that is expected to be executed next in the process.
    /// </summary>
    public Event Target {
      get { return target; }
      protected set { target = value; }
    }

    /// <summary>
    /// Sets up a new process.
    /// The process places an initialize event into the event queue which starts
    /// the process by retrieving events from the generator.
    /// </summary>
    /// <param name="environment">The environment in which the process lives.</param>
    /// <param name="priority">The priority if multiple processes are started at the same time.</param>
    public ProcessBase(Simulation environment, int priority = 0)
      : base(environment) {
      IsOk = true;
      target = new Initialize(environment, this, priority);
    }

    /// <summary>
    /// This interrupts a process and causes the IsOk flag to be set to false.
    /// If a process is interrupted the iterator method needs to call HandleFault()
    /// before continuing to yield further events.
    /// </summary>
    /// <exception cref="InvalidOperationException">This is thrown in three conditions:
    ///  - If the process has already been triggered.
    ///  - If the process attempts to interrupt itself.
    ///  - If the process continues to yield events despite being faulted.</exception>
    /// <param name="cause">The cause of the interrupt.</param>
    /// <param name="priority">The priority to rank events at the same time (smaller value = higher priority).</param>
    public virtual void Interrupt(object cause = null, int priority = 0) {
      if (IsTriggered) throw new InvalidOperationException("The process has terminated and cannot be interrupted.");
      if (Environment.ActiveProcess == this) throw new InvalidOperationException("A process is not allowed to interrupt itself.");

      var interruptEvent = new Event(Environment);
      interruptEvent.AddCallback(Resume);
      interruptEvent.Fail(cause, priority);

      Target?.RemoveCallback(Resume);
    }

    protected abstract Task Resume(Event @event);

    protected abstract bool ProceedToEvent();

    /// <summary>
    /// This method must be called to reset the IsOk flag of the process back to true.
    /// The IsOk flag may be set to false if the process waited on an event that failed.
    /// </summary>
    /// <remarks>
    /// In SimPy a faulting process would throw an exception which is then catched and
    /// chained. In SimSharp catching exceptions from a yield is not possible as a yield
    /// return statement may not throw an exception.
    /// If a processes faulted the Value property may indicate a cause for the fault.
    /// </remarks>
    /// <returns>True if a faulting situation needs to be handled, false if the process
    /// is okay and the last yielded event succeeded.</returns>
    public virtual bool HandleFault() {
      if (IsOk) return false;
      IsOk = true;
      return true;
    }

    private class Initialize : Event {
      public Initialize(Simulation environment, ProcessBase process, int priority)
        : base(environment) {
        CallbackList.Add(new EventActionAsync(process.Resume));
        IsOk = true;
        IsTriggered = true;
        environment.Schedule(this, priority);
      }
    }
  }
}