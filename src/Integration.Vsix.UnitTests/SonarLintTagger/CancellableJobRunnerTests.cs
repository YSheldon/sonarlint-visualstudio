﻿/*
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
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger
{
    [TestClass]
    public class CancellableJobRunnerTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void NoOperations_NoErrors()
        {
            // Arrange
            var testLogger = new TestLogger(logToConsole: true);
            TestContext.WriteLine($"Test executing on thread {Thread.CurrentThread.ManagedThreadId}");

            // Act
            var testSubject = CancellableJobRunner.Start("my job", new Action[] { }, testLogger);
            WaitForRunnerToFinish(testSubject);

            // Assert
            testSubject.State.Should().Be(CancellableJobRunner.RunnerState.Finished);            
        }

        [TestMethod]
        public void AllOperationsExecuted()
        {
            // Arrange
            var testLogger = new TestLogger(logToConsole: true);
            TestContext.WriteLine($"Test executing on thread {Thread.CurrentThread.ManagedThreadId}");

            bool op1Executed = false, op2Executed = false;
            int operationThreadId = -1;
            
            CancellableJobRunner testSubject = null;

            Action op1 = () =>
            {
                TestContext.WriteLine($"Executing op1 on thread {Thread.CurrentThread.ManagedThreadId}");
                testSubject.State.Should().Be(CancellableJobRunner.RunnerState.Running);

                op1Executed = true;
                operationThreadId = Thread.CurrentThread.ManagedThreadId;
            };

            Action op2 = () => op2Executed = true;

            // Act
            testSubject = CancellableJobRunner.Start("my job", new[] { op1, op2 }, testLogger);
            WaitForRunnerToFinish(testSubject);
            
            // Assert
            testSubject.State.Should().Be(CancellableJobRunner.RunnerState.Finished);

            op1Executed.Should().BeTrue();
            op2Executed.Should().BeTrue();

            operationThreadId.Should().NotBe(Thread.CurrentThread.ManagedThreadId);
        }

        [TestMethod]
        public void CancelAfterFirstOperation()
        {
            // Arrange
            var testLogger = new TestLogger(logToConsole: true);
            TestContext.WriteLine($"Test executing on thread {Thread.CurrentThread.ManagedThreadId}");

            bool op1Executed = false, op2Executed = false;
            CancellableJobRunner testSubject = null;

            Action op1 = () =>
            {
                TestContext.WriteLine($"Executing op1 on thread {Thread.CurrentThread.ManagedThreadId}");
                op1Executed = true;

                testSubject.Cancel();
            };

            Action op2 = () =>
            {
                TestContext.WriteLine($"Executing op2 on thread {Thread.CurrentThread.ManagedThreadId}");
                op2Executed = true;
            };

            // Act
            testSubject = CancellableJobRunner.Start("my job", new[] { op1, op2 }, testLogger);


            WaitForRunnerToFinish(testSubject);

            // Other checks
            testSubject.State.Should().Be(CancellableJobRunner.RunnerState.Cancelled);
            op1Executed.Should().BeTrue();
            op2Executed.Should().BeFalse();
        }

        [TestMethod]
        public void ExceptionInOperationPreventsSubsequentOperations()
        {
            // Arrange
            var testLogger = new TestLogger(logToConsole: true);
            TestContext.WriteLine($"Test executing on thread {Thread.CurrentThread.ManagedThreadId}");

            bool op1Executed = false, op2Executed = false;

            Action op1 = () =>
            {
                TestContext.WriteLine($"Executing op1 on thread {Thread.CurrentThread.ManagedThreadId}");
                op1Executed = true;
                throw new InvalidOperationException("XXX YYY");
            };

            Action op2 = () =>
            {
                TestContext.WriteLine($"Executing op2 on thread {Thread.CurrentThread.ManagedThreadId}");
                op2Executed = true;
            };

            // Act
            var testSubject = CancellableJobRunner.Start("my job", new[] { op1, op2 }, testLogger);
            WaitForRunnerToFinish(testSubject);

            // Other checks
            testSubject.State.Should().Be(CancellableJobRunner.RunnerState.Faulted);
            testLogger.AssertPartialOutputStringExists("XXX YYY");

            op1Executed.Should().BeTrue();
            op2Executed.Should().BeFalse();
        }

        private static void WaitForRunnerToFinish(CancellableJobRunner runner)
        {
            int timeout = System.Diagnostics.Debugger.IsAttached ? 20000 : 3000;

            try
            {
                runner.TestingWaitHandle?.WaitOne(timeout);
            }
            catch (ObjectDisposedException)
            {
                // If the runner has finished then the token source will have been disposed
            }
        }

    }
}