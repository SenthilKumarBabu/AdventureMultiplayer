from __future__ import annotations

import inspect
from typing import Any

from fastmcp import Context, FastMCP

from da_mcp_server.transport.models import ToolDefinitionModel
from da_mcp_server.transport.unity_bridge_hub import UnityBridgeHub


class UnityToolBindingService:
    def __init__(self, mcp: FastMCP, endpoint_path: str = "") -> None:
        self._mcp = mcp
        self._endpoint_path = endpoint_path
        self._registered: dict[str, ToolDefinitionModel] = {}

    def register_tools(self, tools: list[ToolDefinitionModel]) -> None:
        for definition in tools:
            self.register_tool(definition)

    def register_tool(self, definition: ToolDefinitionModel) -> None:
        existing = self._registered.get(definition.name)
        if existing and existing.model_dump() == definition.model_dump():
            return
        if existing:
            self._mcp.remove_tool(definition.name)

        handler = self._build_handler(definition)
        self._mcp.tool(
            name=definition.name,
            description=definition.description,
            annotations=definition.annotations or None,
        )(handler)
        self._registered[definition.name] = definition

    def _build_handler(self, definition: ToolDefinitionModel):
        async def _handler(ctx: Context, **kwargs) -> dict[str, Any]:
            params = {key: value for key, value in kwargs.items() if value is not None}
            result = await UnityBridgeHub.send_command(
                UnityBridgeHub.first_session_id(),
                definition.name,
                params,
                endpoint_path=self._endpoint_path,
            )
            return result

        _handler.__name__ = f"da_dynamic_tool_{definition.name}"
        _handler.__doc__ = definition.description or ""
        _handler.__signature__ = self._build_signature(definition)
        _handler.__annotations__ = self._build_annotations(definition)
        return _handler

    def _build_signature(self, definition: ToolDefinitionModel) -> inspect.Signature:
        params: list[inspect.Parameter] = [
            inspect.Parameter("ctx", inspect.Parameter.POSITIONAL_OR_KEYWORD, annotation=Context)
        ]
        schema = definition.inputSchema or {}
        properties = schema.get("properties") or {}
        required = set(schema.get("required") or [])

        for name, prop_schema in properties.items():
            if not isinstance(name, str) or not name.isidentifier():
                continue
            params.append(
                inspect.Parameter(
                    name,
                    inspect.Parameter.POSITIONAL_OR_KEYWORD,
                    default=inspect._empty if name in required else None,
                    annotation=self._map_schema_type(prop_schema),
                )
            )

        return inspect.Signature(parameters=params)

    def _build_annotations(self, definition: ToolDefinitionModel) -> dict[str, object]:
        annotations: dict[str, object] = {"ctx": Context}
        schema = definition.inputSchema or {}
        properties = schema.get("properties") or {}
        for name, prop_schema in properties.items():
            if isinstance(name, str) and name.isidentifier():
                annotations[name] = self._map_schema_type(prop_schema)
        return annotations

    @staticmethod
    def _map_schema_type(prop_schema: Any) -> type:
        if not isinstance(prop_schema, dict):
            return str
        schema_type = str(prop_schema.get("type") or "string").lower()
        if schema_type in ("integer", "int"):
            return int
        if schema_type in ("number", "float", "double"):
            return float
        if schema_type in ("boolean", "bool"):
            return bool
        if schema_type == "array":
            return list
        if schema_type == "object":
            return dict
        return str
