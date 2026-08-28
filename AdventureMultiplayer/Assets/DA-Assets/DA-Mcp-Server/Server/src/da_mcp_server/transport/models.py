from __future__ import annotations

from typing import Any

from pydantic import BaseModel, Field


class ToolDefinitionModel(BaseModel):
    name: str
    description: str = ""
    inputSchema: dict[str, Any] = Field(default_factory=dict)
    annotations: dict[str, Any] = Field(default_factory=dict)
    endpointPath: str = ""
    serverName: str = ""
    requiresPolling: bool = False
    pollAction: str = "status"
    maxPollSeconds: int = 0


class ResourceDefinitionModel(BaseModel):
    uri: str
    name: str
    description: str = ""
    mimeType: str = "text/plain"


class WelcomeMessage(BaseModel):
    type: str = "welcome"
    serverTimeout: int
    keepAliveInterval: int


class RegisteredMessage(BaseModel):
    type: str = "registered"
    session_id: str


class ExecuteCommandMessage(BaseModel):
    type: str = "execute"
    id: str
    name: str
    params: dict[str, Any] = Field(default_factory=dict)
    timeout: float = 30.0
    endpoint_path: str = ""


class PingMessage(BaseModel):
    type: str = "ping"


class RegisterMessage(BaseModel):
    type: str = "register"
    project_name: str = "Unknown Project"
    project_hash: str
    unity_version: str = "Unknown"
    project_path: str | None = None


class RegisterToolsMessage(BaseModel):
    type: str = "register_tools"
    tools: list[ToolDefinitionModel] = Field(default_factory=list)


class RegisterResourcesMessage(BaseModel):
    type: str = "register_resources"
    resources: list[ResourceDefinitionModel] = Field(default_factory=list)


class PongMessage(BaseModel):
    type: str = "pong"
    session_id: str | None = None


class CommandResultMessage(BaseModel):
    type: str = "command_result"
    id: str
    result: dict[str, Any] = Field(default_factory=dict)


class SessionDetails(BaseModel):
    project: str
    hash: str
    unity_version: str
    project_path: str | None = None
    connected_at: float


class SessionList(BaseModel):
    sessions: dict[str, SessionDetails]
