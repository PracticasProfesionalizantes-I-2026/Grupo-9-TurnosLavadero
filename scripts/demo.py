#!/usr/bin/env python3
"""Demo aislada: HTTP real, JWT real y SMTP local. Solo usa Python estándar y .NET 10."""
import argparse
import datetime as dt
import html
import json
import os
from pathlib import Path
import shutil
import secrets
import socket
import socketserver
import subprocess
import tempfile
import threading
import time
import urllib.error
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
MESSAGES = []


class SmtpHandler(socketserver.StreamRequestHandler):
    def handle(self):
        self.wfile.write(b"220 localhost demo SMTP\r\n")
        while line := self.rfile.readline():
            command = line.decode("ascii", errors="replace").strip().upper()
            if command.startswith(("EHLO", "HELO")):
                self.wfile.write(b"250 localhost\r\n")
            elif command == "DATA":
                self.wfile.write(b"354 End with dot\r\n")
                content = bytearray()
                while (part := self.rfile.readline()) not in (b".\r\n", b""):
                    content.extend(part[1:] if part.startswith(b"..") else part)
                MESSAGES.append(bytes(content))
                self.wfile.write(b"250 Captured locally\r\n")
            elif command == "QUIT":
                self.wfile.write(b"221 Bye\r\n")
                return
            else:
                self.wfile.write(b"250 OK\r\n")


def free_port():
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


def redact(value):
    if isinstance(value, dict):
        return {k: "[omitido]" if k.lower() in ("token", "password", "passwordhash") else redact(v)
                for k, v in value.items()}
    if isinstance(value, list):
        return [redact(v) for v in value]
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", default="TestResults/demo")
    parser.add_argument("--dotnet", default=shutil.which("dotnet"))
    args = parser.parse_args()
    if not args.dotnet:
        parser.error("Se requiere el SDK .NET 10 en PATH o --dotnet RUTA.")
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    MESSAGES.clear()
    steps = []
    admin_password = secrets.token_urlsafe(24)
    client_password = secrets.token_urlsafe(24)
    other_password = secrets.token_urlsafe(24)
    port = free_port()
    base = f"http://127.0.0.1:{port}"

    def request(label, method, path, expected, data=None, token=None):
        body = json.dumps(data).encode() if data is not None else None
        headers = {"Content-Type": "application/json"}
        if token:
            headers["Authorization"] = "Bearer " + token
        req = urllib.request.Request(base + path, data=body, headers=headers, method=method)
        try:
            response = urllib.request.urlopen(req, timeout=30)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            raw = response.read().decode()
            result = json.loads(raw) if raw else None
            status = response.status
        steps.append({"paso": label, "metodo": method, "ruta": path, "esperado": expected,
                      "obtenido": status, "correcto": status == expected,
                      "peticion": redact(data), "respuesta": redact(result)})
        print(f"{'OK' if status == expected else 'ERROR'} {status} {label}", flush=True)
        if status != expected:
            raise AssertionError(f"{label}: esperaba {expected}, recibió {status}: {raw}")
        return result

    with socketserver.ThreadingTCPServer(("127.0.0.1", 0), SmtpHandler) as smtp:
        smtp.daemon_threads = True
        threading.Thread(target=smtp.serve_forever, daemon=True).start()
        with tempfile.TemporaryDirectory(prefix="lavadero-demo-") as temporary:
            env = dict(os.environ)
            env.update({"ASPNETCORE_ENVIRONMENT": "Development", "ASPNETCORE_URLS": base,
                        "ConnectionStrings__DefaultConnection": f"Data Source={Path(temporary) / 'demo.db'}",
                        "TURNOSLAVADERO_SEED_ADMIN_EMAIL": "admin@lavadero.local",
                        "TURNOSLAVADERO_SEED_ADMIN_PASSWORD": admin_password,
                        "Recordatorios__Habilitados": "false", "Smtp__Host": "127.0.0.1",
                        "Smtp__Port": str(smtp.server_address[1]), "Smtp__EnableSsl": "false",
                        "Smtp__From": "demo@example.test", "Smtp__User": "", "Smtp__Password": ""})
            with (output / "api.log").open("w") as log:
                process = subprocess.Popen([args.dotnet, "run", "--no-build", "--no-launch-profile",
                                            "--project", str(ROOT / "API")], cwd=ROOT, env=env,
                                           stdout=log, stderr=subprocess.STDOUT)
                try:
                    for _ in range(180):
                        if process.poll() is not None:
                            raise RuntimeError("La API terminó al iniciar. Ver api.log.")
                        try:
                            with urllib.request.urlopen(base + "/openapi/v1.json", timeout=1):
                                break
                        except (urllib.error.URLError, TimeoutError):
                            time.sleep(0.5)
                    else:
                        raise TimeoutError("La API no inició en 90 segundos. Ver api.log.")

                    admin = request("CU-08 Inicio de sesión administrador", "POST", "/api/auth/login", 200,
                                    {"email": "admin@lavadero.local", "password": admin_password})["token"]
                    service = request("CU-06 Alta de servicio", "POST", "/api/servicios", 201,
                                      {"nombre": "Lavado demostración", "importe": 12000}, admin)["id"]
                    account = request("CU-07 Registro de cliente", "POST", "/api/auth/registro", 201,
                                      {"nombre": "Cliente", "apellido": "Demostración",
                                       "email": "cliente.demo@example.test", "password": client_password})
                    token = account["token"]
                    request("RNF-01 Acceso sin token", "GET", "/api/servicios", 401)
                    request("CU-13 Consulta de servicios", "GET", "/api/servicios", 200, token=token)
                    date = (dt.datetime.now(dt.timezone.utc) + dt.timedelta(days=7)).replace(microsecond=0)
                    create = {"servicioId": service, "fechaHora": date.isoformat()}
                    turno = request("CU-01 Reserva del cliente autenticado", "POST", "/api/turnos", 201,
                                    create, token)["id"]
                    request("RN-01 Horario ocupado", "POST", "/api/turnos", 409, create, token)
                    other = request("Registro de segundo cliente", "POST", "/api/auth/registro", 201,
                                    {"nombre": "Otro", "apellido": "Cliente", "email": "otro.demo@example.test",
                                     "password": other_password})["token"]
                    update = {"servicioId": service, "fechaHora": (date + dt.timedelta(hours=1)).isoformat()}
                    request("RNF-02 Rechazo de modificación ajena", "PUT", f"/api/turnos/{turno}", 403, update, other)
                    request("CU-04 Modificación por el titular", "PUT", f"/api/turnos/{turno}", 200, update, token)
                    local_day = (date + dt.timedelta(hours=1)).astimezone(dt.timezone(dt.timedelta(hours=-3))).date()
                    agenda = request("CU-11 Agenda del día argentino", "GET", f"/api/turnos/agenda?fecha={local_day}",
                                     200, token=admin)
                    assert turno in [x["id"] for x in agenda], "El turno debe figurar en la agenda."
                    query = urllib.parse.urlencode({"desde": date.isoformat(), "hasta": (date + dt.timedelta(hours=2)).isoformat()})
                    reminder = request("CU-02 Recordatorio por SMTP local", "POST", "/api/recordatorios/procesar?" + query,
                                       200, token=admin)
                    assert reminder["enviados"] == 1 and len(MESSAGES) == 1, "Debe capturarse un correo."
                    repeat = request("CU-02 Segundo procesamiento sin reenvío", "POST", "/api/recordatorios/procesar?" + query,
                                     200, token=admin)
                    assert repeat["procesados"] == 0 and len(MESSAGES) == 1, "No debe reenviarse el correo."
                    request("CU-05 Cancelación del turno", "DELETE", f"/api/turnos/{turno}", 204, token=token)
                    query = urllib.parse.urlencode({"fechaHora": update["fechaHora"]})
                    available = request("CU-09 Horario liberado", "GET", "/api/turnos/disponibilidad?" + query,
                                        200, token=token)
                    assert available["disponible"] is True
                    history = request("CU-10 Historial de turnos propios", "GET", "/api/turnos/mis-turnos", 200, token=token)
                    assert any(x["id"] == turno and x["estado"] == "Cancelado" for x in history)
                    request("Validación de datos incompletos", "POST", "/api/turnos", 400, {}, token)
                finally:
                    process.terminate()
                    try:
                        process.wait(timeout=15)
                    except subprocess.TimeoutExpired:
                        process.kill()
                        process.wait()
                    smtp.shutdown()

    summary = {"fechaUtc": dt.datetime.now(dt.timezone.utc).isoformat(), "pasos": steps,
               "correosCapturadosLocalmente": len(MESSAGES), "entregaExterna": False}
    (output / "resultados.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    from email import policy
    from email.parser import BytesParser
    email = BytesParser(policy=policy.default).parsebytes(MESSAGES[0])
    message_text = email.get_body(preferencelist=("plain",)).get_content() if email.is_multipart() else email.get_content()
    sections = "".join(f'<section><h2>{html.escape(x["paso"])}</h2><p><b>{x["obtenido"]}</b> '
                       f'{x["metodo"]} {html.escape(x["ruta"])}</p><pre>'
                       f'{html.escape(json.dumps(x["respuesta"], ensure_ascii=False, indent=2))}</pre></section>' for x in steps)
    report = f'''<!doctype html><html lang="es"><meta charset="utf-8"><title>Demo verificada del lavadero</title>
<style>body{{font:17px system-ui;margin:40px auto;max-width:1100px;padding:0 24px;color:#172b3a;background:#f4f7fa}}
h1{{font-size:36px}}section{{background:white;padding:20px;margin:20px 0;border-left:5px solid #008c80}}
pre{{white-space:pre-wrap;overflow-wrap:anywhere;font-size:14px}}b{{color:#007365}}</style>
<h1>Sistema de turnos para lavadero</h1><p>Grupo 9 · Juan Jesús Baigorria y Tomás Martín Ponce</p>
<p>Ejecución: {summary['fechaUtc']} · {len(steps)} peticiones verificadas · JWT real · SQLite temporal</p>
<p>El correo se capturó en un servidor SMTP local. Esta evidencia no acredita entrega a una casilla externa.</p>
{sections}<section><h2>Correo capturado</h2><pre>{html.escape(str(email['Subject']))}\n\n{html.escape(message_text)}</pre></section></html>'''
    (output / "demo.html").write_text(report, encoding="utf-8")
    print(f"Demo completada: {len(steps)} respuestas verificadas y {len(MESSAGES)} correo local. {output / 'demo.html'}")


if __name__ == "__main__":
    main()
