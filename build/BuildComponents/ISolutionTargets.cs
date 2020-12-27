using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.XPath;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.ValueInjection.ValueInjectionUtility;

namespace BuildComponents
{
    public interface ISolutionTargets : IBaseParametersAndPaths
    {

        [PathExecutable]
        Tool Pac => TryGetValue(() => Pac);

        [PackageExecutable(
            "Microsoft.CrmSdk.CoreTools",
            "SolutionPackager.exe")]
        Tool SolutionPackager => TryGetValue(() => SolutionPackager);

        Target ExtractSolution => _ => _
        .Executes(() =>
        {
            var solutionConfig = GetSolutionConfig(DataverseSolution);

            SetActivePacProfile(solutionConfig.MasterProfile);

            var outputDirectory = SolutionDirectory / ".tmp";
            EnsureExistingDirectory(outputDirectory);
            var managedSolutionPath = outputDirectory / $"{DataverseSolution}_managed.zip";
            var unmanagedSolutionPath = outputDirectory / $"{DataverseSolution}.zip";

            Pac($"solution export -p { managedSolutionPath } -n { DataverseSolution } -a -m");
            Pac($"solution export -p { unmanagedSolutionPath } -n { DataverseSolution } -a");

            var metadataFolder = SolutionDirectory / "Extract";
            var mappingFilePath = SolutionDirectory / "ExtractMappingFile.xml";
            SolutionPackager($"/action:Extract /zipfile:{unmanagedSolutionPath} /folder:{ metadataFolder } /packagetype:Both  /allowdelete:Yes /map:{ mappingFilePath }");

            DeleteDirectory(outputDirectory);
        });

        Target PackSolution => _ => _
            .Executes(() =>
            {
                DotNetBuild(s => s
                    .SetProjectFile(SolutionDirectory / $"{DataverseSolution}.cdsproj")
                    .SetConfiguration(SolutionType == SolutionType.Unmanaged ? Configuration.Debug : Configuration.Release)
                    .SetDisableParallel(true));
            });

        Target PrepareDevelopmentEnvironment => _ => _
            .Executes(() =>
            {
                InstallSolutionAndDependencies(DataverseSolution, SolutionType.Unmanaged);
            });

        void SetActivePacProfile(string profile)
        {
            Pac($"auth select -n { profile }");
        }

        SolutionConfiguration GetSolutionConfig(string dataverseSolution)
        {
            return JsonSerializer.Deserialize<SolutionConfiguration>(
                File.ReadAllText(SolutionsDirectory / dataverseSolution / "solution.json"),
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        Dictionary<string, string> GetSolutionDependencies(string solution)
        {
            var dependencyAttributes = (IEnumerable)XDocument.Load(SolutionsDirectory / solution / "Extract" / "Other" / "Solution.xml")
              .XPathEvaluate("/ImportExportXml/SolutionManifest/MissingDependencies/MissingDependency/Required/@solution");

            var dependencies = dependencyAttributes
              .Cast<XAttribute>()
              .Select(solutionAttribute => solutionAttribute.Value)
              .Where(sol => sol != "Active")
              .Distinct()
              .ToDictionary(sol => sol.Split(' ')[0], sol => sol.Split('(', ')')[1]);

            return dependencies;
        }


        void InstallSolutionAndDependencies(string solution, SolutionType solutionType, IList<string> installedSolutions = null)
        {
            if (installedSolutions == null)
            {
                installedSolutions = new List<string>();
                SetActivePacProfile(GetSolutionConfig(solution).DevelopmentProfile);
            }

            foreach (var dependency in GetSolutionDependencies(solution).Where(dep => DirectoryExists(SolutionsDirectory / dep.Key)))
            {
                if (!installedSolutions.Contains(dependency.Key))
                {
                    InstallSolutionAndDependencies(dependency.Key, SolutionType.Managed, installedSolutions);
                }
            }

            var buildConfiguration = solutionType == SolutionType.Managed ? "Release" : "Debug";
            DotNetBuild(s => s
                .SetProjectFile(SolutionsDirectory / solution / $"{solution}.cdsproj")
                .SetConfiguration(buildConfiguration)
                .SetDisableParallel(true));

            Pac($"solution import --path \"{ SolutionsDirectory / solution / "bin" / buildConfiguration / $"{solution}.zip" }\" -ap -pc -a");
            installedSolutions.Add(solution);
        }

    }
}
