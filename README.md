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

## Tecnologias

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

## Variáveis de Ambiente

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
| TestUser__Id | Id fixo do usuário teste |
| TestUser__Name | Nome usuário teste |
| TestUser__Email | Email usuário teste |
| TestUser__Password | Senha usuário teste |

---

## Usuários Seed (Desenvolvimento)

O serviço cria automaticamente os usuários de seed no startup.

### Admin

- Name: TrezzeCloud Admin
- Email: admin@trezzecloud.com
- Password (dev/local): Admin@123

### Teste (compartilhado com CatalogAPI)

- Id: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa
- Name: Usuario Teste
- Email: teste@trezzecloud.com
- Password (dev/local): Teste@123

### Segurança

- Nunca use essas credenciais padrão em produção.
- Em ambientes não locais, sobrescreva os valores via variáveis de ambiente e secret manager.

---

## Executar Localmente

```bash
dotnet restore
dotnet ef database update
dotnet run
```

---

## Docker

```bash
docker build -t trezzecloud-users-api .
```

---

## Kubernetes

Manifestos disponíveis em:

```txt
k8s/users-api
```

---