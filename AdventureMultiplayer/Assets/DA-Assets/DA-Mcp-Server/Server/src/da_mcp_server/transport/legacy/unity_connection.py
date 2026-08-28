from __future__ import annotations

import json
import socket
from typing import Any

from da_mcp_server.constants import FRAMING_HANDSHAKE
from da_mcp_server.transport.legacy.framing import decode_frame_header, encode_frame


class UnityConnection:
    def __init__(self, host: str = "127.0.0.1", port: int | None = None, timeout: float = 30.0) -> None:
        self.host = host
        self.port = port
        self.timeout = timeout
        self.sock: socket.socket | None = None

    def connect(self) -> None:
        if self.port is None:
            raise ValueError("port is required")
        sock = socket.create_connection((self.host, self.port), timeout=self.timeout)
        sock.settimeout(self.timeout)
        handshake = sock.recv(128).decode("ascii", errors="ignore")
        if FRAMING_HANDSHAKE not in handshake:
            sock.close()
            raise ConnectionError(f"DA MCP requires {FRAMING_HANDSHAKE}, got: {handshake!r}")
        self.sock = sock

    def close(self) -> None:
        if self.sock is not None:
            self.sock.close()
            self.sock = None

    def send_command(self, command_type: str, params: dict[str, Any]) -> dict[str, Any]:
        if self.sock is None:
            self.connect()
        assert self.sock is not None

        payload = json.dumps({"type": command_type, "params": params}).encode("utf-8")
        self.sock.sendall(encode_frame(payload))
        response = self._read_response()
        return json.loads(response.decode("utf-8"))

    def _read_response(self) -> bytes:
        assert self.sock is not None
        while True:
            header = self._read_exact(8)
            payload_len = decode_frame_header(header)
            if payload_len == 0:
                continue
            return self._read_exact(payload_len)

    def _read_exact(self, size: int) -> bytes:
        assert self.sock is not None
        chunks: list[bytes] = []
        remaining = size
        while remaining > 0:
            chunk = self.sock.recv(remaining)
            if not chunk:
                raise ConnectionError("Unity connection closed")
            chunks.append(chunk)
            remaining -= len(chunk)
        return b"".join(chunks)
