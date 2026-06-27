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
    diff = f.read()[:10000]

print(f"🔗 Iniciando pipeline multiagente para PR #{PR_NUMBER}: {PR_TITLE}")

# ── Detectar modelo disponible ───────────────────────────────────────────────
print("🔍 Detectando modelo...")
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
    if flash_models:
        model = flash_models[0].replace("models/", "")
except Exception as e:
    print(f"⚠️ No se pudo listar modelos: {e}")

print(f"✅ Usando modelo: {model}")

def call_gemini(prompt: str, agent_name: str) -> str:
    payload = {
        "contents": [{"parts": [{"text": prompt}]}],
        "generationConfig": {"maxOutputTokens": 800, "temperature": 0.3}
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
        result = data["candidates"][0]["content"]["parts"][0]["text"]
        print(f"  ✅ {agent_name} completado")
        return result
    except urllib.error.HTTPError as e:
        error = e.read().decode()
        print(f"  ❌ {agent_name} error: {e.code} - {error}")
        return f"❌ Error en {agent_name}: {e.code}"

# ── Agente 1: Contexto ───────────────────────────────────────────────────────
print("\n🗺️  Agente 1: Contexto...")
context = call_gemini(f"""Sos un agente especializado en analizar el CONTEXTO de Pull Requests.
Analizá qué archivos cambian, qué componentes se ven afectados, cuál es el propósito del cambio
y su nivel de impacto (crítico/alto/medio/bajo). Máximo 200 palabras en español.

DIFF:
{diff}""", "Agente Contexto")

# ── Agente 2: Seguridad ──────────────────────────────────────────────────────
print("🔒  Agente 2: Seguridad...")
security = call_gemini(f"""Sos un experto en seguridad haciendo code review.
Buscá: credenciales hardcodeadas, SQL injection, XSS, datos sensibles expuestos,
autenticación incorrecta, validación de inputs faltante.
Usá 🔴 CRÍTICO / 🟠 ALTO / 🟡 MEDIO / 🔵 BAJO. Máximo 200 palabras en español.
Si no hay problemas, indicalo claramente.

DIFF:
{diff}""", "Agente Seguridad")

# ── Agente 3: Performance ────────────────────────────────────────────────────
print("⚡  Agente 3: Performance...")
performance = call_gemini(f"""Sos un experto en performance haciendo code review.
Buscá: N+1 queries, enumeraciones múltiples, operaciones I/O en loops, memory leaks,
recursos no liberados, filtros en memoria que deberían ir en BD.
Usá 🔴 CRÍTICO / 🟠 ALTO / 🟡 MEDIO / 🔵 BAJO. Máximo 200 palabras en español.
Si no hay problemas, indicalo claramente.

DIFF:
{diff}""", "Agente Performance")

# ── Agente 4: Calidad ────────────────────────────────────────────────────────
print("🏗️  Agente 4: Calidad...")
quality = call_gemini(f"""Sos un experto en clean code y arquitectura haciendo code review.
Buscá: violaciones de SOLID, código duplicado, complejidad excesiva, naming incorrecto,
TODO/FIXME sin ticket, comentarios de debug, métodos muy largos.
Usá 🔴 CRÍTICO / 🟠 ALTO / 🟡 MEDIO / 🔵 BAJO. Máximo 200 palabras en español.
Si no hay problemas, indicalo claramente.

DIFF:
{diff}""", "Agente Calidad")

# ── Agente 5: Sintetizador ───────────────────────────────────────────────────
print("🧩  Agente 5: Sintetizador...")
verdict = call_gemini(f"""Sos el agente coordinador de un sistema multiagente de code review.
Recibís los análisis de 4 agentes especializados y generás un veredicto final consolidado.

CONTEXTO: {context}
SEGURIDAD: {security}
PERFORMANCE: {performance}
CALIDAD: {quality}

Generá un veredicto final en español con:
- Los 3 hallazgos más importantes (con severidad)
- Una decisión clara: ✅ APROBADO / ⚠️ APROBADO CON COMENTARIOS / ❌ REQUIERE CAMBIOS
- Máximo 150 palabras""", "Agente Sintetizador")

# ── Publicar comentario en el PR ─────────────────────────────────────────────
comment_body = f"""## 🔗 Análisis de PR — Pipeline Multiagente (Gemini)

> Análisis automático generado por **5 agentes especializados** para el PR **#{PR_NUMBER}: {PR_TITLE}**

---

### 🗺️ Agente 1 — Contexto
{context}

---

### 🔒 Agente 2 — Seguridad
{security}

---

### ⚡ Agente 3 — Performance
{performance}

---

### 🏗️ Agente 4 — Calidad de Código
{quality}

---

### 🧩 Agente 5 — Veredicto Final (Sintetizador)
{verdict}

---
<sub>🤖 Generado automáticamente | Modo: Multiagente (5 agentes) | Modelo: {model}</sub>
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
        print(f"\n💬 Comentario multiagente publicado en PR #{PR_NUMBER}")
except urllib.error.HTTPError as e:
    print(f"❌ Error al publicar comentario: {e.code} - {e.read().decode()}")
