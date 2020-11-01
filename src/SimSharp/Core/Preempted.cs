#region License Information
/*
 * This file is part of SimSharp which is licensed under the MIT license.
 * See the LICENSE file in the project root for more information.
 */
#endregion

using System;

namespace SimSharp {
  public sealed class Preempted {
    public ProcessBase By { get; private set; }
    public DateTime UsageSince { get; private set; }

    public Preempted(ProcessBase by, DateTime usageSince) {
      By = by;
      UsageSince = usageSince;
    }
  }
}
