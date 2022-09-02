﻿using Immense.RemoteControl.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Immense.RemoteControl.Server.Abstractions
{
    /// <summary>
    /// Contains functionality that needs to be implemented outside of the remote control process.
    /// </summary>
    public interface IHubEventHandler
    {
        /// <summary>
        /// This is called when the remote control session ends unexpectedly from the desktop
        /// side, and the viewer is expecting it to restart automatically.
        /// </summary>
        /// <param name="sessionInfo"></param>
        /// <param name="viewerList">
        ///    This is the list of viewer SignalR connection IDs.  These should be comma-delimited
        ///    and passed into the new remote control process with the --viewer param, and they will
        ///    be signaled to automatically reconnect when the new session is ready.
        /// </param>
        /// <returns></returns>
        Task RestartScreenCaster(RemoteControlSession sessionInfo, HashSet<string> viewerList);

        /// <summary>
        /// This will be called when an unattended session is ready for a viewer to connect.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="relativeAccessUrl">
        ///   The relative URL, including the session ID and access key, that should be used to
        ///   connect to the session.
        /// </param>
        /// <returns></returns>
        Task NotifyUnattendedSessionReady(RemoteControlSession session, string relativeAccessUrl);

        /// <summary>
        /// This is called when a viewer has selected a different Windows session.  A new remote control
        /// process should be started in that session.  The viewer's connection ID should be passed into the
        /// new process using the --viewers argument, and they'll be automatically signaled when the new
        /// session is ready.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="viewerConnectionId"></param>
        /// <param name="targetWindowsSession"></param>
        /// <returns></returns>
        Task ChangeWindowsSession(RemoteControlSession session, string viewerConnectionId, int targetWindowsSession);
    }
}
