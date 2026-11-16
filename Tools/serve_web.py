#!/usr/bin/env python3
"""Serve the exact deployment path locally; never expose the source repository."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class Handler(SimpleHTTPRequestHandler):
    extensions_map = {**SimpleHTTPRequestHandler.extensions_map,
                      ".js": "application/javascript", ".wasm": "application/wasm",
                      ".unityweb": "application/octet-stream"}

    def end_headers(self):
        self.send_header("Cache-Control", "no-cache")
        super().end_headers()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path("Builds/Web"))
    parser.add_argument("--port", type=int, default=8080)
    args = parser.parse_args()
    root = args.root.resolve()
    if not (root / "nyan-link/index.html").is_file():
        raise SystemExit("Build first; expected nyan-link/index.html under --root")
    server = ThreadingHTTPServer(("127.0.0.1", args.port), partial(Handler, directory=str(root)))
    print(f"http://127.0.0.1:{args.port}/nyan-link/", flush=True)
    server.serve_forever()
