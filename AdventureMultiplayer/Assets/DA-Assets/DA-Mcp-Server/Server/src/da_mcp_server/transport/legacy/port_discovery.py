from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class UnityPortRecord:
    project_hash: str
    port: int
    status: str
    path: Path


def state_dir() -> Path:
    return Path.home() / ".da-mcp" / "unity"


def discover_port_records(root: Path | None = None) -> list[UnityPortRecord]:
    root = root or state_dir()
    if not root.exists():
        return []

    records: list[UnityPortRecord] = []
    for path in root.glob("*.json"):
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
            project_hash = str(data["project_hash"])
            port = int(data["port"])
            status = str(data.get("status", "unknown"))
        except (OSError, KeyError, TypeError, ValueError, json.JSONDecodeError):
            continue
        records.append(UnityPortRecord(project_hash=project_hash, port=port, status=status, path=path))
    return records
