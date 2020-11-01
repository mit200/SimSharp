using System;

namespace SimSharp {
  public interface IEventAction<TEventAction> : IEventAction, IEquatable<TEventAction> {
    
  }

  public interface IEventAction {
    
  }
}