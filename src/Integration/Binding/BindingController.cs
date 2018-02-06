/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Client.CommandTarget;
using Microsoft.VisualStudio.ComponentModelHost;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// A dedicated controller for the <see cref="BindCommand"/>
    /// </summary>
    internal class BindingController : HostedCommandControllerBase, IBindingWorkflowExecutor
    {
        private readonly IHost host;
        private readonly IBindingWorkflowExecutor workflowExecutor;
        private readonly IProjectSystemHelper projectSystemHelper;

        public BindingController(IHost host)
            : this(host, null)
        {
        }

        internal /*for testing purposes*/ BindingController(IHost host, IBindingWorkflowExecutor workflowExecutor)
            : base(host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.host = host;

            this.BindCommand = new RelayCommand<BindCommandArgs>(this.OnBind, this.OnBindStatus);
            this.workflowExecutor = workflowExecutor ?? this;
            this.projectSystemHelper = this.host.GetService<IProjectSystemHelper>();
            this.projectSystemHelper.AssertLocalServiceIsNotNull();
        }

        #region Commands
        public RelayCommand<BindCommandArgs> BindCommand { get; }

        internal /*for testing purposes*/ bool IsBindingInProgress
        {
            get
            {
                return this.host.VisualStateManager.IsBusy;
            }
            private set
            {
                if (this.host.VisualStateManager.IsBusy != value)
                {
                    this.host.VisualStateManager.IsBusy = value;
                    this.BindCommand.RequeryCanExecute();
                }
            }
        }

        protected override int OnQueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // Using just as a means that indicates that the status was invalidated and it needs to be recalculate
            // in response to IVsUIShell.UpdateCommandUI which is triggered for the various UI context changes
            this.BindCommand.RequeryCanExecute();

            return base.OnQueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        private bool OnBindStatus(BindCommandArgs args)
        {
            return args != null
                && args.ProjectKey != null
                && this.host.VisualStateManager.IsConnected
                && !this.host.VisualStateManager.IsBusy
                && VsShellUtils.IsSolutionExistsAndFullyLoaded()
                && VsShellUtils.IsSolutionExistsAndNotBuildingAndNotDebugging()
                && (this.projectSystemHelper.GetSolutionProjects()?.Any() ?? false);
        }

        private void OnBind(BindCommandArgs args)
        {
            Debug.Assert(this.OnBindStatus(args));

            var componentModel = host.GetService<SComponentModel, IComponentModel>();
            TelemetryLoggerAccessor.GetLogger(componentModel)?.ReportEvent(TelemetryEvent.BindCommandCommandCalled);

            SonarQubeProject projectInformation = new SonarQubeProject(args.ProjectKey, args.ProjectName);

            this.workflowExecutor.BindProject(projectInformation, args.Connection);
        }

        #endregion

        #region IBindingWorkflowExecutor

        void IBindingWorkflowExecutor.BindProject(SonarQubeProject projectInformation, ConnectionInformation connection)
        {
            //TODO: CM2 - choose the type of binding
            var workflow = new BindingWorkflow(this.host, connection, projectInformation);

            IProgressEvents progressEvents = workflow.Run();
            Debug.Assert(progressEvents != null, "BindingWorkflow.Run returned null");
            this.SetBindingInProgress(progressEvents, projectInformation, connection);
        }

        internal /*for testing purposes*/ void SetBindingInProgress(IProgressEvents progressEvents, SonarQubeProject projectInformation, ConnectionInformation connection)
        {
            this.OnBindingStarted();

            ProgressNotificationListener progressListener = new ProgressNotificationListener(progressEvents, this.host.Logger);
            progressListener.MessageFormat = Strings.BindingSolutionPrefixMessageFormat;

            progressEvents.RunOnFinished(result =>
            {
                progressListener.Dispose();

                this.OnBindingFinished(projectInformation, connection, result == ProgressControllerResult.Succeeded);
            });
        }

        private void OnBindingStarted()
        {
            this.IsBindingInProgress = true;
            this.host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.FailedToBindId);
        }

        private void OnBindingFinished(SonarQubeProject projectInformation, ConnectionInformation connection, bool isFinishedSuccessfully)
        {
            this.IsBindingInProgress = false;
            this.host.VisualStateManager.ClearBoundProject();

            if (isFinishedSuccessfully)
            {
                this.host.VisualStateManager.SetBoundProject(connection.ServerUri, connection.Organization?.Key, projectInformation.Key);

                // TODO: CM2: the conflicts controller is only applicable in legacy connected mode
                // However, it *should* be safe to call it regardless - in new connected mode it should
                // return false.
                var conflictsController = this.host.GetService<IRuleSetConflictsController>();
                conflictsController.AssertLocalServiceIsNotNull();

                if (conflictsController.CheckForConflicts())
                {
                    // In some cases we will end up navigating to the solution explorer, this will make sure that
                    // we're back in team explorer to view the conflicts
                    this.host.GetMefService<ITeamExplorerController>()?.ShowSonarQubePage();
                }
                else
                {
                    VsShellUtils.ActivateSolutionExplorer(this.host);
                }
            }
            else
            {
                IUserNotification notifications = this.host.ActiveSection?.UserNotifications;
                if (notifications != null)
                {
                    // Create a command with a fixed argument with the help of ContextualCommandViewModel that creates proxy command for the contextual (fixed) instance and the passed in ICommand that expects it
                    var rebindCommandVM = new ContextualCommandViewModel(
                        projectInformation,
                        new RelayCommand<BindCommandArgs>(this.OnBind, this.OnBindStatus),
                        new BindCommandArgs(projectInformation.Key, projectInformation.Name, connection));
                    notifications.ShowNotificationError(Strings.FailedToToBindSolution, NotificationIds.FailedToBindId, rebindCommandVM.Command);
                }
            }
        }
        #endregion
    }
}
