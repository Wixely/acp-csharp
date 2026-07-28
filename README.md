# AgentClientProtocol for C#
 Unofficial C# SDK for ACP (Agent Client Protocol) clients and agents

[![NuGet](https://img.shields.io/nuget/v/AgentClientProtocol.svg)](https://www.nuget.org/packages/AgentClientProtocol)
[![Releases](https://img.shields.io/github/release/nuskey8/acp-csharp.svg)](https://github.com/nuskey8/acp-csharp/releases)
[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

## About this fork

This is the [Wixely](https://github.com/Wixely/acp-csharp) fork of [nuskey8/acp-csharp](https://github.com/nuskey8/acp-csharp), maintained to keep the SDK usable for building the **agent** side of ACP while changes make their way upstream. It diverges from upstream `v0.1.5` as follows:

- **Agent-side client API and JSON-RPC fixes** — `AgentSideConnection` implements `IAcpClient` (send `session/update`, call `session/request_permission` / `fs/*` / `terminal/*`), handlers dispatch off the read loop (fixes `session/cancel` starvation and a permission-request deadlock), EOF terminates the read loop, `terminal/output` uses the correct response type, and required value-type fields (e.g. `stopReason`) are no longer omitted when serializing.
- **Session management** (`session/list`, `session/fork`, `session/resume`, `session/close`) — cherry-picked from [wuyunben518/acp-csharp](https://github.com/wuyunben518/acp-csharp) (commit `b11cf0e`, authored by [@wuyunben518](https://github.com/wuyunben518)), extended here with `session/delete` and `AgentCapabilities.sessionCapabilities` to match the current [stable v1 schema](https://github.com/agentclientprotocol/agent-client-protocol/tree/main/schema/v1).
- **`ClientSideConnection.ExtNotificationAsync` forwards notifications** instead of dropping them — the same fix independently made in [wenytang-ms/acp-csharp](https://github.com/wenytang-ms/acp-csharp).

Related work elsewhere: [syan2018/acp-csharp](https://github.com/syan2018/acp-csharp) proposes a schema-sync code generator ([nuskey8/acp-csharp#9](https://github.com/nuskey8/acp-csharp/pull/9)) that regenerates all models from the official schema — likely the right long-term approach for tracking the spec.

## What's Agent Client Protocol?

Agent Client Protocol is a protocol proposed by Zed to standardize communication between code editors/IDEs and coding agents.

> ACP solves this by providing a standardized protocol for agent-editor communication, similar to how the Language Server Protocol (LSP) standardized language server integration.

Please refer to [the official documentation](https://agentclientprotocol.com/) for details.

## Installation

### .NET CLI

```ps1
dotnet add package AgentClientProtocol
```

### Package Manager

```ps1
Install-Package AgentClientProtocol
```

## Quick Start

### Client

```cs
class ExampleClient : IAcpClient { ... }
```

```cs
var client = new ExampleClient();

using var conn = new ClientSideConnection( _ => client, reader, writer);

conn.Open();

var initResult = await conn.InitializeAsync(new InitializeRequest
{
    ProtocolVersion = 1,
    ClientCapabilities = new ClientCapabilities
    {
        Fs = new FileSystemCapability
        {
            ReadTextFile = true,
            WriteTextFile = true
        }
    }
});

Console.WriteLine($"Connected to agent (protocol v{initResult.ProtocolVersion})");
```

### Agent

```cs
class ExampleClient : IAcpAgent { ... }
```

```cs
var agent = new ExampleAgent();
using var conn = new AgentSideConnection(agent, reader, writer);
conn.Open();

await Task.Delay(-1);
```

## License

This library is under the [MIT License](LICENSE).