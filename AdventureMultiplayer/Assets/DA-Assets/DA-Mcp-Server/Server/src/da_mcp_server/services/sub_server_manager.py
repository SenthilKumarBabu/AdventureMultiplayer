from __future__ import annotations

import logging
import asyncio
from typing import Any

from fastmcp import FastMCP
from starlette.applications import Starlette
from starlette.routing import Mount

from da_mcp_server.services.dynamic_resource_service import DynamicResourceService
from da_mcp_server.services.unity_tool_binding_service import UnityToolBindingService
from da_mcp_server.transport.models import ResourceDefinitionModel, ToolDefinitionModel

logger = logging.getLogger(__name__)


class SubServerManager:
    def __init__(self) -> None:
        self._app: Starlette | None = None
        self._servers: dict[str, FastMCP] = {}
        self._tool_services: dict[str, UnityToolBindingService] = {}
        self._resource_services: dict[str, DynamicResourceService] = {}
        self._sub_apps: dict[str, Starlette] = {}
        self._lifespans: dict[str, Any] = {}
        self._start_queue: asyncio.Queue[tuple[str, asyncio.Future[None]] | None] | None = None
        self._worker_task: asyncio.Task[None] | None = None
        self._running = False

    def set_app(self, app: Starlette) -> None:
        self._app = app

    async def start(self) -> None:
        self._running = True
        self._start_queue = asyncio.Queue()
        self._worker_task = asyncio.create_task(self._lifespan_worker())
        for endpoint_path in list(self._sub_apps):
            await self._ensure_sub_app_started(endpoint_path)

    async def stop(self) -> None:
        self._running = False
        if self._start_queue is not None:
            await self._start_queue.put(None)
        if self._worker_task is not None:
            await self._worker_task
        self._start_queue = None
        self._worker_task = None

    async def get_or_create(self, endpoint_path: str, display_name: str = "") -> FastMCP:
        if endpoint_path in self._servers:
            return self._servers[endpoint_path]

        name = display_name or endpoint_path.upper()
        sub = FastMCP(name=name)
        self._servers[endpoint_path] = sub
        self._tool_services[endpoint_path] = UnityToolBindingService(sub, endpoint_path)
        self._resource_services[endpoint_path] = DynamicResourceService(sub, endpoint_path)

        sub_app = sub.http_app(path="/mcp")
        self._sub_apps[endpoint_path] = sub_app
        if self._app is not None:
            self._app.routes.insert(len(self._app.routes), Mount(f"/{endpoint_path}", app=sub_app))
        if self._running:
            await self._ensure_sub_app_started(endpoint_path)
        logger.info("Created sub-server '%s' at /%s/mcp", name, endpoint_path)
        return sub

    async def register_tool(self, endpoint_path: str, display_name: str, tool_def: ToolDefinitionModel) -> None:
        await self.get_or_create(endpoint_path, display_name)
        self._tool_services[endpoint_path].register_tool(tool_def)

    async def register_resource(self, endpoint_path: str, display_name: str, resource_def: ResourceDefinitionModel) -> None:
        await self.get_or_create(endpoint_path, display_name)
        self._resource_services[endpoint_path].register_resources([resource_def])

    def list_servers(self) -> dict[str, str]:
        return {k: v.name for k, v in self._servers.items()}

    async def _ensure_sub_app_started(self, endpoint_path: str) -> None:
        if endpoint_path in self._lifespans or self._start_queue is None:
            return
        loop = asyncio.get_running_loop()
        future: asyncio.Future[None] = loop.create_future()
        await self._start_queue.put((endpoint_path, future))
        await future

    async def _lifespan_worker(self) -> None:
        if self._start_queue is None:
            return

        try:
            while True:
                item = await self._start_queue.get()
                if item is None:
                    return

                endpoint_path, future = item
                if endpoint_path in self._lifespans:
                    future.set_result(None)
                    continue

                try:
                    sub_app = self._sub_apps[endpoint_path]
                    lifespan = sub_app.router.lifespan_context(sub_app)
                    await lifespan.__aenter__()
                    self._lifespans[endpoint_path] = lifespan
                    future.set_result(None)
                except Exception as ex:
                    future.set_exception(ex)
        finally:
            for endpoint_path, lifespan in list(self._lifespans.items())[::-1]:
                try:
                    await lifespan.__aexit__(None, None, None)
                except Exception:
                    logger.exception("Failed to stop sub-server lifespan for '%s'", endpoint_path)
            self._lifespans.clear()
