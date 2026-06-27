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

# ── Detectar modelo disponible ───────────────────────────────────────────────
print("🔍 Listando modelos disponibles...")
list_url = f"https://generativelanguage.googleapis.com/v1beta/models?key={GEMINI_API_KEY}"
model = "gemini-2.5-flash"

try:
    with urllib.request.urlopen(urllib.request.Request(list_url)) as resp:
        data = json.loads(resp.read())
    flash_models = [
        m["name"] for m in data.get("models", [])
        if "flash" in m["name"].lower()
        and "generateContent" in m.get("supportedGenerationMethods", [])
    ]
    print(f"✅ Modelos flash disponibles: {flash_models[:5]}")
    if flash_models:
        model = flash_models[0].replace("models/", "")
except Exception as e:
    print(f"⚠️ No se pudo listar modelos: {e}")

print(f"🚀 Usando modelo: {model}")

# ── Llamar a Gemini ──────────────────────────────────────────────────────────
prompt = f"""Sos un Senior Developer haciendo code review. Analizá este diff y respondé en Markdown en español, MUY CONCISO (máximo 400 palabras total):

**RESUMEN:** 1-2 oraciones sobre qué cambia.
**BUGS 🔴/🟠/🟡/🔵:** Lista los problemas encontrados con severidad.
**SEGURIDAD:** Vulnerabilidades si las hay.
**PERFORMANCE:** Anti-patterns si los hay.
**VEREDICTO:** ✅ APROBADO / ⚠️ APROBADO CON COMENTARIOS / ❌ REQUIERE CAMBIOS

DIFF:
{diff}
"""

payload = {
    "contents": [{"parts": [{"text": prompt}]}],
    "generationConfig": {"maxOutputTokens": 500, "temperature": 0.3}
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
