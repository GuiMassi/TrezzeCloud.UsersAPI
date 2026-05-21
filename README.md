# README - UsersAPI

```md
# TrezzeCloud.UsersAPI

Microsserviço responsável pelo gerenciamento de usuários da plataforma.

## Responsabilidades

- Cadastro de usuários
- Login
- JWT Authentication
- Roles e autorização
- Seed de usuário administrador
- Publicação do evento UserCreatedEvent

---

# Tecnologias

- .NET 10
- ASP.NET Core
- Entity Framework Core
- SQL Server
- JWT
- RabbitMQ
- MassTransit
- Docker
- Kubernetes

---

# Variáveis de Ambiente

| Variável | Descrição |
|---|---|
| ConnectionStrings__UsersDatabase | Connection string SQL Server |
| RabbitMq__Host | Host RabbitMQ |
| RabbitMq__Username | Usuário RabbitMQ |
| RabbitMq__Password | Senha RabbitMQ |
| Jwt__Issuer | Issuer JWT |
| Jwt__Audience | Audience JWT |
| Jwt__SecretKey | Secret JWT |
| AdminUser__Name | Nome admin |
| AdminUser__Email | Email admin |
| AdminUser__Password | Senha admin |

---

# Executar Localmente

```bash
dotnet restore
dotnet ef database update
dotnet run
````

---

# Docker

```bash
docker build -t trezzecloud-users-api .
```

---

# Kubernetes

Manifestos disponíveis em:

```txt
k8s/users-api
```

````

---