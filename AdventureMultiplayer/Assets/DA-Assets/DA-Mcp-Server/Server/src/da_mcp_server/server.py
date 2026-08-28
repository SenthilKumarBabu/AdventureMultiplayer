from __future__ import annotations

import argparse
import atexit
import logging
import os
import time
from contextlib import asynccontextmanager
from collections.abc import AsyncGenerator

import uvicorn
from fastmcp import FastMCP
from fastmcp.utilities.cli import log_server_banner
from fastmcp.utilities.logging import configure_logging, get_logger
from starlette.applications import Starlette
from starlette.requests import Request
from starlette.responses import JSONResponse
from starlette.routing import Route, WebSocketRoute

from da_mcp_server import __version__
from da_mcp_server.constants import APP_NAME, DEFAULT_HTTP_HOST, DEFAULT_HTTP_PORT
from da_mcp_server.services.sub_server_manager import SubServerManager
from da_mcp_server.transport.unity_bridge_hub import UnityBridgeHub
from da_mcp_server.transport.unity_session_registry import UnitySessionRegistry

logger = logging.getLogger(__name__)
fastmcp_transport_logger = get_logger("fastmcp.server.mixins.transport")


def create_gateway() -> tuple[Starlette, SubServerManager]:
    registry = UnitySessionRegistry()
    sub_manager = SubServerManager()
    UnityBridgeHub.configure(registry, sub_manager)

    async def health(_: Request) -> JSONResponse:
        return JSONResponse(
            {
                "status": "healthy",
                "timestamp": time.time(),
                "version": __version__,
                "message": "DA MCP server is running",
            }
        )

    async def api_instances(_: Request) -> JSONResponse:
        return JSONResponse(registry.list_sessions().model_dump())

    async def api_command(request: Request) -> JSONResponse:
        payload = await request.json()
        session_id = payload.get("session_id") or UnityBridgeHub.first_session_id()
        command_type = payload.get("type") or payload.get("name")
        if not command_type:
            return JSONResponse({"success": False, "error": "type is required"}, status_code=400)
        params = payload.get("params") or {}
        endpoint_path = payload.get("endpoint_path") or ""
        result = await UnityBridgeHub.send_command(session_id, command_type, params, endpoint_path=endpoint_path)
        return JSONResponse(result)

    async def api_custom_tools(_: Request) -> JSONResponse:
        tools = []
        for session_id in registry.list_sessions().sessions:
            tools.extend(tool.model_dump() for tool in registry.get_tools_for_session(session_id))
        return JSONResponse({"tools": tools})

    async def api_custom_resources(_: Request) -> JSONResponse:
        resources = []
        for session_id in registry.list_sessions().sessions:
            resources.extend(resource.model_dump() for resource in registry.get_resources_for_session(session_id))
        return JSONResponse({"resources": resources})

    async def api_servers(_: Request) -> JSONResponse:
        return JSONResponse({"servers": sub_manager.list_servers()})

    routes = [
        Route("/health", health, methods=["GET"]),
        Route("/api/instances", api_instances, methods=["GET"]),
        Route("/api/command", api_command, methods=["POST"]),
        Route("/api/custom-tools", api_custom_tools, methods=["GET"]),
        Route("/api/custom-resources", api_custom_resources, methods=["GET"]),
        Route("/api/servers", api_servers, methods=["GET"]),
        WebSocketRoute("/hub/bridge", UnityBridgeHub),
    ]

    @asynccontextmanager
    async def lifespan(_: Starlette) -> AsyncGenerator[None, None]:
        await sub_manager.start()
        try:
            yield
        finally:
            await sub_manager.stop()

    app = Starlette(routes=routes, lifespan=lifespan)
    sub_manager.set_app(app)
    logger.debug("%s v%s gateway created", APP_NAME, __version__)
    return app, sub_manager


def run_server(
    transport: str = "stdio",
    host: str = DEFAULT_HTTP_HOST,
    port: int = DEFAULT_HTTP_PORT,
    pidfile: str | None = None,
) -> None:
    logging.basicConfig(level=logging.INFO)
    _write_pidfile(pidfile)

    if transport == "http":
        app, _ = create_gateway()
        mcp = FastMCP(name=APP_NAME, version=__version__)
        configure_logging(level="INFO")
        log_server_banner(server=mcp)
        fastmcp_transport_logger.info(
            f"Starting MCP server {APP_NAME!r} with transport {'http'!r} on http://{host}:{port}/mcp"
        )
        uvicorn.run(app, host=host, port=port)
    else:
        mcp = FastMCP(name=APP_NAME, version=__version__)
        mcp.run(transport="stdio")


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(prog=APP_NAME)
    parser.add_argument("--transport", choices=("stdio", "http"), default="stdio")
    parser.add_argument("--host", default=DEFAULT_HTTP_HOST)
    parser.add_argument("--port", type=int, default=DEFAULT_HTTP_PORT)
    parser.add_argument("--pidfile", default=None)
    parser.add_argument("--unity-instance-token", default=None)
    args = parser.parse_args(argv)
    run_server(transport=args.transport, host=args.host, port=args.port, pidfile=args.pidfile)


def _write_pidfile(pidfile: str | None) -> None:
    if not pidfile:
        return

    os.makedirs(os.path.dirname(os.path.abspath(pidfile)), exist_ok=True)
    with open(pidfile, "w", encoding="utf-8") as file:
        file.write(str(os.getpid()))

    def cleanup() -> None:
        try:
            if os.path.exists(pidfile):
                os.remove(pidfile)
        except OSError:
            pass

    atexit.register(cleanup)
