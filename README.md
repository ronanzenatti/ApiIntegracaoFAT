# API Integradora FAT

API Integradora que atua como middleware entre a API da CETTPRO e o Portal da FAT, fornecendo sincronização automática de dados, geração de cronogramas e processamento de frequência.

## 📋 Visão Geral

A **API Integradora FAT** é uma solução robusta desenvolvida em ASP.NET Core 8.0 que:

- **Sincroniza** dados da CETTPRO (Cursos, Turmas, Alunos) automaticamente
- **Enriquece** dados com informações específicas do Portal FAT
- **Gera** cronogramas de aulas automatizados
- **Processa** arquivos de frequência (CSV/XLSX)
- **Oferece** endpoints RESTful otimizados para alta performance

## 🎯 Principais Funcionalidades

### 🔄 Sincronização Automática
- Sincronização bidirecional com a API CETTPRO
- Execução automática em background (2x ao dia)
- Sistema de retry e logs detalhados
- Soft delete para auditoria completa

### 📅 Geração de Cronogramas
- Cálculo inteligente de datas de aulas
- Configuração flexível de horários e feriados
- Persistência otimizada no banco local

### 📊 Processamento de Frequência
- Upload de arquivos multipart (CSV/XLSX)
- Validação e reconciliação de dados
- Integração transacional com CETTPRO

## 🏗️ Arquitetura

### Stack Tecnológico
```
├── ASP.NET Core 8.0          # Framework principal
├── Entity Framework Core     # ORM
├── MySQL 8.0+               # Banco de dados
├── Pomelo.EntityFrameworkCore.MySql # Driver MySQL
├── Background Services      # Sincronização automática
├── Azure App Service       # Hospedagem
└── Azure Key Vault         # Gerenciamento de secrets
```

### Estrutura do Projeto
```
ApiIntegracao/
├── Controllers/             # Endpoints da API
├── Models/                 # Entidades do domínio
├── Data/                   # DbContext e configurações
├── Services/               # Lógica de negócio
├── BackgroundServices/     # Serviços em background
├── DTOs/                   # Data Transfer Objects
├── Helpers/                # Utilitários
└── Middleware/             # Middlewares customizados
```

## 🚀 Início Rápido

### Pré-requisitos
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL 8.0+](https://dev.mysql.com/downloads/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) ou [VS Code](https://code.visualstudio.com/)

### Instalação

1. **Clone o repositório**
   ```bash
   git clone https://github.com/ronanzenatti/ApiIntegracaoFAT.git
   cd ApiIntegracaoFAT
   ```

2. **Configure o banco de dados**
   ```sql
   CREATE DATABASE api_integracao CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   ```

3. **Configure as variáveis de ambiente**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "server=localhost;database=api_integracao;user=root;password=sua_senha"
     },
     "CettproApi": {
       "BaseUrl": "https://cettpro-appweb-crm-api-externo-hml.azurewebsites.net",
       "Email": "seu_email@teste.com",
       "Password": "sua_senha"
     }
   }
   ```

4. **Execute as migrations**
   ```bash
   dotnet ef database update
   ```

5. **Execute a aplicação**
   ```bash
   dotnet run
   ```

6. **Acesse a documentação**
   - Swagger UI: `https://localhost:5001/swagger`

## 📝 Endpoints Principais

### 🎓 Cursos
```http
GET    /api/v1/cursos              # Lista todos os cursos
GET    /api/v1/cursos/{id}         # Busca curso por ID
GET    /api/v1/cursos?search=nome  # Busca por nome
```

### 👥 Turmas
```http
GET    /api/v1/turmas                    # Lista todas as turmas
GET    /api/v1/turmas/{id}               # Busca turma por ID
GET    /api/v1/turmas/{id}/alunos        # Lista alunos da turma
GET    /api/v1/turmas?id_curso=123       # Filtra por curso
```

### 🎓 Alunos
```http
GET    /api/v1/alunos?cpf=12345678901    # Busca por CPF
GET    /api/v1/alunos?email=teste@email  # Busca por e-mail
```

### 📅 Cronogramas
```http
POST   /api/v1/cronogramas               # Gera cronograma de aulas
```

### 📊 Frequência
```http
POST   /api/v1/frequencia                # Processa arquivo de frequência
```

## 🔧 Configuração Avançada

### Variáveis de Ambiente de Produção
```bash
# Banco de dados
ConnectionStrings__DefaultConnection="server=prod-server;database=api_integracao_prod;..."

# API CETTPRO
CettproApi__BaseUrl="https://cettpro-appweb-crm-api-externo-hml.azurewebsites.net"
CettproApi__Email="externo@testeapi.com.br"
CettproApi__Password="teste123"

# Sincronização
SyncSettings__AutoSyncEnabled=true
SyncSettings__SyncIntervalHours=12
SyncSettings__SyncStartTime="06:00"
```

### Configuração de Logs
A aplicação utiliza Serilog para logging estruturado:
- **Console**: Logs de desenvolvimento
- **File**: Logs persistentes em arquivos
- **Application Insights**: Monitoramento em produção

## 🧪 Desenvolvimento

### Padrões de Código
- **Clean Architecture**: Separação clara de responsabilidades
- **Repository Pattern**: Abstração da camada de dados
- **SOLID Principles**: Código maintível e testável
- **Async/Await**: Operações assíncronas para performance

### Principais Dependências
```xml
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.3" />
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
<PackageReference Include="ExcelDataReader" Version="3.7.0" />
<PackageReference Include="CsvHelper" Version="33.1.0" />
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.8.0" />
```

### Executando Testes
```bash
# Executar todos os testes
dotnet test

# Executar com cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## 🚀 Deploy

### Azure App Service
O projeto inclui pipeline automatizado via GitHub Actions:

1. **Build** automático no push para `main`
2. **Deploy** automatizado para Azure App Service
3. **Configuração** via Azure Key Vault
4. **Monitoramento** via Application Insights

### Pipeline de CI/CD
- `.github/workflows/main_apifat.yml` - Pipeline principal
- `azure-pipelines.yml` - Pipeline alternativo do Azure DevOps

## 📊 Monitoramento

### Health Checks
- `/health` - Status geral da aplicação
- `/health/database` - Status do banco de dados
- `/health/cettpro` - Conectividade com CETTPRO API

### Métricas
- **Performance**: Tempo de resposta dos endpoints
- **Sincronização**: Status e logs das sincronizações
- **Erros**: Tracking de exceções e falhas

## 🔒 Segurança

### Implementações de Segurança
- **HTTPS**: Obrigatório em todos os endpoints
- **JWT Authentication**: Validação de tokens
- **Input Validation**: Validação rigorosa de entradas
- **SQL Injection Protection**: Via Entity Framework
- **Azure Key Vault**: Gerenciamento seguro de secrets

## 📚 Documentação

### Recursos Adicionais
- [Documentação da API CETTPRO](docs/cettpro-api-spec.pdf)
- [Documento de Arquitetura](docs/arquitetura-especificacao.docx)
- [Guia de Desenvolvimento](docs/documentacao-desenvolvimento.md)

### Swagger/OpenAPI
Documentação interativa disponível em `/swagger` com:
- Descrição detalhada de todos os endpoints
- Modelos de request/response
- Exemplos de uso
- Testes interativos

## 👥 Contribuição

### Desenvolvimento Local
1. Fork o repositório
2. Crie uma branch para sua feature (`git checkout -b feature/nova-funcionalidade`)
3. Commit suas mudanças (`git commit -am 'Adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feature/nova-funcionalidade`)
5. Abra um Pull Request

### Padrões de Commit
- `feat:` Nova funcionalidade
- `fix:` Correção de bug
- `docs:` Alterações na documentação
- `style:` Formatação de código
- `refactor:` Refatoração de código
- `test:` Adição de testes

## 📄 Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## 🆘 Suporte

### Contato
- **Desenvolvedor**: Ronan Zenatti
- **Email**: ronan@exemplo.com
- **Repositório**: [GitHub](https://github.com/ronanzenatti/ApiIntegracaoFAT)

### Problemas Conhecidos
Consulte as [Issues](https://github.com/ronanzenatti/ApiIntegracaoFAT/issues) do GitHub para problemas conhecidos e soluções.

---

**Desenvolvido com ❤️ para a FAT (Fundação de Apoio à Tecnologia)**
