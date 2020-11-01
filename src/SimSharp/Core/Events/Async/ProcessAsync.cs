using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimSharp.Async {
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
  public class ProcessAsync : ProcessBase {
    private readonly IAsyncEnumerator<Event> generator;

    /// <summary>
    /// Sets up a new process.
    /// The process places an initialize event into the event queue which starts
    /// the process by retrieving events from the generator.
    /// </summary>
    /// <param name="environment">The environment in which the process lives.</param>
    /// <param name="generator">The generator function of the process.</param>
    /// <param name="priority">The priority if multiple processes are started at the same time.</param>
    public ProcessAsync(Simulation environment, IAsyncEnumerable<Event> generator, int priority = 0)
      : base(environment, priority) {
      this.generator = generator.GetAsyncEnumerator();
    }

    protected override async Task Resume(Event @event) {
      Environment.ActiveProcess = this;
      while (true) {
        if (@event.IsOk) {
          if (await generator.MoveNextAsync()) {
            if (IsTriggered) {
              // the generator called e.g. Environment.ActiveProcess.Fail
              Environment.ActiveProcess = null;
              return;
            }
            if (!ProceedToEvent()) {
              @event = Target;
              continue;
            } else break;
          } else if (!IsTriggered) {
            Succeed(@event.Value);
            break;
          } else break;
        } else {
          /* Fault handling differs from SimPy as in .NET it is not possible to inject an
         * exception into an enumerator and it is impossible to put a yield return inside
         * a try-catch block. In SimSharp the Process will set IsOk and will then move to
         * the next yield in the generator. However, if after this move IsOk is still false
         * we know that the error was not handled. It is assumed the error is handled if
         * HandleFault() is called on the environment's ActiveProcess which will reset the
         * flag. */
          IsOk = false;
          Value = @event.Value;

          if (await generator.MoveNextAsync()) {
            if (IsTriggered) {
              // the generator called e.g. Environment.ActiveProcess.Fail
              Environment.ActiveProcess = null;
              return;
            }
            // if we move next, but IsOk is still false
            if (!IsOk) throw new InvalidOperationException("The process did not react to being faulted.");
            // otherwise HandleFault was called and the fault was handled
            if (ProceedToEvent()) break;
          } else if (!IsTriggered) {
            if (!IsOk) Fail(@event.Value);
            else Succeed(@event.Value);
            break;
          } else break;
        }
      }
      Environment.ActiveProcess = null;
    }
    
    protected override bool ProceedToEvent() {
      Target = generator.Current;
      Value = Target.Value;
      if (Target.IsProcessed) return false;
      Target.AddCallback(Resume);
      return true;
    }
  }

  public class PseudoRealtimeProcessAsync : ProcessAsync {
    public double RealtimeScale { get; set; }
    public new PseudoRealtimeSimulation Environment {
      get { return (PseudoRealtimeSimulation)base.Environment; }
    }

    /// <summary>
    /// Sets up a new process.
    /// The process places an initialize event into the event queue which starts
    /// the process by retrieving events from the generator.
    /// </summary>
    /// <param name="environment">The environment in which the process lives.</param>
    /// <param name="generator">The generator function of the process.</param>
    /// <param name="priority">The priority if multiple processes are started at the same time.</param>
    /// <param name="realtimeScale">A value strictly greater than 0 used to scale real time events (1 = realtime).</param>
    public PseudoRealtimeProcessAsync(PseudoRealtimeSimulation environment, IAsyncEnumerable<Event> generator, int priority = 0, double realtimeScale = PseudoRealtimeSimulation.DefaultRealtimeScale)
      : base(environment, generator, priority) {
      RealtimeScale = realtimeScale;
    }

    protected override Task Resume(Event @event) {
      Environment.SetRealtime(RealtimeScale);
      return base.Resume(@event);
    }
  }
}