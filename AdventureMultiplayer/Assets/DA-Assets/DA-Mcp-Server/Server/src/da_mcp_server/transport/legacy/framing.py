from __future__ import annotations

import struct

from da_mcp_server.constants import FRAMED_MAX


def encode_frame(payload: bytes) -> bytes:
    if len(payload) > FRAMED_MAX:
        raise ValueError(f"Invalid framed length: {len(payload)}")
    return struct.pack(">Q", len(payload)) + payload


def decode_frame_header(header: bytes) -> int:
    if len(header) != 8:
        raise ValueError("Frame header must be exactly 8 bytes")
    payload_len = struct.unpack(">Q", header)[0]
    if payload_len > FRAMED_MAX:
        raise ValueError(f"Invalid framed length: {payload_len}")
    return payload_len
