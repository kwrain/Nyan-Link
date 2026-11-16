#!/usr/bin/env python3
"""Package a Unity Web release without server credentials or network writes."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import zipfile


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--build", type=Path, default=Path("Builds/Web/nyan-link"))
    parser.add_argument("--output", type=Path, default=Path("Builds/Web/nyan-link-deploy.zip"))
    args = parser.parse_args()
    build = args.build.resolve()
    index = build / "index.html"
    if not index.is_file() or "{{{" in index.read_text():
        raise SystemExit("Missing or unprocessed Unity Web build")
    if not list((build / "Build").glob("*.wasm.unityweb")):
        raise SystemExit("Expected gzip fallback WebAssembly build")
    license_root = Path("Assets/Resources/Fonts/Galmuri")
    for source, name in [(license_root / "LICENSE.txt", "Galmuri-LICENSE.txt"),
                         (license_root / "NOTICE.txt", "Galmuri-NOTICE.txt"),
                         (Path("Tools/WebHosting/apache.htaccess"), ".htaccess")]:
        shutil.copyfile(source, build / name)
    files = []
    for path in sorted(build.rglob("*")):
        if path.is_file() and path.name != "release.json":
            files.append({"path": path.relative_to(build).as_posix(), "bytes": path.stat().st_size,
                          "sha256": sha256(path)})
    manifest = {"url": "https://fairstudios.kr/nyan-link/", "compression": "gzip-with-unity-fallback",
                "files": files}
    (build / "release.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(args.output, "w", zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(build.rglob("*")):
            if path.is_file():
                archive.write(path, "nyan-link/" + path.relative_to(build).as_posix())
    digest = sha256(args.output)
    args.output.with_suffix(args.output.suffix + ".sha256").write_text(digest + "  " + args.output.name + "\n")
    print(json.dumps({"archive": str(args.output.resolve()), "bytes": args.output.stat().st_size,
                      "sha256": digest, "files": len(files) + 1}, indent=2))


if __name__ == "__main__":
    main()
