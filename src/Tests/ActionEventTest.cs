using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Xunit;

namespace SimSharp.Tests {
  public class ActionEventTest {
    
    [Fact]
    public void TestEventActionNotEqual() {
      IEventAction action1 = new EventAction(TestAction1);
      IEventAction action2 = new EventAction(TestAction2);
      Assert.NotEqual(action1, action2);
      
      action1 = new EventActionAsync(TestActionAsync1);
      action2 = new EventActionAsync(TestActionAsync2);
      Assert.NotEqual(action1, action2);
      
      action1 = new EventAction(TestAction1);
      action2 = new EventActionAsync(TestActionAsync1);
      Assert.NotEqual(action1, action2);
    }
    
    [Fact]
    public void TestEventActionEqual() {
      IEventAction action1 = new EventAction(TestAction1);
      IEventAction action2 = new EventAction(TestAction1);
      Assert.Equal(action1, action2);
      
      action1 = new EventAction(TestAction2);
      action2 = new EventAction(TestAction2);
      Assert.Equal(action1, action2);
      
      action1 = new EventActionAsync(TestActionAsync1);
      action2 = new EventActionAsync(TestActionAsync1);
      Assert.Equal(action1, action2);
      
      action2 = new EventActionAsync(TestActionAsync2);
      action1 = new EventActionAsync(TestActionAsync2);
      Assert.Equal(action1, action2);
    }
    
    private static Task TestActionAsync1(Event @event) {
      return Task.CompletedTask;
    }

    private static Task TestActionAsync2(Event @event) {
      return Task.CompletedTask;
    }

    private static void TestAction1(Event @event) {
    }

    private static void TestAction2(Event @event) {
    }
  }
}