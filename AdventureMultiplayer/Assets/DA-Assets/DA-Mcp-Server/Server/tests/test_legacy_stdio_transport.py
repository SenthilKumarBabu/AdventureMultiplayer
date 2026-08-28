import struct

import pytest

from da_mcp_server.constants import FRAMED_MAX
from da_mcp_server.transport.legacy.framing import decode_frame_header, encode_frame


def test_frame_uses_8_byte_big_endian_unsigned_length_prefix():
    payload = b'{"type":"ping"}'

    frame = encode_frame(payload)

    assert frame[:8] == struct.pack(">Q", len(payload))
    assert frame[8:] == payload
    assert decode_frame_header(frame[:8]) == len(payload)


def test_frame_rejects_payload_larger_than_64_mib():
    oversized_header = struct.pack(">Q", FRAMED_MAX + 1)

    with pytest.raises(ValueError):
        decode_frame_header(oversized_header)
