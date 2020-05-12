﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;

namespace SonarLint.VisualStudio.Core
{
    public class EnvironmentSettings : IEnvironmentSettings
    {
        internal const string TreatBlockerAsErrorEnvVar = "SONAR_INTERNAL_TREAT_BLOCKER_AS_ERROR";
        internal const string CFamilyAnalysisTimeoutEnvVar = "SONAR_INTERNAL_CFAMILY_ANALYSIS_TIMEOUT_MS";
        public const string SonarLintDownloadUrlEnvVar = "SONARLINT_DAEMON_DOWNLOAD_URL";

        public bool TreatBlockerSeverityAsError()
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable(TreatBlockerAsErrorEnvVar), out var result))
            {
                return result;
            }
            return false;
        }

        public int CFamilyAnalysisTimeoutInMs()
        {
            var setting = Environment.GetEnvironmentVariable(CFamilyAnalysisTimeoutEnvVar);

            if (int.TryParse(setting, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo, out int userSuppliedTimeout)
                && userSuppliedTimeout > 0)
            {
                return userSuppliedTimeout;
            }

            return 0;
        }

        public string SonarLintDaemonDownloadUrl()
        {
            // The URL validation and logging is being done by the daemon installer, so
            // this is just a passthrough
            return Environment.GetEnvironmentVariable(SonarLintDownloadUrlEnvVar);
        }
    }
}