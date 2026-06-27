import os
import json
import urllib.request
import urllib.error

GEMINI_API_KEY = os.environ["GEMINI_API_KEY"]
PR_TITLE       = os.environ.get("PR_TITLE", "PR")
PR_NUMBER      = os.environ["PR_NUMBER"]
GITHUB_TOKEN   = os.environ["GITHUB_TOKEN"]
REPO           = os.environ["REPO"]

# Leer diff
with open("pr.diff", "r", encoding="utf-8") as f:
    diff = f.read()[:12000]  # limitar tamaño

print(f"📋 Analizando PR #{PR_NUMBER}: {PR_TITLE}")
print(f"📄 Tamaño del diff: {len(diff)} caracteres")

# ── Llamar a Gemini ──────────────────────────────────────────────────────────
prompt = f"""Sos un Senior Developer especializado en code review exhaustivo.
Analizá el siguiente Pull Request llamado "{PR_TITLE}" y generá un informe en Markdown en español.

El informe debe incluir:
1. 📋 **RESUMEN** — qué cambia y nivel de impacto (crítico/alto/medio/bajo)
2. 🐛 **BUGS IDENTIFICADOS** — cada bug con severidad (🔴 CRÍTICO / 🟠 ALTO / 🟡 MEDIO / 🔵 BAJO), causa raíz e impacto
3. ⚡ **PERFORMANCE** — anti-patterns, N+1, enumeraciones múltiples, I/O en loops
4. 🔒 **SEGURIDAD** — vulnerabilidades, credenciales hardcodeadas, validaciones faltantes
5. 🏗️ **CALIDAD DE CÓDIGO** — SOLID, clean code, TODO/FIXME sin ticket, código duplicado
6. ✅ **VEREDICTO FINAL** — una de estas tres opciones:
   - ✅ APROBADO
   - ⚠️ APROBADO CON COMENTARIOS
   - ❌ REQUIERE CAMBIOS

Sé técnico, específico y citá líneas de código cuando sea relevante.

GIT DIFF:
{diff}
"""

payload = {
    "contents": [{"parts": [{"text": prompt}]}],
    "generationConfig": {"maxOutputTokens": 2048, "temperature": 0.3}
}

url = f"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={GEMINI_API_KEY}"
req = urllib.request.Request(
    url,
    data=json.dumps(payload).encode("utf-8"),
    headers={"Content-Type": "application/json"},
    method="POST"
)

try:
    with urllib.request.urlopen(req) as resp:
        data = json.loads(resp.read())
    analysis = data["candidates"][0]["content"]["parts"][0]["text"]
    print("✅ Análisis generado correctamente")
except urllib.error.HTTPError as e:
    error_body = e.read().decode()
    print(f"❌ Error Gemini: {e.code} - {error_body}")
    analysis = f"❌ Error al generar el análisis: {e.code}"

# ── Publicar comentario en el PR ─────────────────────────────────────────────
comment_body = f"""## 🤖 Análisis de PR — Agente Único (Gemini)

> Análisis automático generado por IA para el PR **#{PR_NUMBER}: {PR_TITLE}**

---

{analysis}

---
<sub>🤖 Generado automáticamente por el PR Review Agent | Modo: Agente Único</sub>
"""

comment_payload = json.dumps({"body": comment_body}).encode("utf-8")
comment_url = f"https://api.github.com/repos/{REPO}/issues/{PR_NUMBER}/comments"
comment_req = urllib.request.Request(
    comment_url,
    data=comment_payload,
    headers={
        "Authorization": f"Bearer {GITHUB_TOKEN}",
        "Content-Type": "application/json",
        "Accept": "application/vnd.github+json"
    },
    method="POST"
)

try:
    with urllib.request.urlopen(comment_req) as resp:
        print(f"💬 Comentario publicado en PR #{PR_NUMBER}")
except urllib.error.HTTPError as e:
    print(f"❌ Error al publicar comentario: {e.code} - {e.read().decode()}")
