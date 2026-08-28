from __future__ import annotations

from fastmcp import FastMCP

from da_mcp_server.transport.models import ResourceDefinitionModel
from da_mcp_server.transport.unity_bridge_hub import UnityBridgeHub


class DynamicResourceService:
    def __init__(self, mcp: FastMCP, endpoint_path: str = "") -> None:
        self._mcp = mcp
        self._endpoint_path = endpoint_path
        self._registered: dict[str, ResourceDefinitionModel] = {}

    def register_resources(self, resources: list[ResourceDefinitionModel]) -> None:
        for definition in resources:
            if definition.uri in self._registered:
                continue
            self._mcp.resource(
                definition.uri,
                name=definition.name,
                description=definition.description,
                mime_type=definition.mimeType,
            )(self._build_handler(definition))
            self._registered[definition.uri] = definition

    def _build_handler(self, definition: ResourceDefinitionModel):
        async def _handler() -> str:
            result = await UnityBridgeHub.send_command(
                UnityBridgeHub.first_session_id(),
                "read_resource",
                {"uri": definition.uri},
                endpoint_path=self._endpoint_path,
            )
            content = result.get("content") or []
            if not content:
                return ""
            first = content[0] or {}
            return str(first.get("text") or "")

        _handler.__name__ = f"da_dynamic_resource_{definition.name}"
        _handler.__doc__ = definition.description or ""
        return _handler
