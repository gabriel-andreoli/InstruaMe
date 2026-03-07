# InstruaMe API — Documentação para o Front-end

## 1. Visão Geral

InstruaMe é uma marketplace de instrutores de direção. A API conecta **alunos** (Students) a **instrutores** (Instructors), permitindo busca, avaliações e chat em tempo real.

Tecnologia: **ASP.NET Core 10**, banco **PostgreSQL**, autenticação **JWT HS256**, chat via **WebSocket**.

---

## 2. Base URL

| Ambiente   | URL                        |
|------------|----------------------------|
| Local      | `http://localhost:5229`    |
| HTTPS local| `https://localhost:7102`   |

---

## 3. Autenticação

### JWT Bearer

Endpoints protegidos exigem o header:

```
Authorization: Bearer <token>
```

O token é obtido via `POST /v1/InstruaMe/login`.

Claims presentes no token:

| Claim                      | Descrição              |
|----------------------------|------------------------|
| `sub` (NameIdentifier)     | UserId (Guid)          |
| `email`                    | E-mail do usuário      |
| `role`                     | `"Instructor"` ou `"Student"` |

### Roles

| Role         | Valor no token |
|--------------|----------------|
| Instrutor    | `"Instructor"` |
| Aluno        | `"Student"`    |

Alguns endpoints aceitam qualquer usuário autenticado (`Bearer`), outros exigem role específica.

### WebSocket

O WebSocket **não suporta headers HTTP**, por isso o token é passado via query string:

```
ws://localhost:5229/ws/chat/{conversationId}?token=<jwt>
```

---

## 4. Tipos e Enums

### EUserRole

| Valor | Nome       |
|-------|------------|
| 0     | None       |
| 1     | Instructor |
| 2     | Student    |

### Datas

Todos os campos de data usam **ISO 8601 com timezone** (`DateTimeOffset`):
```
"2000-03-20T00:00:00Z"
```

### Paginação (PagedResult)

```json
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3
}
```

---

## 5. Endpoints

---

### Auth & Usuário

---

#### `POST /v1/InstruaMe/instructor`

Cadastra um novo instrutor.

**Auth:** Nenhuma

**Request body:**

```json
{
  "name": "string",          // obrigatório
  "email": "string",         // obrigatório, único
  "phoneNumber": "string",   // obrigatório
  "document": "string",      // obrigatório (CPF/CNPJ)
  "state": "string",         // obrigatório
  "city": "string",          // obrigatório
  "birthday": "ISO8601|null",// opcional
  "carModel": "string",      // obrigatório
  "biography": "string",     // obrigatório
  "description": "string",   // obrigatório
  "photo": "string|null",    // opcional (URL)
  "pricePerHour": 120.00,    // obrigatório
  "password": "string"       // obrigatório
}
```

**Responses:**

| Status | Descrição        | Body |
|--------|------------------|------|
| 201    | Criado           | vazio |
| 400    | Dados inválidos  | erros de validação |

**Exemplo fetch:**

```js
await fetch('http://localhost:5229/v1/InstruaMe/instructor', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    name: 'João Silva',
    email: 'joao@exemplo.com',
    phoneNumber: '11999990001',
    document: '12345678901',
    state: 'SP',
    city: 'São Paulo',
    carModel: 'Honda Civic',
    biography: 'Instrutor com 10 anos de experiência.',
    description: 'Aulas para iniciantes e avançados.',
    pricePerHour: 120.00,
    password: 'Senha@123'
  })
});
```

---

#### `POST /v1/InstruaMe/student`

Cadastra um novo aluno.

**Auth:** Nenhuma

**Request body:**

```json
{
  "name": "string",           // obrigatório
  "email": "string",          // obrigatório, único
  "birthday": "ISO8601|null", // opcional
  "photo": "string|null",     // opcional (URL)
  "password": "string",       // obrigatório
  "confirmPassword": "string" // obrigatório
}
```

**Responses:**

| Status | Descrição        | Body |
|--------|------------------|------|
| 201    | Criado           | vazio |
| 400    | Dados inválidos  | erros de validação |

**Exemplo fetch:**

```js
await fetch('http://localhost:5229/v1/InstruaMe/student', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    name: 'Maria Oliveira',
    email: 'maria@exemplo.com',
    password: 'Senha@123',
    confirmPassword: 'Senha@123'
  })
});
```

---

#### `POST /v1/InstruaMe/login`

Autentica instrutor ou aluno e retorna JWT.

**Auth:** Nenhuma

**Request body:**

```json
{
  "email": "string",    // obrigatório
  "password": "string"  // obrigatório
}
```

**Responses:**

| Status | Descrição          | Body |
|--------|--------------------|------|
| 200    | Autenticado        | `LoginResult` |
| 401    | Credenciais inválidas | vazio |

**LoginResult:**

```json
{
  "token": "eyJhbGci...",
  "role": 1
}
```

> `role`: 1 = Instructor, 2 = Student

**Exemplo fetch:**

```js
const res = await fetch('http://localhost:5229/v1/InstruaMe/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: 'joao@exemplo.com', password: 'Senha@123' })
});
const { token, role } = await res.json();
localStorage.setItem('token', token);
```

---

#### `GET /v1/InstruaMe/me`

Retorna os dados básicos do usuário autenticado.

**Auth:** Bearer (qualquer role)

**Responses:**

| Status | Descrição         | Body |
|--------|-------------------|------|
| 200    | OK                | objeto com userId, email, role |
| 401    | Não autenticado   | vazio |

**Response body:**

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "joao@exemplo.com",
  "role": "Instructor"
}
```

**Exemplo fetch:**

```js
const res = await fetch('http://localhost:5229/v1/InstruaMe/me', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const me = await res.json();
```

---

### Instrutores

---

#### `GET /v1/instructor`

Lista instrutores com filtros opcionais e paginação.

**Auth:** Nenhuma

**Query params:**

| Param           | Tipo    | Obrigatório | Descrição                        |
|-----------------|---------|-------------|----------------------------------|
| `name`          | string  | não         | Busca parcial (case-insensitive) |
| `city`          | string  | não         | Filtro por cidade (exato)        |
| `state`         | string  | não         | Filtro por estado (exato)        |
| `carModel`      | string  | não         | Filtro por modelo de carro (exato)|
| `minRating`     | double  | não         | Rating mínimo médio              |
| `maxPricePerHour`| decimal| não         | Preço máximo por hora            |
| `page`          | int     | não         | Página (default: 1)              |
| `pageSize`      | int     | não         | Itens por página (default: 20)   |

**Responses:**

| Status | Descrição | Body |
|--------|-----------|------|
| 200    | OK        | `PagedResult<InstructorCardResult>` |

**InstructorCardResult:**

```json
{
  "id": "guid",
  "name": "string",
  "photo": "string|null",
  "city": "string|null",
  "state": "string|null",
  "carModel": "string|null",
  "pricePerHour": 120.00,
  "averageRating": 4.5,
  "totalReviews": 12
}
```

**Exemplo fetch:**

```js
const params = new URLSearchParams({ name: 'João', minRating: '4', page: '1', pageSize: '10' });
const res = await fetch(`http://localhost:5229/v1/instructor?${params}`);
const { items, totalCount, totalPages } = await res.json();
```

---

#### `GET /v1/instructor/{id}`

Retorna o perfil completo de um instrutor, incluindo todas as avaliações.

**Auth:** Nenhuma

**Path params:**

| Param | Tipo | Descrição      |
|-------|------|----------------|
| `id`  | Guid | ID do instrutor |

**Responses:**

| Status | Descrição          | Body |
|--------|--------------------|------|
| 200    | OK                 | `InstructorProfileResult` |
| 404    | Não encontrado     | vazio |

**InstructorProfileResult:**

```json
{
  "id": "guid",
  "name": "string",
  "email": "string",
  "phoneNumber": "string|null",
  "state": "string|null",
  "city": "string|null",
  "birthday": "ISO8601|null",
  "carModel": "string|null",
  "biography": "string|null",
  "description": "string|null",
  "photo": "string|null",
  "pricePerHour": 120.00,
  "averageRating": 4.5,
  "totalReviews": 3,
  "reviews": [
    {
      "id": "guid",
      "studentId": "guid",
      "studentName": "string",
      "studentPhoto": "string|null",
      "rating": 5,
      "comment": "string",
      "createdAt": "ISO8601|null"
    }
  ]
}
```

**Exemplo fetch:**

```js
const res = await fetch(`http://localhost:5229/v1/instructor/${instructorId}`);
const profile = await res.json();
```

---

#### `PUT /v1/instructor/me`

Atualiza o perfil do instrutor autenticado. Todos os campos são opcionais — apenas os enviados são atualizados.

**Auth:** Bearer **Instructor**

**Request body:**

```json
{
  "name": "string|null",
  "phoneNumber": "string|null",
  "state": "string|null",
  "city": "string|null",
  "birthday": "ISO8601|null",
  "carModel": "string|null",
  "biography": "string|null",
  "description": "string|null",
  "photo": "string|null",
  "pricePerHour": 130.00
}
```

**Responses:**

| Status | Descrição          | Body |
|--------|--------------------|------|
| 204    | Atualizado         | vazio |
| 401    | Não autenticado    | vazio |
| 403    | Role incorreta     | vazio |
| 404    | Instrutor não encontrado | vazio |

**Exemplo fetch:**

```js
await fetch('http://localhost:5229/v1/instructor/me', {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ city: 'Campinas', pricePerHour: 130.00 })
});
```

---

#### `GET /v1/instructor/me/dashboard`

Retorna o dashboard do instrutor: estatísticas de avaliações e as 5 avaliações mais recentes.

**Auth:** Bearer **Instructor**

**Responses:**

| Status | Descrição       | Body |
|--------|-----------------|------|
| 200    | OK              | `InstructorDashboardResult` |
| 401    | Não autenticado | vazio |
| 403    | Role incorreta  | vazio |

**InstructorDashboardResult:**

```json
{
  "totalStudentReviewers": 8,
  "averageRating": 4.6,
  "recentReviews": [
    {
      "id": "guid",
      "studentId": "guid",
      "studentName": "string",
      "studentPhoto": "string|null",
      "rating": 5,
      "comment": "string",
      "createdAt": "ISO8601|null"
    }
  ]
}
```

> `recentReviews` retorna no máximo as 5 mais recentes.

**Exemplo fetch:**

```js
const res = await fetch('http://localhost:5229/v1/instructor/me/dashboard', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const dashboard = await res.json();
```

---

#### `POST /v1/instructor/{id}/reviews`

Envia uma avaliação de um aluno para um instrutor. Cada aluno pode avaliar um instrutor apenas uma vez.

**Auth:** Bearer **Student**

**Path params:**

| Param | Tipo | Descrição       |
|-------|------|-----------------|
| `id`  | Guid | ID do instrutor |

**Request body:**

```json
{
  "rating": 5,                   // obrigatório, inteiro entre 1 e 5
  "comment": "string"            // obrigatório
}
```

**Responses:**

| Status | Descrição                         | Body |
|--------|-----------------------------------|------|
| 201    | Avaliação criada                  | vazio |
| 400    | Rating fora do intervalo 1–5      | `{ "message": "Rating deve ser entre 1 e 5." }` |
| 401    | Não autenticado                   | vazio |
| 403    | Role incorreta (não é Student)    | vazio |
| 404    | Instrutor não encontrado          | vazio |
| 409    | Aluno já avaliou este instrutor   | `{ "message": "Você já avaliou este instrutor." }` |

**Regras de negócio:**
- `rating` deve ser inteiro entre **1 e 5** (inclusive). Valores fora desse intervalo retornam 400.
- Um aluno só pode avaliar o mesmo instrutor **uma vez**. Tentativas repetidas retornam 409.

**Exemplo fetch:**

```js
const res = await fetch(`http://localhost:5229/v1/instructor/${instructorId}/reviews`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${studentToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ rating: 5, comment: 'Excelente instrutor!' })
});
// 201 = criado, 409 = já avaliou
```

---

#### `GET /v1/instructor/{id}/reviews`

Lista todas as avaliações de um instrutor, ordenadas por data decrescente.

**Auth:** Nenhuma

**Path params:**

| Param | Tipo | Descrição       |
|-------|------|-----------------|
| `id`  | Guid | ID do instrutor |

**Responses:**

| Status | Descrição         | Body |
|--------|-------------------|------|
| 200    | OK                | `ReviewResult[]` |
| 404    | Não encontrado    | vazio |

**ReviewResult:**

```json
[
  {
    "id": "guid",
    "studentId": "guid",
    "studentName": "string",
    "studentPhoto": "string|null",
    "rating": 5,
    "comment": "string",
    "createdAt": "ISO8601|null"
  }
]
```

**Exemplo fetch:**

```js
const res = await fetch(`http://localhost:5229/v1/instructor/${instructorId}/reviews`);
const reviews = await res.json();
```

---

### Alunos

---

#### `GET /v1/student/me`

Retorna o perfil do aluno autenticado.

**Auth:** Bearer (qualquer role)

**Responses:**

| Status | Descrição         | Body |
|--------|-------------------|------|
| 200    | OK                | `StudentProfileResult` |
| 401    | Não autenticado   | vazio |
| 404    | Não encontrado    | vazio |

**StudentProfileResult:**

```json
{
  "id": "guid",
  "name": "string",
  "email": "string",
  "birthday": "ISO8601|null",
  "photo": "string|null"
}
```

**Exemplo fetch:**

```js
const res = await fetch('http://localhost:5229/v1/student/me', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const student = await res.json();
```

---

#### `GET /v1/student/{id}`

Retorna o perfil público de um aluno.

**Auth:** Bearer (qualquer role)

**Path params:**

| Param | Tipo | Descrição   |
|-------|------|-------------|
| `id`  | Guid | ID do aluno |

**Responses:**

| Status | Descrição         | Body |
|--------|-------------------|------|
| 200    | OK                | `StudentProfileResult` |
| 401    | Não autenticado   | vazio |
| 404    | Não encontrado    | vazio |

**Exemplo fetch:**

```js
const res = await fetch(`http://localhost:5229/v1/student/${studentId}`, {
  headers: { 'Authorization': `Bearer ${token}` }
});
const student = await res.json();
```

---

#### `PUT /v1/student/me`

Atualiza o perfil do aluno autenticado. Todos os campos são opcionais.

**Auth:** Bearer **Student**

**Request body:**

```json
{
  "name": "string|null",
  "birthday": "ISO8601|null",
  "photo": "string|null"
}
```

**Responses:**

| Status | Descrição         | Body |
|--------|-------------------|------|
| 204    | Atualizado        | vazio |
| 401    | Não autenticado   | vazio |
| 403    | Role incorreta    | vazio |
| 404    | Não encontrado    | vazio |

**Exemplo fetch:**

```js
await fetch('http://localhost:5229/v1/student/me', {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ name: 'Maria Atualizada' })
});
```

---

### Chat REST

---

#### `GET /v1/chat/conversations`

Lista todas as conversas do usuário autenticado (como instrutor ou aluno), ordenadas por data decrescente.

**Auth:** Bearer (qualquer role)

**Responses:**

| Status | Descrição         | Body |
|--------|-------------------|------|
| 200    | OK                | `ConversationResult[]` |
| 401    | Não autenticado   | vazio |

**ConversationResult:**

```json
[
  {
    "id": "guid",
    "instructorId": "guid",
    "instructorName": "string",
    "studentId": "guid",
    "studentName": "string",
    "createdAt": "ISO8601|null"
  }
]
```

**Exemplo fetch:**

```js
const res = await fetch('http://localhost:5229/v1/chat/conversations', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const conversations = await res.json();
```

---

#### `POST /v1/chat/conversations/{instructorId}`

Cria uma conversa entre o aluno autenticado e o instrutor indicado. Se já existir uma conversa entre eles, retorna a existente sem criar nova.

**Auth:** Bearer **Student**

**Path params:**

| Param        | Tipo | Descrição       |
|--------------|------|-----------------|
| `instructorId` | Guid | ID do instrutor |

**Request body:** Nenhum

**Responses:**

| Status | Descrição                        | Body |
|--------|----------------------------------|------|
| 201    | Conversa criada                  | `ConversationResult` |
| 200    | Conversa já existia              | `ConversationResult` |
| 401    | Não autenticado                  | vazio |
| 403    | Role incorreta (não é Student)   | vazio |
| 404    | Instrutor não encontrado         | `{ "message": "Instrutor não encontrado." }` |

**Regras de negócio:**
- Apenas **alunos** podem iniciar conversas.
- Se o par (aluno, instrutor) já tiver uma conversa, retorna 200 com os dados da existente (sem criar duplicata).

**Exemplo fetch:**

```js
const res = await fetch(`http://localhost:5229/v1/chat/conversations/${instructorId}`, {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${studentToken}` }
});
// 201 → nova conversa criada
// 200 → conversa já existia
const conversation = await res.json();
const conversationId = conversation.id;
```

---

#### `GET /v1/chat/conversations/{id}/messages`

Retorna o histórico de mensagens de uma conversa, ordenado por data crescente. Apenas os participantes (instrutor ou aluno da conversa) podem acessar.

**Auth:** Bearer (qualquer role, mas deve ser participante)

**Path params:**

| Param | Tipo | Descrição          |
|-------|------|--------------------|
| `id`  | Guid | ID da conversa     |

**Responses:**

| Status | Descrição                        | Body |
|--------|----------------------------------|------|
| 200    | OK                               | `ChatMessageResult[]` |
| 401    | Não autenticado                  | vazio |
| 403    | Usuário não é participante       | vazio |
| 404    | Conversa não encontrada          | vazio |

**ChatMessageResult:**

```json
[
  {
    "id": "guid",
    "conversationId": "guid",
    "senderId": "guid",
    "senderRole": "Student",
    "content": "string",
    "read": false,
    "createdAt": "ISO8601|null"
  }
]
```

**Regras de negócio:**
- Somente o instrutor ou o aluno daquela conversa específica podem ler as mensagens. Qualquer outro usuário (mesmo autenticado) recebe 403.

**Exemplo fetch:**

```js
const res = await fetch(`http://localhost:5229/v1/chat/conversations/${conversationId}/messages`, {
  headers: { 'Authorization': `Bearer ${token}` }
});
if (res.status === 403) {
  // usuário não é participante desta conversa
}
const messages = await res.json();
```

---

### Chat WebSocket

---

#### `WS /ws/chat/{conversationId}?token=<jwt>`

Conexão WebSocket para chat em tempo real numa conversa.

**Auth:** JWT passado via query string `?token=<jwt>`

**URL:**

```
ws://localhost:5229/ws/chat/{conversationId}?token=<jwt>
```

**Path params:**

| Param           | Tipo | Descrição      |
|-----------------|------|----------------|
| `conversationId`| Guid | ID da conversa |

**Query params:**

| Param   | Tipo   | Obrigatório | Descrição |
|---------|--------|-------------|-----------|
| `token` | string | sim         | JWT válido |

**Enviar mensagem (cliente → servidor):**

```json
{ "content": "Olá, tudo bem?" }
```

**Receber mensagem (servidor → cliente):**

```json
{
  "id": "guid",
  "conversationId": "guid",
  "senderId": "guid",
  "senderRole": "Student",
  "content": "Olá, tudo bem?",
  "read": false,
  "createdAt": "ISO8601|null"
}
```

**Comportamento:**
- Ao enviar uma mensagem, o servidor persiste no banco e faz broadcast para todos os participantes conectados naquela conversa (incluindo o remetente).
- Conexões inválidas ou de usuários não-participantes são encerradas imediatamente pelo servidor.
- O servidor mantém KeepAlive de 30 segundos.

**Erros de conexão:**

| Código HTTP | Descrição                    |
|-------------|------------------------------|
| 400         | Não é uma requisição WebSocket |
| 401         | Token ausente ou inválido    |

**Exemplo JavaScript:**

```js
const ws = new WebSocket(
  `ws://localhost:5229/ws/chat/${conversationId}?token=${token}`
);

ws.onopen = () => {
  console.log('Conectado');
};

ws.onmessage = (event) => {
  const message = JSON.parse(event.data);
  console.log(`${message.senderRole}: ${message.content}`);
};

ws.onclose = () => {
  console.log('Desconectado');
};

// Enviar mensagem
ws.send(JSON.stringify({ content: 'Olá!' }));
```

---

## 6. Regras de Negócio

| Regra | Detalhe |
|-------|---------|
| Cadastro único por e-mail | E-mails de instrutores e alunos são armazenados em tabelas separadas; não há validação de unicidade cruzada entre as duas tabelas |
| Soft delete | Entidades têm campo `Deleted` — registros deletados nunca aparecem nas listagens |
| Rating válido | Avaliações devem ter `rating` entre 1 e 5. Valores fora retornam 400 |
| Review única por par | Um aluno só pode avaliar um instrutor uma vez. Segunda tentativa retorna 409 |
| Conversa única por par | Há no máximo uma conversa por par (aluno, instrutor). `POST /conversations/{id}` retorna a existente se já houver |
| Mensagens privadas | Apenas instrutor e aluno da conversa podem ler histórico ou trocar mensagens. Terceiros recebem 403 |
| Dashboard restrito | `GET /instructor/me/dashboard` só é acessível com role `Instructor` |
| Aluno inicia chat | Apenas usuários com role `Student` podem criar conversas |

---

## 7. Tabela de Códigos de Erro

| Código | Significado                           | Casos comuns |
|--------|---------------------------------------|--------------|
| 200    | OK                                    | Sucesso em GETs, conversa já existente |
| 201    | Created                               | Cadastro, nova avaliação, nova conversa |
| 204    | No Content                            | Update bem-sucedido (PUT) |
| 400    | Bad Request                           | Rating inválido, campos obrigatórios ausentes |
| 401    | Unauthorized                          | Token ausente, expirado ou inválido |
| 403    | Forbidden                             | Role incorreta, acesso a conversa alheia |
| 404    | Not Found                             | Entidade não encontrada |
| 409    | Conflict                              | Review duplicada |

---

## 8. Exemplos de Fluxo

### Fluxo Instrutor (cadastro → atualizar perfil → ver dashboard)

```js
// 1. Cadastrar
await fetch('/v1/InstruaMe/instructor', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ name: '...', email: '...', password: '...', /* demais campos */ })
});

// 2. Login
const loginRes = await fetch('/v1/InstruaMe/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: '...', password: '...' })
});
const { token } = await loginRes.json();

// 3. Atualizar preço
await fetch('/v1/instructor/me', {
  method: 'PUT',
  headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
  body: JSON.stringify({ pricePerHour: 150.00 })
});

// 4. Ver dashboard
const dash = await fetch('/v1/instructor/me/dashboard', {
  headers: { Authorization: `Bearer ${token}` }
}).then(r => r.json());
console.log(dash.averageRating, dash.recentReviews);
```

---

### Fluxo Aluno (buscar instrutor → avaliar → iniciar chat)

```js
// 1. Buscar instrutores
const { items } = await fetch('/v1/instructor?minRating=4&maxPricePerHour=200').then(r => r.json());
const instructor = items[0];

// 2. Login do aluno
const { token: studentToken } = await fetch('/v1/InstruaMe/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: '...', password: '...' })
}).then(r => r.json());

// 3. Avaliar
const reviewRes = await fetch(`/v1/instructor/${instructor.id}/reviews`, {
  method: 'POST',
  headers: { Authorization: `Bearer ${studentToken}`, 'Content-Type': 'application/json' },
  body: JSON.stringify({ rating: 5, comment: 'Ótimo instrutor!' })
});
// 201 = criado, 409 = já avaliou

// 4. Criar/buscar conversa
const conv = await fetch(`/v1/chat/conversations/${instructor.id}`, {
  method: 'POST',
  headers: { Authorization: `Bearer ${studentToken}` }
}).then(r => r.json());

// 5. Carregar histórico
const messages = await fetch(`/v1/chat/conversations/${conv.id}/messages`, {
  headers: { Authorization: `Bearer ${studentToken}` }
}).then(r => r.json());
```

---

### Fluxo WebSocket (conectar → enviar → receber)

```js
// 1. Obter conversationId via REST (fluxo acima)

// 2. Conectar WebSocket
const ws = new WebSocket(`ws://localhost:5229/ws/chat/${conv.id}?token=${studentToken}`);

// 3. Escutar mensagens
ws.onmessage = ({ data }) => {
  const msg = JSON.parse(data);
  appendMessageToUI(msg); // { id, senderId, senderRole, content, createdAt, ... }
};

// 4. Enviar mensagem
function sendMessage(text) {
  ws.send(JSON.stringify({ content: text }));
}

// 5. Fechar ao sair
window.addEventListener('beforeunload', () => ws.close());
```

---

*Gerado em 2026-03-07 — InstruaMe API v1*
