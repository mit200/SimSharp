#region License Information

/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimSharp.Async;
using Xunit;

namespace SimSharp.Tests {
  public class ProcessAsyncTest {
    [Fact]
    public void TestStartNonProcess() {
      // Check that you cannot start a normal function.
      // This always holds due to the static-typed nature of C#
      // a process always expects an async IAsyncEnumerable<Event>
      Assert.True(true);
    }

    [Fact]
    public async Task TestGetStateAsync() {
      // A process is alive until it's generator has not terminated.
      var env = new Simulation();
      var procA = env.Process(GetStatePemAAsync(env));
      env.Process(GetStatePemBAsync(env, procA));
      await env.RunAsync();
    }

    private async IAsyncEnumerable<Event> GetStatePemAAsync(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(3));
      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> GetStatePemBAsync(Simulation env, ProcessAsync pemA) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      Assert.True(pemA.IsAlive);
      yield return env.Timeout(TimeSpan.FromSeconds(3));
      Assert.False(pemA.IsAlive);
      await Task.CompletedTask;
    }

    [Fact]
    public async void TestTarget() {
      var start = new DateTime(1970, 1, 1, 0, 0, 0);
      var delay = TimeSpan.FromSeconds(5);
      var env = new Simulation(start);
      var @event = env.Timeout(delay);
      var proc = env.Process(TargetPem(env, @event));
      while (env.Peek() < start + delay) {
        await env.Step();
      }

      Assert.Equal(@event, proc.Target);
      proc.Interrupt();
    }

    private async IAsyncEnumerable<Event> TargetPem(Simulation env, Event @event) {
      yield return @event;

      await Task.CompletedTask;
    }

    [Fact]
    public async void TestWaitForProc() {
      // A process can wait until another process finishes.
      var executed = false;
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(WaitForProcWaiter(env, () => executed = true));
      await env.RunAsync();
      Assert.True(executed);
    }

    private async IAsyncEnumerable<Event> WaitForProcFinisher(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(5));

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> WaitForProcWaiter(Simulation env, Action handle) {
      var proc = env.Process(WaitForProcFinisher(env));
      yield return proc; // Wait until "proc" finishes
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 5), env.Now);
      handle();

      await Task.CompletedTask;
    }

    [Fact]
    public async void TestExit() {
      // Processes can set a return value
      var executed = false;
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ExitParent(env, () => executed = true));
      await env.RunAsync();
      Assert.True(executed);
    }

    private async IAsyncEnumerable<Event> ExitChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      env.ActiveProcess.Succeed(env.Now);

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> ExitParent(Simulation env, Action handle) {
      var result1 = env.Process(ExitChild(env));
      yield return result1;
      var result2 = env.Process(ExitChild(env));
      yield return result2;

      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), result1.Value);
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 2), result2.Value);
      handle();

      await Task.CompletedTask;
    }

    [Fact]
    public async void TestReturnValue() {
      // Processes can set a return value
      var executed = false;
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ReturnValueParent(env, () => executed = true));
      await env.RunAsync();
      Assert.True(executed);
    }

    private async IAsyncEnumerable<Event> ReturnValueParent(Simulation env, Action handle) {
      var proc1 = env.Process(ReturnValueChild(env));
      yield return proc1;
      var proc2 = env.Process(ReturnValueChild(env));
      yield return proc2;
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), proc1.Value);
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 2), proc2.Value);
      handle();

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> ReturnValueChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      env.ActiveProcess.Succeed(env.Now);

      await Task.CompletedTask;
    }

    [Fact]
    public async void TestChildException() {
      // A child catches an exception and sends it to its parent.
      // This is the same as TestExit
      var executed = false;
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ChildExceptionParent(env, () => executed = true));
      await env.RunAsync();
      Assert.True(executed);
    }

    private async IAsyncEnumerable<Event> ChildExceptionChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      env.ActiveProcess.Succeed(new Exception("Onoes!"));

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> ChildExceptionParent(Simulation env, Action handle) {
      var child = env.Process(ChildExceptionChild(env));
      yield return child;
      Assert.IsAssignableFrom<Exception>(child.Value);
      handle();

      await Task.CompletedTask;
    }

    [Fact]
    public async void TestInterruptedJoin() {
      /* Tests that interrupts are raised while the victim is waiting for
         another process. The victim should get unregistered from the other
         process.
       */
      var executed = false;
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      var parent = env.Process(InterruptedJoinParent(env, () => executed = true));
      env.Process(InterruptedJoinInterruptor(env, parent));
      await env.RunAsync();
      Assert.True(executed);
    }

    private async IAsyncEnumerable<Event> InterruptedJoinInterruptor(Simulation env, ProcessBase process) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      process.Interrupt();

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> InterruptedJoinChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(2));

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> InterruptedJoinParent(Simulation env, Action handle) {
      var child = env.Process(InterruptedJoinChild(env));
      yield return child;
      if (env.ActiveProcess.HandleFault()) {
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), env.Now);
        Assert.True(child.IsAlive);
        // We should not get resumed when child terminates.
        yield return env.Timeout(TimeSpan.FromSeconds(5));
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 6), env.Now);
        handle();
      } else throw new NotImplementedException("Did not receive an interrupt.");

      await Task.CompletedTask;
    }

    [Fact]
    public async void TestInterruptedJoinAndRejoin() {
      // Tests that interrupts are raised while the victim is waiting for
      // another process. The victim tries to join again.
      var executed = false;
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      var parent = env.Process(InterruptedJoinAndRejoinParent(env, () => executed = true));
      env.Process(InterruptedJoinAndRejoinInterruptor(env, parent));
      await env.RunAsync();
      Assert.True(executed);
    }

    private async IAsyncEnumerable<Event> InterruptedJoinAndRejoinInterruptor(Simulation env, ProcessBase process) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      process.Interrupt();

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> InterruptedJoinAndRejoinChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(2));

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> InterruptedJoinAndRejoinParent(Simulation env, Action handle) {
      var child = env.Process(InterruptedJoinAndRejoinChild(env));
      yield return child;
      if (env.ActiveProcess.HandleFault()) {
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), env.Now);
        Assert.True(child.IsAlive);
        yield return child;
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 2), env.Now);
        handle();
      } else throw new NotImplementedException("Did not receive an interrupt.");

      await Task.CompletedTask;
    }

    [Fact]
    public async void TestUnregisterAfterInterrupt() {
      // If a process is interrupted while waiting for another one, it
      // should be unregistered from that process.
      var executed = false;
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      var parent = env.Process(UnregisterAfterInterruptParent(env, () => executed = true));
      env.Process(UnregisterAfterInterruptInterruptor(env, parent));
      await env.RunAsync();
      Assert.True(executed);
    }

    private async IAsyncEnumerable<Event> UnregisterAfterInterruptInterruptor(Simulation env, ProcessBase process) {
      yield return env.Timeout(TimeSpan.FromSeconds(1));
      process.Interrupt();

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> UnregisterAfterInterruptChild(Simulation env) {
      yield return env.Timeout(TimeSpan.FromSeconds(2));

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> UnregisterAfterInterruptParent(Simulation env, Action handle) {
      var child = env.Process(UnregisterAfterInterruptChild(env));
      yield return child;
      if (env.ActiveProcess.HandleFault()) {
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 1), env.Now);
        Assert.True(child.IsAlive);
      } else throw new NotImplementedException("Did not receive an interrupt.");

      yield return env.Timeout(TimeSpan.FromSeconds(2));
      Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 3), env.Now);
      Assert.False(child.IsAlive);
      handle();

      await Task.CompletedTask;
    }

    [Fact]
    public async void TestErrorAndInterruptedJoin() {
      var executed = false;
      var env = new Simulation(new DateTime(1970, 1, 1, 0, 0, 0));
      env.Process(ErrorAndInterruptedJoinParent(env, () => executed = true));
      await env.RunAsync();
      Assert.True(executed);
    }

    private async IAsyncEnumerable<Event> ErrorAndInterruptedJoinChildA(Simulation env, ProcessBase process) {
      process.Interrupt("InterruptA");
      env.ActiveProcess.Succeed();
      yield return env.Timeout(TimeSpan.FromSeconds(1));

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> ErrorAndInterruptedJoinChildB(Simulation env) {
      env.ActiveProcess.Fail("spam");
      yield return env.Timeout(TimeSpan.FromSeconds(1));

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> ErrorAndInterruptedJoinParent(Simulation env, Action handle) {
      env.Process(ErrorAndInterruptedJoinChildA(env, env.ActiveProcess));
      var b = env.Process(ErrorAndInterruptedJoinChildB(env));
      yield return b;
      if (env.ActiveProcess.HandleFault())
        Assert.Equal("InterruptA", env.ActiveProcess.Value);
      yield return env.Timeout(TimeSpan.FromSeconds(0));
      if (env.ActiveProcess.HandleFault())
        throw new NotImplementedException("process should not react.");
      handle();

      await Task.CompletedTask;
    }

    [Fact]
    public async void TestYieldFailedProcess() {
      var env = new Simulation(defaultStep: TimeSpan.FromMinutes(1));
      var proc = env.Process(Proc(env));
      env.Process(Proc1(env, proc));
      var p2 = env.Process(Proc2(env, proc));
      Assert.Throws<InvalidOperationException>(() => env.Run());
      await env.RunAsync();
      Assert.Equal(42, (int) p2.Value);
    }

    private async IAsyncEnumerable<Event> Proc(Simulation env) {
      yield return env.Timeout(TimeSpan.FromMinutes(10));
      env.ActiveProcess.Fail();

      await Task.CompletedTask;
    }

    private async IAsyncEnumerable<Event> Proc1(Simulation env, ProcessBase dep) {
      yield return env.Timeout(TimeSpan.FromMinutes(20));
      yield return dep;
      yield return env.Timeout(TimeSpan.FromMinutes(5));
      await Task.CompletedTask;
      throw new NotImplementedException("process should not be able to continue");
    }

    private async IAsyncEnumerable<Event> Proc2(Simulation env, ProcessBase dep) {
      yield return env.Timeout(TimeSpan.FromMinutes(20));
      yield return dep;
      env.ActiveProcess.HandleFault();
      yield return env.Timeout(TimeSpan.FromMinutes(5));
      env.ActiveProcess.Succeed(42);

      await Task.CompletedTask;
    }

    [Fact]
    public async void TestPrioritizedProcesses() {
      var env = new Simulation(defaultStep: TimeSpan.FromMinutes(1));
      var order = new List<int>();
      for (var p = 5; p >= -5; p--) {
        // processes are created such that lowest priority process is first
        env.Process(PrioritizedProcess(env, p, order), p);
      }

      await env.RunAsync();
      Assert.Equal(11, order.Count);
      // processes must be executed such that highest priority process is started first
      Assert.Equal(new[] {-5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5}, order);
    }

    private async IAsyncEnumerable<Event> PrioritizedProcess(Simulation env, int prio, List<int> order) {
      order.Add(prio);
      await Task.CompletedTask;
      yield break;
    }
  }
}