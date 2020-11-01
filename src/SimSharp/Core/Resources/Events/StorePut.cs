#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  public class StorePut : Event {
    public DateTime Time { get; private set; }
    public ProcessBase Owner { get; set; }

    public StorePut(Simulation environment, Action<Event> callback, object value)
      : base(environment) {
      if (value == null) throw new ArgumentNullException("value", "Value to put in a Store cannot be null.");
      CallbackList.Add(new EventAction(callback));
      Value = value;
      Time = environment.Now;
      Owner = environment.ActiveProcess;
    }
  }
}
