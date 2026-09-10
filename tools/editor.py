#!/usr/bin/env python3
"""Editor de cartas de Merliot.

    python3 tools/editor.py

Levanta un servidor local y abre el navegador. Escribe `data/merliot.json`
de verdad — no descarga una copia — preservando formato y orden de claves
para que los diffs de git se puedan leer.

El brief está en docs/editor-de-cartas.md. Lo importante de ahí: un editor
que sea sólo un formulario es peor que editar el JSON a mano. El valor está
en ver las consecuencias mientras editás, así que la mitad de esto es la
canonicalización que mantiene el diff limpio y los chequeos que corren solos.
"""

import argparse
import http.server
import json
import os
import shutil
import socketserver
import sys
import tempfile
import threading
import webbrowser
from collections import OrderedDict
from datetime import datetime
from pathlib import Path

RAIZ = Path(__file__).resolve().parent.parent
# data/merliot.json es un symlink al archivo real dentro de Assets/. Hay que
# resolverlo: si escribimos sobre el symlink con os.replace lo reemplazamos
# por un archivo suelto y se acabó la fuente de verdad única.
DATOS = (RAIZ / "data" / "merliot.json").resolve()
ARTE = RAIZ / "Assets" / "Resources" / "Arte" / "Cartas"
RESPALDOS = RAIZ / "tools" / ".respaldos"
MAX_RESPALDOS = 20

RECURSOS = ["vigor", "temple", "destreza", "saber"]
TIPOS = ["perm", "mejora", "amuleto", "uso"]
# El orden de Efecto es el de MerliotData.cs: primero lo que te hace el
# enemigo, después lo que hace una carta de uso.
EFECTO = ["dmg", "dmgTodos", "dmgFondo", "dmgDado", "rotar", "descartar", "brota",
          "cura", "robar", "dar", "nido", "resolver", "revivir", "tresdados", "huir"]


# ── canonicalización ──────────────────────────────────────────────────────
# Una sola implementación, acá. El navegador manda valores; el orden de las
# claves y qué se omite lo decide el servidor, así que una carta que no tocaste
# vuelve al archivo byte por byte como estaba.

def _entero(v):
    try:
        return int(v)
    except (TypeError, ValueError):
        return 0


def _produccion(p):
    if not isinstance(p, dict):
        return None
    o = OrderedDict((r, _entero(p.get(r))) for r in RECURSOS if _entero(p.get(r)))
    return o or None


def _banda(b):
    o = OrderedDict(desde=_entero(b.get("desde")), hasta=_entero(b.get("hasta")))
    pr = _produccion(b.get("produce"))
    if pr:
        o["produce"] = pr
    for k in ("cristales", "cura", "roba"):
        if _entero(b.get(k)):
            o[k] = _entero(b[k])
    if b.get("porCadaRaza"):
        o["porCadaRaza"] = b["porCadaRaza"]
    return o


def _efecto(e):
    if not isinstance(e, dict):
        return None
    o = OrderedDict()
    for k in EFECTO:
        if k == "dar":
            d = _produccion(e.get("dar"))
            if d:
                o["dar"] = d
        elif _entero(e.get(k)):
            o[k] = _entero(e[k])
    return o or None


def canonizar_carta(c):
    """Devuelve la carta con las claves en el orden del archivo y sin las vacías."""
    o = OrderedDict()
    o["id"] = c["id"]
    o["nombre"] = c["nombre"]
    o["tipo"] = c["tipo"]
    # copias 0 es significativo (la semilla y las mutaciones), así que va siempre.
    o["copias"] = _entero(c.get("copias"))
    o["costoCristales"] = _entero(c.get("costoCristales"))
    o["texto"] = c.get("texto", "")

    tipo = c["tipo"]
    if tipo == "perm":
        if c.get("raza"):
            o["raza"] = c["raza"]
        o["vida"] = _entero(c.get("vida"))
    if tipo == "mejora" and c.get("soloRaza"):
        o["soloRaza"] = c["soloRaza"]

    ac = c.get("alCaer")
    if isinstance(ac, dict) and (_entero(ac.get("cris")) or _entero(ac.get("curaTodos"))):
        a = OrderedDict()
        for k in ("cris", "curaTodos"):
            if _entero(ac.get(k)):
                a[k] = _entero(ac[k])
        o["alCaer"] = a

    if tipo == "amuleto" and c.get("pasiva"):
        o["pasiva"] = c["pasiva"]
    if c.get("efectoTexto"):
        o["efectoTexto"] = c["efectoTexto"]
    ef = _efecto(c.get("efecto"))
    if ef:
        o["efecto"] = ef
    if c.get("bandas"):
        o["bandas"] = [_banda(b) for b in c["bandas"]]
    return o


# ── guardar sin romper nada ───────────────────────────────────────────────

def revisar(doc, previo):
    """Motivos para no escribir. Esto guarda la fuente de verdad del proyecto:
    ante la duda, no se escribe."""
    if not isinstance(doc, dict):
        return "lo que llegó no es un objeto JSON"

    faltan = [k for k in previo if k not in doc]
    if faltan:
        return f"faltan secciones enteras: {', '.join(faltan)}"

    cartas = doc.get("cartas")
    if not isinstance(cartas, list) or not cartas:
        return "no hay cartas"

    vistos = set()
    for i, c in enumerate(cartas):
        if not isinstance(c, dict):
            return f"la carta {i} no es un objeto"
        if not c.get("id"):
            return f"la carta {i} no tiene id"
        if not c.get("nombre"):
            return f"{c['id']}: sin nombre"
        if c.get("tipo") not in TIPOS:
            return f"{c['id']}: tipo '{c.get('tipo')}' no existe"
        if c["id"] in vistos:
            return f"el id '{c['id']}' está repetido"
        vistos.add(c["id"])

    nombres = {c["nombre"] for c in cartas}
    for e in doc.get("mazoInicial") or []:
        if e.get("carta") not in nombres:
            return f"el mazo inicial pide '{e.get('carta')}', que ya no existe"
    return None


def respaldar(texto):
    RESPALDOS.mkdir(parents=True, exist_ok=True)
    sello = datetime.now().strftime("%Y%m%d-%H%M%S")
    (RESPALDOS / f"merliot-{sello}.json").write_text(texto, encoding="utf-8")
    viejos = sorted(RESPALDOS.glob("merliot-*.json"))[:-MAX_RESPALDOS]
    for v in viejos:
        v.unlink()


def guardar(doc):
    previo = json.loads(DATOS.read_text(encoding="utf-8"), object_pairs_hook=OrderedDict)
    error = revisar(doc, previo)
    if error:
        return error

    doc["cartas"] = [canonizar_carta(c) for c in doc["cartas"]]
    texto = json.dumps(doc, ensure_ascii=False, indent=2) + "\n"

    respaldar(DATOS.read_text(encoding="utf-8"))
    # Escritura atómica, en el mismo directorio para que el rename no cruce
    # sistemas de archivos. Si algo explota, el archivo viejo queda intacto.
    fd, tmp = tempfile.mkstemp(dir=str(DATOS.parent), prefix=".merliot-", suffix=".json")
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            f.write(texto)
        os.replace(tmp, DATOS)
    except BaseException:
        Path(tmp).unlink(missing_ok=True)
        raise
    return None


def ids_con_arte():
    if not ARTE.is_dir():
        return []
    return sorted({p.stem for p in ARTE.iterdir()
                   if p.is_file() and not p.name.startswith(".")
                   and p.suffix.lower() != ".meta"})


# ── servidor ──────────────────────────────────────────────────────────────

class Mano(http.server.BaseHTTPRequestHandler):
    def responder(self, codigo, cuerpo, tipo="application/json; charset=utf-8"):
        datos = cuerpo.encode("utf-8") if isinstance(cuerpo, str) else cuerpo
        self.send_response(codigo)
        self.send_header("Content-Type", tipo)
        self.send_header("Content-Length", str(len(datos)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(datos)

    def do_GET(self):
        ruta = self.path.split("?")[0]
        if ruta in ("/", "/index.html"):
            html = (Path(__file__).resolve().parent / "editor.html").read_text(encoding="utf-8")
            return self.responder(200, html, "text/html; charset=utf-8")
        if ruta == "/datos":
            return self.responder(200, DATOS.read_text(encoding="utf-8"))
        if ruta == "/arte":
            return self.responder(200, json.dumps({"ids": ids_con_arte()}))
        if ruta == "/donde":
            return self.responder(200, json.dumps({
                "archivo": str(DATOS),
                "enlace": str(RAIZ / "data" / "merliot.json"),
            }))
        self.responder(404, json.dumps({"error": "no existe"}))

    def do_PUT(self):
        if self.path.split("?")[0] != "/datos":
            return self.responder(404, json.dumps({"error": "no existe"}))
        n = int(self.headers.get("Content-Length") or 0)
        try:
            doc = json.loads(self.rfile.read(n).decode("utf-8"),
                             object_pairs_hook=OrderedDict)
        except json.JSONDecodeError as e:
            return self.responder(400, json.dumps({"error": f"JSON inválido: {e}"}))
        try:
            error = guardar(doc)
        except OSError as e:
            return self.responder(500, json.dumps({"error": f"no se pudo escribir: {e}"}))
        if error:
            return self.responder(400, json.dumps({"error": error}))
        return self.responder(200, json.dumps({"ok": True, "archivo": str(DATOS)}))

    def log_message(self, formato, *args):
        if "--ruidoso" in sys.argv:
            super().log_message(formato, *args)


class Servidor(socketserver.ThreadingTCPServer):
    allow_reuse_address = True
    daemon_threads = True


def main():
    ap = argparse.ArgumentParser(description="Editor de cartas de Merliot")
    ap.add_argument("--puerto", type=int, default=8731)
    ap.add_argument("--no-abrir", action="store_true", help="no abrir el navegador")
    ap.add_argument("--ruidoso", action="store_true", help="loguear cada request")
    args = ap.parse_args()

    if not DATOS.is_file():
        sys.exit(f"No encuentro {DATOS}")

    url = f"http://127.0.0.1:{args.puerto}/"
    with Servidor(("127.0.0.1", args.puerto), Mano) as srv:
        print(f"Editor de cartas  →  {url}")
        print(f"Escribe en        →  {DATOS}")
        print(f"Respaldos en      →  {RESPALDOS}  (los últimos {MAX_RESPALDOS})")
        print("Ctrl-C para cortar.")
        if not args.no_abrir:
            threading.Timer(0.4, lambda: webbrowser.open(url)).start()
        try:
            srv.serve_forever()
        except KeyboardInterrupt:
            print("\nChau.")


if __name__ == "__main__":
    main()
