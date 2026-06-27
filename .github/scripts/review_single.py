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

model = "gemini-2.5-flash"
print(f"🚀 Usando modelo: {model}")

# ── Llamar a Gemini ──────────────────────────────────────────────────────────
prompt = f"""Sos un Senior Developer haciendo code review. Analizá este diff y respondé en Markdown en español de forma CONCISA y DIRECTA.

Estructura del informe:
**📋 RESUMEN:** 1-2 oraciones sobre qué cambia.

**🐛 BUGS IDENTIFICADOS:** Lista de problemas con severidad 🔴 CRÍTICO / 🟠 ALTO / 🟡 MEDIO / 🔵 BAJO. Para cada uno: archivo, línea aproximada y descripción corta.

**🔒 SEGURIDAD:** Vulnerabilidades encontradas.

**⚡ PERFORMANCE:** Anti-patterns encontrados.

**🏗️ CALIDAD:** Issues de clean code (TODO/FIXME, naming, etc).

**✅ VEREDICTO:** Una de estas opciones: ✅ APROBADO / ⚠️ APROBADO CON COMENTARIOS / ❌ REQUIERE CAMBIOS. Justificá en 1 oración.

GIT DIFF:
{diff}
"""

payload = {
    "contents": [{"parts": [{"text": prompt}]}],
    "generationConfig": {
        "maxOutputTokens": 3000,
        "temperature": 0.3,
        "thinkingConfig": {"thinkingBudget": 0}
    }
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
    print(f"✅ Análisis generado: {len(analysis)} caracteres")
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
