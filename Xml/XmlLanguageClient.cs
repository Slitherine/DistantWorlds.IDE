using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace DW2IDE.Xml;

public sealed class XmlLanguageClientFactory {

  private static string _schemaDir = Path.Combine(Environment.CurrentDirectory, "schema");

  public static readonly object DefaultSettingsObject = new {
    settings = new {
      xml = new {
        trace = new {
          server = "verbose"
        },
        logs = new {
          client = true
        },
        useCache = true,
        linkedEditingEnabled = true,
        telemetry = new {
          enabled = false
        },
        downloadExternalResources = new {
          enabled = false
        },
        format = new {
          enabled = true,
          splitAttributes = "preserve",
          trimFinalNewlines = true,
          trimTrailingWhitespace = true,
          insertFinalNewline = true
        },
        completion = new {
          autoCloseTags = false
        },
        validation = new {
          disallowDocTypeDeclaration = true,
          resolveExternalEntities = false,
          namespaces = new {
            enabled = "never" // "always" | "never" | "onNamespaceEncountered"
          },
          schema = new {
            enabled = "onValidSchema" // "always" | "never" | "onValidSchema"
          },
          xInclude = new {
            enabled = "never" // ???
          },
        },
        fileAssociations = new[] {
          new { pattern = "ArmyTemplates*.xml", systemId = $"{_schemaDir}/ArmyTemplateList.xsd" },
          new { pattern = "Artifacts*.xml", systemId = $"{_schemaDir}/ArtifactList.xsd" },
          new { pattern = "CharacterAnimations*.xml", systemId = $"{_schemaDir}/CharacterAnimationList.xsd" },
          new { pattern = "CharacterDefinitions*.xml", systemId = $"{_schemaDir}/CharacterDefinitionList.xsd" },
          new { pattern = "CharacterRooms*.xml", systemId = $"{_schemaDir}/CharacterRoomList.xsd" },
          new { pattern = "ColonyEventDefinitions*.xml", systemId = $"{_schemaDir}/ColonyEventDefinitionList.xsd" },
          new { pattern = "ComponentDefinitions*.xml", systemId = $"{_schemaDir}/ComponentDefinitionList.xsd" },
          new { pattern = "CreatureTypes*.xml", systemId = $"{_schemaDir}/CreatureTypeList.xsd" },
          new { pattern = "DesignTemplates*.xml", systemId = $"{_schemaDir}/DesignTemplateList.xsd" },
          new { pattern = "FleetTemplates*.xml", systemId = $"{_schemaDir}/FleetTemplateList.xsd" },
          new { pattern = "GameEvents*.xml", systemId = $"{_schemaDir}/GameEventList.xsd" },
          new { pattern = "Governments*.xml", systemId = $"{_schemaDir}/GovernmentList.xsd" },
          new { pattern = "OrbTypes*.xml", systemId = $"{_schemaDir}/OrbTypeList.xsd" },
          new { pattern = "PlanetaryFacilityDefinitions*.xml", systemId = $"{_schemaDir}/PlanetaryFacilityDefinitionList.xsd" },
          new { pattern = "Races*.xml", systemId = $"{_schemaDir}/RaceList.xsd" },
          new { pattern = "ResearchProjectDefinitions*.xml", systemId = $"{_schemaDir}/ResearchProjectDefinitionList.xsd" },
          new { pattern = "Resources*.xml", systemId = $"{_schemaDir}/ResourceList.xsd" },
          new { pattern = "ShipHulls*.xml", systemId = $"{_schemaDir}/ShipHullList.xsd" },
          new { pattern = "SpaceItemDefinitions*.xml", systemId = $"{_schemaDir}/SpaceItemDefinitionList.xsd" },
          new { pattern = "TourItems*.xml", systemId = $"{_schemaDir}/TourItemList.xsd" },
          new { pattern = "TroopDefinitions*.xml", systemId = $"{_schemaDir}/TroopDefinitionList.xsd" },
          new { pattern = "policy/*.xml", systemId = $"{_schemaDir}/EmpirePolicy.xsd" },
        },
        catalogs = new string[] {
          // "path/to/catalog.xml"
        },
      },
    }
  };

  public static async Task<LanguageClient> CreateAsync() {
    static LogLevel GetLogLevel(MessageType type)
      => type switch {
        MessageType.Info => LogLevel.Information,
        MessageType.Warning => LogLevel.Warning,
        MessageType.Error => LogLevel.Error,
        MessageType.Log => LogLevel.Trace,
        _ => LogLevel.Debug
      };

    var pathToLemMinX = Path.Combine(Environment.CurrentDirectory, "xml", "lemminx.exe");
    if (!File.Exists(pathToLemMinX)) throw new NotImplementedException();

    var lemMinXImplLogger = SharedConsoleLoggerFactory.CreateLogger("LemMinX");
    var lemMinX = new LemMinXWrapper(pathToLemMinX);
    var lemMinXClient = LanguageClient.Create
    (o => o
      .WithInput(lemMinX.CreateReader()!)
      .WithOutput(lemMinX.CreateWriter()!)
      .WithRootPath(Path.GetDirectoryName(Environment.CurrentDirectory)!)
      .WithInitializationOptions(DefaultSettingsObject)
      .WithTrace(InitializeTrace.Verbose)
      .WithLoggerFactory(SharedConsoleLoggerFactory)
      .OnLogTrace(logTrace => {
#pragma warning disable CA2254
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        lemMinXImplLogger.Log(LogLevel.Trace, $"{logTrace.Message}; {logTrace.Verbose}");
#pragma warning restore CA2254
      })
      .OnLogMessage(logMsg => {
#pragma warning disable CA2254
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        lemMinXImplLogger.Log(GetLogLevel(logMsg.Type), logMsg.Message);
#pragma warning restore CA2254
      }));
    await lemMinXClient.Initialize(default);

    AppDomain.CurrentDomain.ProcessExit += (_, _) => {
      try {
        lemMinXClient.Shutdown()
          .Wait(2000);
        lemMinX.Dispose();
        lemMinXClient.Dispose();
      }
      catch {
        // exiting
      }
    };

    return lemMinXClient;
  }

  public static LanguageClient Create() {
    return SharedJoinableTaskFactory
      .Run(async () => await CreateAsync());
  }

}