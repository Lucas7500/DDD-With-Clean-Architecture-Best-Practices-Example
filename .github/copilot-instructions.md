# Copilot Instructions

## Diretrizes de projeto
- Nos testes de NUnit, o usuário prefere não usar atribuição `null!` em dependências privadas.
- Nos testes de NUnit, não inicializar dependências privadas diretamente no campo; concentrar a inicialização no método `SetUp`.
- Sempre que testar métodos async com NSubstitute, usar atribuição de descarte (`_ =`) nas verificações/chamadas assíncronas.

## Padrões Identificados por Projeto

### 1. `BookStore.API`
- **Controllers Base:** Todos os controllers devem herdar de `ApiBase`.
- **Versionamento:** A API requer os atributos `[ApiVersion("X.X")]` e a rota `[Route("api/v{version:apiVersion}/[controller]")]`.
- **Injeção de Dependências:** O projeto adota a injeção nas ações (métodos) do controller usando o atributo `[FromServices]` em vez de no construtor.
- **Tipos de Retorno Seguros:** As respostas devem retornar `IActionResult` através de métodos auxiliares como `OkOrBadRequest(result)`, processando wrappers da biblioteca `ErrorOr`.
- **Modificadores:** Preferencialmente use o modificador `sealed` em todos os controllers e implementações.
- **Isolamento Constritivo:** Utilize Strongly Typed IDs (ex: `AuthorId`) como passo inicial a partir de um `Guid` nativo nos endpoints.

### 2. `BookStore.Domain`
- **Modelação Estrutural:** Os modelos de domínio formam raízes de agregação (Aggregate Roots), geralmente herdando da classe base `AggregateRoot<TId, TKey>` e utilizando as abstrações `Entity`.
- **Identificadores (IDs):** O uso de Strongly Typed IDs (ex: `AuthorId`, `BookId`) é mandatório, implementando a interface `IStronglyTypedId`.
- **Mapeamento (ORM/NHibernate/EF):** As entidades de domínio precisam de um construtor `protected` sem parâmetros para resolver proxies e materialização nos objetos através do ORM.
- **Encapsulamento:** As propriedades públicas devem definir modificadores `protected set` e não permitir mutações externas, exceto por métodos do modelo explícitos (`AlterarX(...)`).
- **Validação de Negócio (Regras):** Em vez de tratar exceções de validação de qualquer jeito, é utilizado o método interno `CheckRule(new RegraDeNegocio(this))` baseados na interface `IBusinessRule`. As exceptions de negócio herdam de `BusinessRuleValidationException`.

### 3. `BookStore.Application`
- **CQRS / UseCases:** Separado pelo padrão de Use Cases. Cada funcionalidade é executada numa classe separada (ex. `AddAuthorUseCase`), implementando uma respectiva Interface (`IAddAuthorUseCase`) e um método `ExecuteAsync`.
- **Primary Constructors & Sealed:** As classes de aplicação são marcadas com o modificador `internal sealed` (ou `sealed`) e tiram proveito de Primary Constructors introduzidos em versões recentes do C#.
- **Wrapper de Retorno:** Retornos encapsulados usando a bibliteca `ErrorOr<TResponse>`. Validar resultados no caso de uso, evitando o disparo direto de Exceptions como erro padrão (Pattern de Return Value/Result).
- **Validações de Entrada (FluentValidation):** Classes de Request DTO são validadas através da injeção de `IValidator<TRequest>` (via pacote FluentValidation) como primeiro passo executado no `UseCase`. Erros geram retorno de falha no `ErrorOr`.
- **Padrão UnitOfWork & Repositórios:** Manipulações de banco de dados são fetas delegadas pro `IUnitOfWork` que fornece Repositorios (`unitOfWork.AuthorsRepository.AddAsync(...)`) com `CommitAsync` e `RollbackAsync` explícitos dentro de cada Use Case.
- **Mapeamento Extension Method:** Usa de conversão explícita baseada em extension methods (`ToResponse(...)`) encontrados na pasta `Mappers`, não dependendo de automappers em tempo de execução.

### 4. Testes (`xUnit`, `NUnit`, `MSTest`)
- **Nomenclatura (Naming Convention):** Os testes em toda a solução usam a convenção de `NomedoMetodo_Cenario_ResultadoEsperado` ou similar (`ExecuteAsync_GivenValidRequest_ShouldAddAuthorAndCommit`).
- **Fake Data Generators:** Utiliza da biblioteca de mock de dummy fields chamada _Bogus_ (Classe `Faker`).
- **Mocking Libraries:** Os testes na solution abordam múltiplas frameworks de Isolamento (Moq, NSubstitute, FakeItEasy) isoladas em escopos/namespaces diferentes para fins didáticos/exemplificação.
- **xUnit Guidelines:** Dependências privadas devem ser inicializadas no construtor da classe de testes. Testes frequentemente agrupados usando classes aninhadas (Nested Classes), como em `public sealed class UsingStandardAssertions`.
- **MSTest Guidelines:** Instâncias privadas nas classes costumam ser atribuídas com o supressor default `= null!`, com a instanciação ocorrendo dentro do método anotado com `[TestInitialize]`.
- **Métodos Privados Auxiliares:** Métodos privados auxiliares em classes de extensão devem ficar ao final da classe.