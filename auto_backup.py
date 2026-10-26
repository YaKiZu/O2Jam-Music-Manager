# -*- coding: utf-8 -*-
r"""
Automatic backup utility for O2Jam Music Manager codebase.
Copies all files and folders to A:\Backup App with a timestamped folder.
"""

import os
import shutil
from datetime import datetime
from pathlib import Path


def perform_backup(src_dir: str = r"A:\Antigravity app", dest_parent: str = r"A:\Backup App") -> Path:
    src = Path(src_dir)
    dest_root = Path(dest_parent)
    dest_root.mkdir(parents=True, exist_ok=True)

    timestamp = datetime.now().strftime("%Y-%m-%d_%H%M%S")
    backup_dir = dest_root / f"O2Jam_Manager_Backup_{timestamp}"
    backup_dir.mkdir(parents=True, exist_ok=True)

    ignored_patterns = shutil.ignore_patterns(
        "*.tmp", "*~", "__pycache__", ".pytest_cache", ".vs", "obj", "*.log", "crash_log.txt"
    )

    for item in src.iterdir():
        # Do not backup Backup App into itself if nested
        if item.resolve() == dest_root.resolve():
            continue
        dest_item = backup_dir / item.name
        if item.is_dir():
            shutil.copytree(item, dest_item, ignore=ignored_patterns, dirs_exist_ok=True)
        else:
            shutil.copy2(item, dest_item)

    print(f"[AUTO-BACKUP] Backup successfully created at: {backup_dir}")
    return backup_dir


if __name__ == "__main__":
    perform_backup()
