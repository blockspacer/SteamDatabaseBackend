﻿/*
 * Copyright (c) 2013-present, SteamDB. All rights reserved.
 * Use of this source code is governed by a BSD-style license that can be
 * found in the LICENSE file.
 */

using System;
using System.Threading;

namespace SteamDatabaseBackend
{
    class Watchdog : IDisposable
    {
        public Timer Timer { get; private set; }

        public Watchdog()
        {
            Timer = new Timer(OnTimer, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20));
        }

        public void Dispose()
        {
            if (Timer != null)
            {
                Timer.Dispose();
                Timer = null;
            }
        }

        private void OnTimer(object state)
        {
            if (Settings.Current.IRC.Enabled && !IRC.Instance.IsConnected)
            {
                Log.WriteWarn("Watchdog", "Forcing IRC reconnect.");

                IRC.Instance.Connect();
            }

            if (Steam.Instance.Client.IsConnected && Steam.Instance.IsLoggedIn)
            {
                AccountInfo.Sync();

                if (DateTime.Now.Subtract(Connection.LastSuccessfulLogin).TotalMinutes >= 1.0)
                {
                    IRC.Instance.SendOps("[Watchdog] Forcing a new changelist request.");

                    Log.WriteWarn("Watchdog", "Forcing a new changelist request.");

                    Steam.Instance.Apps.PICSGetChangesSince(Steam.Instance.PICSChanges.PreviousChangeNumber, true, true);
                }
            }
            else if (DateTime.Now.Subtract(Connection.LastSuccessfulLogin).TotalMinutes >= 5.0)
            {
                IRC.Instance.SendOps("[Watchdog] Forcing a Steam reconnect.");

                Log.WriteWarn("Watchdog", "Forcing a Steam reconnect.");

                Connection.Reconnect(null, null);
            }
        }
    }
}
