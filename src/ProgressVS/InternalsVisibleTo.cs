/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System.Runtime.CompilerServices;

#if SignAssembly
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Progress.UnitTests,PublicKey=002400000480000094000000060200000024000052534131000400000100010081b4345a022cc0f4b42bdc795a5a7a1623c1e58dc2246645d751ad41ba98f2749dc5c4e0da3a9e09febcb2cd5b088a0f041f8ac24b20e736d8ae523061733782f9c4cd75b44f17a63714aced0b29a59cd1ce58d8e10ccdb6012c7098c39871043b7241ac4ab9f6b34f183db716082cd57c1ff648135bece256357ba735e67dc6")]
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Progress.TestFramework,PublicKey=002400000480000094000000060200000024000052534131000400000100010081b4345a022cc0f4b42bdc795a5a7a1623c1e58dc2246645d751ad41ba98f2749dc5c4e0da3a9e09febcb2cd5b088a0f041f8ac24b20e736d8ae523061733782f9c4cd75b44f17a63714aced0b29a59cd1ce58d8e10ccdb6012c7098c39871043b7241ac4ab9f6b34f183db716082cd57c1ff648135bece256357ba735e67dc6")]
#else
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Progress.UnitTests")]
[assembly: InternalsVisibleTo("SonarLint.VisualStudio.Progress.TestFramework")]
#endif
