#if DEV_XML_LANG_SERVER_INPROC
using System;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using OmniSharp.Extensions.LanguageServer.Server;

namespace DistantWorlds.IDE.XmlLanguageServer;

public class XmlLanguageServer {

  private Task Run(Pipe input, Pipe output) {
    var server = LanguageServer.Create(o => o
      .WithOutput(output.Writer)
      .WithInput(input.Reader)
      //.WithHandler<DocumentHandler>() // ...
      .AddDefaultLoggingProvider()
      .ConfigureLogging(b => b.AddLanguageProtocolLogging())
      .OnInitialize(async (server, request, token) => {
        server.LogInfo("OnInitialize");
        await CompletedAsync();
      })
      .OnInitialized(async (server, request, response, token) => {
        server.LogInfo("OnInitialize");
        await CompletedAsync();
      })
      .OnStarted(async (server, ct) => {
        server.LogInfo("OnStarted");
        ct.ThrowIfCancellationRequested();
        await CompletedAsync();
      })
      .OnTextDocumentSync(
        TextDocumentSyncKind.Incremental,
        docUri => {
          var uriStr = docUri.ToString();
          var path = DocumentUriToLocalPathTracker.GetLocalPath(uriStr);

          if (path is null)
            throw new FileNotFoundException("Can't locate local file path for URI.", uriStr);

          var attribs = new TextDocumentAttributes(docUri, "xml");

          return attribs;
        },
        openTextDocParams => {
          var xmlStr = openTextDocParams.TextDocument.Text;
          throw new NotImplementedException();
        },
        closeTextDocParams => {
          throw new NotImplementedException();
        },
        changeTextDocParams => {
          throw new NotImplementedException();
        },
        saveTextDocParams => {
          throw new NotImplementedException();
        },
        (capability, capabilities) => {
          throw new NotImplementedException();
        }
      )
      .OnCodeAction(
        codeActionParams => {
          throw new NotImplementedException();
        },
        action => {
          throw new NotImplementedException();
        },
        (capability, capabilities) => {
          throw new NotImplementedException();
        }
      )
      .OnCompletion(
        completionParams => {
          throw new NotImplementedException();
        },
        completionItem => {
          throw new NotImplementedException();
        },
        (capability, capabilities) => {
          throw new NotImplementedException();
        })
      .OnDocumentHighlight(
        docHighlightParams => {
          throw new NotImplementedException();
        },
        (capability, capabilities) => {
          throw new NotImplementedException();
        })
      .OnDocumentLink(
        docLinkParams => {
          throw new NotImplementedException();
        },
        docLink => {
          throw new NotImplementedException();
        },
        (capability, capabilities) => {
          throw new NotImplementedException();
        }
      )
      .OnDocumentSymbol(
        docSymbolParams => {
          throw new NotImplementedException();
        },
        (capability, capabilities) => {
          throw new NotImplementedException();
        }
      )
      //.OnFoldingRange()
      .OnDocumentFormatting(
        docFormattingParams => {
          throw new NotImplementedException();
        },
        (capability, capabilities) => {
          throw new NotImplementedException();
        }
      )
      .OnHover(
        hoverParams => {
          throw new NotImplementedException();
        },
        (capability, capabilities) => {
          throw new NotImplementedException();
        }
      )
      .OnDocumentRangeFormatting(
        docRangeFormattingParams => {
          throw new NotImplementedException();
        },
        (capability, capabilities) => {
          throw new NotImplementedException();
        }
      )
      .OnRename(
        renameParams => {
          throw new NotImplementedException();
        },
        (capability, capabilities) => {
          throw new NotImplementedException();
        })
    );

    throw new NotImplementedException();
  }

  private static ValueTask CompletedAsync()
    => ValueTask.CompletedTask;

}
#endif