#!/usr/bin/env bash
# =============================================================================
# InstruaMe — Test all endpoints
# Usage: bash test-endpoints.sh  (run from the project root)
# Dependencies: curl, python
# =============================================================================

BASE="http://localhost:5229"
PASS=0
FAIL=0

# ─── helpers ─────────────────────────────────────────────────────────────────

check() {
  local label="$1"
  local expected="$2"
  local actual="$3"
  if [ "$actual" = "$expected" ]; then
    echo "  [OK]  #${label} — HTTP ${actual}"
    PASS=$((PASS + 1))
  else
    echo "  [FAIL] #${label} — expected HTTP ${expected}, got HTTP ${actual}"
    FAIL=$((FAIL + 1))
  fi
}

# Parse a top-level field from a JSON file
json_get() {
  local field="$1"
  local file="$2"
  python -c "import json,sys; d=json.load(open(sys.argv[1])); print(d.get(sys.argv[2]) or '')" "$file" "$field" 2>/dev/null
}

# Parse items[0].id from a paged result JSON file
json_first_id() {
  local file="$1"
  python -c "
import json,sys
d=json.load(open(sys.argv[1]))
items=d.get('items',[])
print(items[0]['id'] if items else '')
" "$file" 2>/dev/null
}

# ─── start API ───────────────────────────────────────────────────────────────

echo ""
echo "Starting API in background..."
dotnet run --project src/InstruaMe.csproj > /tmp/instruame-api.log 2>&1 &
API_PID=$!
echo "API PID: ${API_PID} — waiting 8s for startup..."
sleep 8

echo ""
echo "============================================================"
echo " Running 23 endpoint tests"
echo "============================================================"

# ─── 1. Register instructor ───────────────────────────────────────────────────
echo ""
echo "--- Auth & User ---"

TIMESTAMP=$(date +%s)
INSTRUCTOR_EMAIL="instructor${TIMESTAMP}@test.com"
STUDENT_EMAIL="student${TIMESTAMP}@test.com"

STATUS=$(curl -s -o /tmp/res1.json -w "%{http_code}" -X POST "${BASE}/v1/InstruaMe/instructor" \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"Test Instructor\",
    \"email\": \"${INSTRUCTOR_EMAIL}\",
    \"phoneNumber\": \"11999990001\",
    \"document\": \"12345678901\",
    \"state\": \"SP\",
    \"city\": \"Sao Paulo\",
    \"birthday\": \"1985-06-15T00:00:00Z\",
    \"carModel\": \"Honda Civic\",
    \"biography\": \"Instrutor experiente com 10 anos de mercado.\",
    \"description\": \"Aulas para iniciantes e avancados.\",
    \"pricePerHour\": 120.00,
    \"password\": \"Senha@123\"
  }")
check "1 POST /v1/InstruaMe/instructor" "201" "$STATUS"

# ─── 2. Register student ─────────────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res2.json -w "%{http_code}" -X POST "${BASE}/v1/InstruaMe/student" \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"Test Student\",
    \"email\": \"${STUDENT_EMAIL}\",
    \"birthday\": \"2000-03-20T00:00:00Z\",
    \"photo\": null,
    \"password\": \"Senha@123\",
    \"confirmPassword\": \"Senha@123\"
  }")
check "2 POST /v1/InstruaMe/student" "201" "$STATUS"

# ─── 3. Login as instructor ───────────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res3.json -w "%{http_code}" -X POST "${BASE}/v1/InstruaMe/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\": \"${INSTRUCTOR_EMAIL}\", \"password\": \"Senha@123\"}")
check "3 POST /v1/InstruaMe/login (instrutor)" "200" "$STATUS"
INSTRUCTOR_TOKEN=$(json_get "token" "/tmp/res3.json")

if [ -z "$INSTRUCTOR_TOKEN" ]; then
  echo "  [WARN] Could not extract INSTRUCTOR_TOKEN — check /tmp/res3.json"
  cat /tmp/res3.json
fi

# ─── 4. Login as student ─────────────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res4.json -w "%{http_code}" -X POST "${BASE}/v1/InstruaMe/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\": \"${STUDENT_EMAIL}\", \"password\": \"Senha@123\"}")
check "4 POST /v1/InstruaMe/login (aluno)" "200" "$STATUS"
STUDENT_TOKEN=$(json_get "token" "/tmp/res4.json")

if [ -z "$STUDENT_TOKEN" ]; then
  echo "  [WARN] Could not extract STUDENT_TOKEN — check /tmp/res4.json"
fi

# ─── 5. GET /me (instructor) ─────────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res5.json -w "%{http_code}" \
  -H "Authorization: Bearer ${INSTRUCTOR_TOKEN}" \
  "${BASE}/v1/InstruaMe/me")
check "5 GET /v1/InstruaMe/me" "200" "$STATUS"

# ─── 6. List instructors (no auth) ───────────────────────────────────────────
echo ""
echo "--- Instructors ---"

STATUS=$(curl -s -o /tmp/res6.json -w "%{http_code}" "${BASE}/v1/instructor")
check "6 GET /v1/instructor" "200" "$STATUS"
INSTRUCTOR_ID=$(json_first_id "/tmp/res6.json")
echo "  [INFO] Instructor ID for tests: ${INSTRUCTOR_ID}"

# ─── 7. List instructors with filters ────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res7.json -w "%{http_code}" \
  "${BASE}/v1/instructor?name=Test&minRating=0")
check "7 GET /v1/instructor?name=Test&minRating=0" "200" "$STATUS"

# ─── 8. GET instructor by id ─────────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res8.json -w "%{http_code}" \
  "${BASE}/v1/instructor/${INSTRUCTOR_ID}")
check "8 GET /v1/instructor/{id}" "200" "$STATUS"

# ─── 9. Update instructor (PUT /instructor/me) ───────────────────────────────

STATUS=$(curl -s -o /tmp/res9.json -w "%{http_code}" -X PUT \
  -H "Authorization: Bearer ${INSTRUCTOR_TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{\"city\": \"Campinas\", \"pricePerHour\": 130.00}" \
  "${BASE}/v1/instructor/me")
check "9 PUT /v1/instructor/me" "204" "$STATUS"

# ─── 10. Dashboard before review ─────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res10.json -w "%{http_code}" \
  -H "Authorization: Bearer ${INSTRUCTOR_TOKEN}" \
  "${BASE}/v1/instructor/me/dashboard")
check "10 GET /v1/instructor/me/dashboard (0 reviews)" "200" "$STATUS"

# ─── 11. Submit review (student → instructor) ────────────────────────────────

STATUS=$(curl -s -o /tmp/res11.json -w "%{http_code}" -X POST \
  -H "Authorization: Bearer ${STUDENT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{\"rating\": 5, \"comment\": \"Excelente instrutor, muito paciente!\"}" \
  "${BASE}/v1/instructor/${INSTRUCTOR_ID}/reviews")
check "11 POST /v1/instructor/{id}/reviews (rating=5)" "201" "$STATUS"

# ─── 12. Duplicate review → 409 ──────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res12.json -w "%{http_code}" -X POST \
  -H "Authorization: Bearer ${STUDENT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{\"rating\": 4, \"comment\": \"Segunda tentativa.\"}" \
  "${BASE}/v1/instructor/${INSTRUCTOR_ID}/reviews")
check "12 POST /v1/instructor/{id}/reviews (duplicado -> 409)" "409" "$STATUS"

# ─── 13. Invalid rating → 400 ────────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res13.json -w "%{http_code}" -X POST \
  -H "Authorization: Bearer ${STUDENT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{\"rating\": 0, \"comment\": \"Rating invalido.\"}" \
  "${BASE}/v1/instructor/${INSTRUCTOR_ID}/reviews")
check "13 POST /v1/instructor/{id}/reviews (rating=0 -> 400)" "400" "$STATUS"

# ─── 14. Get reviews (no auth) ───────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res14.json -w "%{http_code}" \
  "${BASE}/v1/instructor/${INSTRUCTOR_ID}/reviews")
check "14 GET /v1/instructor/{id}/reviews" "200" "$STATUS"

# ─── 15. Dashboard after review ──────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res15.json -w "%{http_code}" \
  -H "Authorization: Bearer ${INSTRUCTOR_TOKEN}" \
  "${BASE}/v1/instructor/me/dashboard")
check "15 GET /v1/instructor/me/dashboard (1 review)" "200" "$STATUS"
DASHBOARD_RATING=$(json_get "averageRating" "/tmp/res15.json")
echo "  [INFO] AverageRating after review: ${DASHBOARD_RATING}"

# ─── 16. Student GET /me ─────────────────────────────────────────────────────
echo ""
echo "--- Students ---"

STATUS=$(curl -s -o /tmp/res16.json -w "%{http_code}" \
  -H "Authorization: Bearer ${STUDENT_TOKEN}" \
  "${BASE}/v1/student/me")
check "16 GET /v1/student/me" "200" "$STATUS"
STUDENT_ID=$(json_get "id" "/tmp/res16.json")
echo "  [INFO] Student ID: ${STUDENT_ID}"

# ─── 17. Student GET /{id} ───────────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res17.json -w "%{http_code}" \
  -H "Authorization: Bearer ${STUDENT_TOKEN}" \
  "${BASE}/v1/student/${STUDENT_ID}")
check "17 GET /v1/student/{id}" "200" "$STATUS"

# ─── 18. Update student ──────────────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res18.json -w "%{http_code}" -X PUT \
  -H "Authorization: Bearer ${STUDENT_TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{\"name\": \"Test Student Atualizado\"}" \
  "${BASE}/v1/student/me")
check "18 PUT /v1/student/me" "204" "$STATUS"

# ─── 19. Create conversation ─────────────────────────────────────────────────
echo ""
echo "--- Chat ---"

STATUS=$(curl -s -o /tmp/res19.json -w "%{http_code}" -X POST \
  -H "Authorization: Bearer ${STUDENT_TOKEN}" \
  "${BASE}/v1/chat/conversations/${INSTRUCTOR_ID}")
check "19 POST /v1/chat/conversations/{instructorId} (criar)" "201" "$STATUS"
CONVERSATION_ID=$(json_get "id" "/tmp/res19.json")
echo "  [INFO] Conversation ID: ${CONVERSATION_ID}"

# ─── 20. Get or create conversation (returns existing → 200) ─────────────────

STATUS=$(curl -s -o /tmp/res20.json -w "%{http_code}" -X POST \
  -H "Authorization: Bearer ${STUDENT_TOKEN}" \
  "${BASE}/v1/chat/conversations/${INSTRUCTOR_ID}")
check "20 POST /v1/chat/conversations/{instructorId} (existente -> 200)" "200" "$STATUS"

# ─── 21. List conversations ──────────────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res21.json -w "%{http_code}" \
  -H "Authorization: Bearer ${STUDENT_TOKEN}" \
  "${BASE}/v1/chat/conversations")
check "21 GET /v1/chat/conversations" "200" "$STATUS"

# ─── 22. Get messages (participant) ──────────────────────────────────────────

STATUS=$(curl -s -o /tmp/res22.json -w "%{http_code}" \
  -H "Authorization: Bearer ${STUDENT_TOKEN}" \
  "${BASE}/v1/chat/conversations/${CONVERSATION_ID}/messages")
check "22 GET /v1/chat/conversations/{id}/messages (participante)" "200" "$STATUS"

# ─── 23. Get messages (unauthorized third party → 403) ───────────────────────
THIRD_EMAIL="third${TIMESTAMP}@test.com"
curl -s -o /tmp/res_third_reg.json -X POST "${BASE}/v1/InstruaMe/student" \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"Third Party\",
    \"email\": \"${THIRD_EMAIL}\",
    \"password\": \"Senha@123\",
    \"confirmPassword\": \"Senha@123\"
  }" > /dev/null
curl -s -o /tmp/res_third_login.json -X POST "${BASE}/v1/InstruaMe/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\": \"${THIRD_EMAIL}\", \"password\": \"Senha@123\"}"
THIRD_TOKEN=$(json_get "token" "/tmp/res_third_login.json")

STATUS=$(curl -s -o /tmp/res23.json -w "%{http_code}" \
  -H "Authorization: Bearer ${THIRD_TOKEN}" \
  "${BASE}/v1/chat/conversations/${CONVERSATION_ID}/messages")
check "23 GET /v1/chat/conversations/{id}/messages (terceiro -> 403)" "403" "$STATUS"

# ─── Summary ─────────────────────────────────────────────────────────────────

echo ""
echo "============================================================"
echo " Results: ${PASS} passed, ${FAIL} failed"
echo "============================================================"
echo ""

# ─── Teardown ────────────────────────────────────────────────────────────────

echo "Stopping API (PID ${API_PID})..."
kill $API_PID 2>/dev/null
echo "Done."
echo ""

if [ "$FAIL" -gt 0 ]; then
  exit 1
fi
