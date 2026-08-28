from __future__ import annotations

import asyncio
import logging
import time
import uuid
from typing import Any, ClassVar

from starlette.endpoints import WebSocketEndpoint
from starlette.websockets import WebSocket

from da_mcp_server.transport.models import (
    CommandResultMessage,
    ExecuteCommandMessage,
    PingMessage,
    PongMessage,
    RegisteredMessage,
    RegisterResourcesMessage,
    RegisterMessage,
    RegisterToolsMessage,
    WelcomeMessage,
)
from da_mcp_server.transport.unity_session_registry import UnitySessionRegistry

logger = logging.getLogger(__name__)


class UnityBridgeDisconnectedError(RuntimeError):
    pass


class NoUnityBridgeSessionError(RuntimeError):
    pass


class UnityBridgeHub(WebSocketEndpoint):
    encoding = "json"

    KEEP_ALIVE_INTERVAL = 15
    SERVER_TIMEOUT = 30
    COMMAND_TIMEOUT = 30
    PING_INTERVAL = 10
    PING_TIMEOUT = 20
    FAST_FAIL_TIMEOUT = 2.0

    _registry: ClassVar[UnitySessionRegistry | None] = None
    _connections: ClassVar[dict[str, WebSocket]] = {}
    _pending: ClassVar[dict[str, asyncio.Future[dict[str, Any]]]] = {}
    _last_pong: ClassVar[dict[str, float]] = {}
    _lock: ClassVar[asyncio.Lock | None] = None
    _sub_manager: ClassVar[Any | None] = None

    @classmethod
    def configure(cls, registry: UnitySessionRegistry, sub_manager: Any | None = None) -> None:
        cls._registry = registry
        cls._sub_manager = sub_manager
        cls._lock = asyncio.Lock()

    async def on_connect(self, websocket: WebSocket) -> None:
        await websocket.accept()
        await websocket.send_json(
            WelcomeMessage(
                serverTimeout=self.SERVER_TIMEOUT,
                keepAliveInterval=self.KEEP_ALIVE_INTERVAL,
            ).model_dump()
        )

    async def on_receive(self, websocket: WebSocket, data: Any) -> None:
        if not isinstance(data, dict):
            logger.warning("Ignoring non-object WebSocket payload: %r", data)
            return

        message_type = data.get("type")
        if message_type == "register":
            await self._handle_register(websocket, RegisterMessage(**data))
        elif message_type == "register_tools":
            await self._handle_register_tools(websocket, RegisterToolsMessage(**data))
        elif message_type == "register_resources":
            await self._handle_register_resources(websocket, RegisterResourcesMessage(**data))
        elif message_type == "pong":
            await self._handle_pong(PongMessage(**data))
        elif message_type == "command_result":
            await self._handle_command_result(CommandResultMessage(**data))
        else:
            logger.debug("Ignoring unknown bridge message: %r", data)

    async def on_disconnect(self, websocket: WebSocket, close_code: int) -> None:
        session_id = next((sid for sid, ws in self._connections.items() if ws is websocket), None)
        if session_id is None:
            return

        self._connections.pop(session_id, None)
        self._last_pong.pop(session_id, None)
        if self._registry is not None:
            self._registry.unregister(session_id)

        for command_id, future in list(self._pending.items()):
            if not future.done():
                future.set_exception(UnityBridgeDisconnectedError(f"Unity session {session_id} disconnected"))
            self._pending.pop(command_id, None)

    @classmethod
    async def send_command(
        cls,
        session_id: str,
        command_type: str,
        params: dict[str, Any],
        timeout: float | None = None,
        endpoint_path: str = "",
    ) -> dict[str, Any]:
        websocket = cls._connections.get(session_id)
        if websocket is None:
            raise NoUnityBridgeSessionError(f"Unity session {session_id} is not connected")

        command_id = uuid.uuid4().hex
        loop = asyncio.get_running_loop()
        future: asyncio.Future[dict[str, Any]] = loop.create_future()
        cls._pending[command_id] = future

        await websocket.send_json(
            ExecuteCommandMessage(
                id=command_id,
                name=command_type,
                params=params,
                timeout=timeout or cls.COMMAND_TIMEOUT,
                endpoint_path=endpoint_path,
            ).model_dump()
        )

        try:
            return await asyncio.wait_for(future, timeout or cls.COMMAND_TIMEOUT)
        finally:
            cls._pending.pop(command_id, None)

    @classmethod
    def first_session_id(cls) -> str:
        if not cls._connections:
            raise NoUnityBridgeSessionError("No Unity transport sessions running")
        return next(iter(cls._connections))

    async def _handle_register(self, websocket: WebSocket, message: RegisterMessage) -> None:
        session_id = uuid.uuid4().hex
        self._connections[session_id] = websocket
        self._last_pong[session_id] = time.monotonic()

        if self._registry is not None:
            self._registry.register(
                session_id=session_id,
                project_name=message.project_name,
                project_hash=message.project_hash,
                unity_version=message.unity_version,
                project_path=message.project_path,
            )

        websocket.state.session_id = session_id
        await websocket.send_json(RegisteredMessage(session_id=session_id).model_dump())

    async def _handle_register_tools(self, websocket: WebSocket, message: RegisterToolsMessage) -> None:
        if self._registry is None:
            return
        session_id = getattr(websocket.state, "session_id", None)
        if not session_id:
            return
        self._registry.register_tools_for_session(session_id, message.tools)
        if self._sub_manager is not None:
            for tool in message.tools:
                if tool.endpointPath:
                    await self._sub_manager.register_tool(
                        tool.endpointPath, tool.serverName, tool)

    async def _handle_register_resources(self, websocket: WebSocket, message: RegisterResourcesMessage) -> None:
        if self._registry is None:
            return
        session_id = getattr(websocket.state, "session_id", None)
        if not session_id:
            return
        self._registry.register_resources_for_session(session_id, message.resources)
        if self._sub_manager is not None:
            for resource in message.resources:
                await self._sub_manager.register_resource(
                    "", "", resource)

    async def _handle_pong(self, message: PongMessage) -> None:
        if message.session_id:
            self._last_pong[message.session_id] = time.monotonic()

    async def _handle_command_result(self, message: CommandResultMessage) -> None:
        future = self._pending.get(message.id)
        if future is not None and not future.done():
            future.set_result(message.result)

    async def _send_ping(self, session_id: str) -> None:
        websocket = self._connections.get(session_id)
        if websocket is not None:
            await websocket.send_json(PingMessage().model_dump())
