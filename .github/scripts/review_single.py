import os
import json
import urllib.request
import urllib.error

GEMINI_API_KEY = os.environ["GEMINI_API_KEY"]
PR_TITLE       = os.environ.get("PR_TITLE", "PR")
PR_NUMBER      = os.environ["PR_NUMBER"]
GITHUB_TOKEN   = os.environ["GITHUB_TOKEN"]
REPO           = os.environ["REPO"]

with open("pr.diff", "r", encoding="utf-8") as f:
    diff = f.read()[:12000]

print(f"📋 Analizando PR #{PR_NUMBER}: {PR_TITLE}")

# ── Listar modelos disponibles ───────────────────────────────────────────────
print("🔍 Listando modelos disponibles...")
list_url = f"https://generativelanguage.googleapis.com/v1beta/models?key={GEMINI_API_KEY}"
list_req = urllib.request.Request(list_url)
model = None

try:
    with urllib.request.urlopen(list_req) as resp:
        data = json.loads(resp.read())
    all_models = data.get("models", [])
    flash_models = [
        m["name"] for m in all_models
        if "flash" in m["name"].lower()
        and "generateContent" in m.get("supportedGenerationMethods", [])
    ]
    print(f"✅ Modelos flash disponibles: {flash_models}")
    if flash_models:
        model = flash_models[0].replace("models/", "")
    else:
        # usar cualquier modelo con generateContent
        any_model = [
            m["name"] for m in all_models
            if "generateContent" in m.get("supportedGenerationMethods", [])
        ]
        print(f"📋 Todos los modelos: {any_model}")
        if any_model:
            model = any_model[0].replace("models/", "")
except urllib.error.HTTPError as e:
    print(f"❌ Error listando modelos: {e.code} - {e.read().decode()}")
    model = "gemini-pro"

print(f"🚀 Usando modelo: {model}")

# ── Llamar a Gemini ──────────────────────────────────────────────────────────
prompt = f"""Sos un Senior Developer especializado en code review exhaustivo.
Analizá el siguiente Pull Request llamado "{PR_TITLE}" y generá un informe en Markdown en español.

El informe debe incluir:
1. 📋 **RESUMEN** — qué cambia y nivel de impacto
2. 🐛 **BUGS IDENTIFICADOS** — severidad 🔴/🟠/🟡/🔵
3. ⚡ **PERFORMANCE** — anti-patterns, N+1, enumeraciones múltiples
4. 🔒 **SEGURIDAD** — vulnerabilidades, credenciales hardcodeadas
5. 🏗️ **CALIDAD** — SOLID, TODO/FIXME sin ticket
6. ✅ **VEREDICTO** — APROBADO / APROBADO CON COMENTARIOS / REQUIERE CAMBIOS

GIT DIFF:
{diff}
"""

payload = {
    "contents": [{"parts": [{"text": prompt}]}],
    "generationConfig": {"maxOutputTokens": 2048, "temperature": 0.3}
}

url = f"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={GEMINI_API_KEY}"
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
<sub>🤖 Generado automáticamente | Modelo: {model}</sub>
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
