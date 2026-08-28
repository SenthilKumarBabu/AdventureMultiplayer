from __future__ import annotations

import time
from dataclasses import dataclass, field

from da_mcp_server.transport.models import ResourceDefinitionModel, SessionDetails, SessionList, ToolDefinitionModel


@dataclass
class UnitySessionRecord:
    session_id: str
    project_name: str
    project_hash: str
    unity_version: str
    project_path: str | None
    connected_at: float = field(default_factory=time.time)
    tools: list[ToolDefinitionModel] = field(default_factory=list)
    resources: list[ResourceDefinitionModel] = field(default_factory=list)


class UnitySessionRegistry:
    def __init__(self) -> None:
        self._sessions: dict[str, UnitySessionRecord] = {}
        self._tools: dict[str, list[ToolDefinitionModel]] = {}
        self._resources: dict[str, list[ResourceDefinitionModel]] = {}

    def register(
        self,
        session_id: str,
        project_name: str,
        project_hash: str,
        unity_version: str,
        project_path: str | None,
    ) -> UnitySessionRecord:
        session = UnitySessionRecord(
            session_id=session_id,
            project_name=project_name,
            project_hash=project_hash,
            unity_version=unity_version,
            project_path=project_path,
        )
        self._sessions[session_id] = session
        self._tools.setdefault(session_id, [])
        self._resources.setdefault(session_id, [])
        return session

    def unregister(self, session_id: str) -> None:
        self._sessions.pop(session_id, None)
        self._tools.pop(session_id, None)
        self._resources.pop(session_id, None)

    def register_tools_for_session(self, session_id: str, tools: list[ToolDefinitionModel]) -> None:
        self._tools[session_id] = list(tools)
        if session_id in self._sessions:
            self._sessions[session_id].tools = list(tools)

    def get_tools_for_session(self, session_id: str) -> list[ToolDefinitionModel]:
        return list(self._tools.get(session_id, []))

    def register_resources_for_session(self, session_id: str, resources: list[ResourceDefinitionModel]) -> None:
        self._resources[session_id] = list(resources)
        if session_id in self._sessions:
            self._sessions[session_id].resources = list(resources)

    def get_resources_for_session(self, session_id: str) -> list[ResourceDefinitionModel]:
        return list(self._resources.get(session_id, []))

    def list_sessions(self) -> SessionList:
        return SessionList(
            sessions={
                sid: SessionDetails(
                    project=session.project_name,
                    hash=session.project_hash,
                    unity_version=session.unity_version,
                    project_path=session.project_path,
                    connected_at=session.connected_at,
                )
                for sid, session in self._sessions.items()
            }
        )

    def has_sessions(self) -> bool:
        return bool(self._sessions)
