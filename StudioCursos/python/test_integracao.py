"""Integra API real, gRPC real e arquivo SQLite; HTML local substitui a internet."""
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from contextlib import closing
from pathlib import Path
import json
import os
import socket
import sqlite3
import subprocess
import sys
import tempfile
import threading
import time
import unittest
import grpc
import requests

ROOT = Path(__file__).resolve().parents[1]
HTML = '<article class="product_pod"><h3><a title="Estudo de composicao">Livro</a></h3><p class="price_color">£12.50</p></article><article class="product_pod"><h3><a title="Caderno de desenho">Livro</a></h3><p class="price_color">£8.00</p></article>'


def porta():
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


class Fonte(BaseHTTPRequestHandler):
    falhar = False

    def do_GET(self):
        self.send_response(503 if type(self).falhar else 200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.end_headers()
        self.wfile.write(HTML.encode())

    def log_message(self, *args):
        pass


class Integracao(unittest.TestCase):
    def test_curso_persistencia_importacao_e_falhas(self):
        processes, logs = [], []
        fonte = ThreadingHTTPServer(("127.0.0.1", 0), Fonte)
        thread = threading.Thread(target=fonte.serve_forever, daemon=True)
        thread.start()
        with tempfile.TemporaryDirectory(prefix="studio-cursos-") as temp:
            api_port, grpc_port = porta(), porta()
            base = f"http://127.0.0.1:{api_port}"
            db_path = Path(temp) / "studio.db"
            env = os.environ.copy()
            env.update(Database__Path=str(db_path), PythonGrpc__Url=f"http://127.0.0.1:{grpc_port}",
                       GRPC_BIND=f"127.0.0.1:{grpc_port}", SCRAPER_URL=f"http://127.0.0.1:{fonte.server_port}/")

            def start(args, cwd):
                log = open(Path(temp) / f"process-{len(logs)}.log", "w", encoding="utf-8")
                logs.append(log)
                process = subprocess.Popen(args, cwd=cwd, env=env, stdout=log, stderr=subprocess.STDOUT,
                                           creationflags=subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0)
                processes.append(process)
                return process

            def start_api():
                process = start(["dotnet", str(ROOT / "Api/bin/Debug/net10.0/StudioCursos.Api.dll"),
                                 "--urls", base, "--environment", "Development"], ROOT / "Api")
                for _ in range(100):
                    if process.poll() is not None:
                        raise AssertionError("API encerrou antes de iniciar")
                    try:
                        if requests.get(base + "/health", timeout=1).status_code == 200:
                            return process
                    except requests.RequestException:
                        pass
                    time.sleep(.2)
                raise AssertionError("API nao iniciou")

            try:
                python = start([sys.executable, "server.py"], ROOT / "python")
                with grpc.insecure_channel(f"127.0.0.1:{grpc_port}") as channel:
                    grpc.channel_ready_future(channel).result(timeout=15)
                api = start_api()
                payload = json.loads((ROOT / "exemplos/curso.json").read_text(encoding="utf-8-sig"))
                created = requests.post(base + "/api/cursos", json=payload, timeout=10)
                self.assertEqual(201, created.status_code, created.text)
                course_id = created.json()["id"]
                url = base + "/api/cursos/" + course_id
                self.assertEqual(3, len(created.json()["modulos"]))

                # Reinicio com o mesmo arquivo comprova persistencia real.
                api.terminate(); api.wait(timeout=10)
                api = start_api()
                saved = requests.get(url, timeout=10).json()
                self.assertEqual(payload["titulo"], saved["titulo"])
                self.assertEqual(3, len(saved["modulos"]))

                imported = requests.post(url + "/referencias/importar", timeout=15)
                self.assertEqual(200, imported.status_code, imported.text)
                self.assertEqual(2, imported.json()["adicionadas"])
                repeated = requests.post(url + "/referencias/importar", timeout=15)
                self.assertEqual(200, repeated.status_code, repeated.text)
                self.assertEqual(0, repeated.json()["adicionadas"])

                invalid = dict(payload, modulos=[payload["modulos"][0], payload["modulos"][0]])
                self.assertEqual(400, requests.put(url, json=invalid, timeout=10).status_code)
                payload["estudio"] = "Atelie Personalizado"
                payload["modulos"] = payload["modulos"][:2]
                updated = requests.put(url, json=payload, timeout=10)
                self.assertEqual(200, updated.status_code, updated.text)
                self.assertEqual("Atelie Personalizado", updated.json()["estudio"])
                self.assertEqual(2, len(updated.json()["modulos"]))
                self.assertEqual(2, len(updated.json()["referencias"]))

                Fonte.falhar = True
                self.assertEqual(503, requests.post(url + "/referencias/importar", timeout=15).status_code)
                with closing(sqlite3.connect(db_path)) as db:
                    self.assertEqual(1, db.execute('SELECT count(*) FROM "Cursos"').fetchone()[0])
                    self.assertEqual(2, db.execute('SELECT count(*) FROM "Referencias"').fetchone()[0])
                    self.assertEqual(2, db.execute('SELECT count(*) FROM "Modulos"').fetchone()[0])

                # O cadastro e a leitura funcionam mesmo com Python fora do ar.
                python.terminate(); python.wait(timeout=10)
                self.assertEqual(200, requests.get(url, timeout=10).status_code)
                self.assertEqual(503, requests.post(url + "/referencias/importar", timeout=15).status_code)
                self.assertEqual(204, requests.delete(url, timeout=10).status_code)
                self.assertEqual(404, requests.get(url, timeout=10).status_code)
                with closing(sqlite3.connect(db_path)) as db:
                    self.assertEqual(0, db.execute('SELECT count(*) FROM "Referencias"').fetchone()[0])
                    self.assertEqual(0, db.execute('SELECT count(*) FROM "Modulos"').fetchone()[0])
                print("PASSOU: criar, reiniciar/persistir, importar por gRPC, nao duplicar, validar, personalizar, tratar falhas e excluir em cascata.")
            finally:
                Fonte.falhar = False
                for process in processes:
                    if process.poll() is None:
                        process.terminate()
                        try:
                            process.wait(timeout=10)
                        except subprocess.TimeoutExpired:
                            process.kill(); process.wait(timeout=5)
                for log in logs:
                    log.close()
                fonte.shutdown(); fonte.server_close()


if __name__ == "__main__":
    unittest.main()
