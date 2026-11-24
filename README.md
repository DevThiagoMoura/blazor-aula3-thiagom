# LojaBlazor – Produtos guardados no MySQL

Blazor Web App (.NET 10, render mode Interactive Server) com as telas **Produtos** e **Categorias**.
A tela `/produtos` lê e grava no MySQL com o EF Core.

## Como executar

1. No MySQL Workbench (ou linha de comando), crie o banco:
   ```sql
   CREATE DATABASE loja;
   ```
2. Ajuste usuário e senha em `LojaBlazor/appsettings.json` (`ConnectionStrings:Loja`).
3. Crie a tabela pela migration (o `dotnet-ef` está no manifesto `dotnet-tools.json`):
   ```bash
   dotnet tool restore
   dotnet ef database update --project LojaBlazor
   ```
4. Rode o app:
   ```bash
   dotnet run --project LojaBlazor
   ```

Acesse http://localhost:5151/produtos.

## A6 – Conexão com banco de dados

### O que foi feito

| Requisito | Onde |
|---|---|
| Provedor MySQL compatível com EF Core 10 | `MySql.EntityFrameworkCore` 10.0.12 (Oracle) + `Microsoft.EntityFrameworkCore.Design` 10.0.12 |
| String de conexão fora do código | `appsettings.json` → `ConnectionStrings:Loja`, lida em `Program.cs` com `GetConnectionString("Loja")` |
| DbContext em pasta própria | `Data/LojaContext.cs` com `DbSet<Produto> Produtos` |
| Fábrica de contextos | `AddDbContextFactory<LojaContext>(o => o.UseMySQL(...))` em `Program.cs` |
| Tabela criada por migration | `Data/Migrations/*_CriarTabelaProdutos.cs` (nenhum SQL escrito à mão) |
| Preço com 2 casas decimais | `HasPrecision(10, 2)` no `OnModelCreating` → coluna `decimal(10,2)` |
| Página lê/grava no banco | `Pages/Produtos.razor`: `OnInitializedAsync` lista, `Cadastrar` adiciona, `Excluir` remove; todos com `SaveChangesAsync` |
| Botão Excluir por EventCallback | `TabelaProdutos.razor` ganhou `EventCallback<Produto> AoExcluir`; só avisa, quem apaga é a página |
| Componentes sem conhecer o banco | `Cartao`, `FormProduto` e `TabelaProdutos` não referenciam `Data` nem EF Core |

### Pesquisa

**O que é o EF Core e que problema resolve?**
É o ORM (mapeador objeto-relacional) oficial do .NET. Ele liga classes C# a tabelas: em vez de escrever `INSERT`, `SELECT`, `DELETE` e converter cada linha em objeto na mão, trabalhamos com objetos (`Produto`) e LINQ, e o EF Core gera o SQL, abre a conexão e preenche os objetos. Também cria e evolui o banco a partir do código (migrations).

**Como o Blazor se conecta ao MySQL?**
- **Pacote:** um *provedor* do EF Core para MySQL. O mais conhecido, `Pomelo.EntityFrameworkCore.MySql`, só chega ao EF Core 9 (a versão 9.0.0 é a última). Como o projeto é .NET 10, usamos o provedor oficial da Oracle, `MySql.EntityFrameworkCore` 10.0.x, que depende do EF Core 10. Junto vai o `Microsoft.EntityFrameworkCore.Design`, que o comando `dotnet ef` usa para gerar migrations.
- **String de conexão:** `Server=localhost;Port=3306;Database=loja;User=...;Password=...`.
- **Onde fica:** na seção `ConnectionStrings` do `appsettings.json`. O `Program.cs` lê com `builder.Configuration.GetConnectionString("Loja")` e entrega ao `UseMySQL(...)`. Assim a conexão muda sem recompilar e não fica espalhada no C#.

**DbContext e DbSet**
- `DbContext` (`LojaContext`) representa uma sessão com o banco: guarda a configuração (provedor, conexão), o mapeamento das classes (`OnModelCreating`) e o *change tracker*, que anota o que foi adicionado, alterado ou removido.
- `DbSet<Produto>` representa a tabela `Produtos`: é por ele que consultamos (`db.Produtos.ToListAsync()`), adicionamos (`Add`) e removemos (`Remove`).

**Por que `IDbContextFactory` no Blazor Server?**
No Blazor Server o usuário fica conectado por um *circuito* (SignalR) que dura enquanto a aba estiver aberta, e o escopo de DI dura o circuito inteiro. Um `DbContext` injetado direto viveria esse tempo todo, e isso traz três problemas:
- ele não é thread-safe, então dois cliques rápidos podem disparar operações simultâneas no mesmo contexto e dar erro;
- o change tracker vai acumulando entidades e dados velhos;
- se uma operação falha, o contexto fica "sujo" para as seguintes.

Com a fábrica, cada operação cria um contexto curto (`await using var db = await DbFactory.CreateDbContextAsync();`), faz o trabalho e descarta. Essa é a recomendação da documentação da Microsoft para Blazor Server.

**Migrations**
Migration é uma classe C# gerada pelo EF Core que descreve, em código, como levar o banco de um estado para outro (`Up` aplica, `Down` desfaz). Fluxo:
1. `dotnet ef migrations add CriarTabelaProdutos` compara o modelo (`LojaContext` + `Produto`) com o snapshot anterior e gera o `CreateTable("Produtos", ...)` com `Id int auto_increment` (PK), `Nome varchar(100)` e `Preco decimal(10,2)`.
2. `dotnet ef database update` executa as migrations pendentes no MySQL e registra cada uma na tabela `__EFMigrationsHistory`, para não rodar de novo.

**Listar, adicionar, remover e por que salvar no final**
```csharp
var lista = await db.Produtos.AsNoTracking().OrderBy(p => p.Id).ToListAsync(); // SELECT
db.Produtos.Add(produto);                                                     // marca como Added
db.Produtos.Remove(existente);                                                // marca como Deleted
await db.SaveChangesAsync();                                                  // INSERT / DELETE de verdade
```
`Add` e `Remove` só alteram o estado das entidades **na memória** (change tracker). Nada chega ao banco até o `SaveChangesAsync()`, que gera os comandos SQL e os executa juntos em uma transação. Sem ele, a mudança se perde quando o contexto é descartado. No `Add`, é também o `SaveChanges` que traz de volta o `Id` gerado pelo MySQL.

### Prova de que funcionou

1. Banco `loja` criado (`CREATE DATABASE loja;`) e tabela gerada com `dotnet ef database update`. Resultado no MySQL:
   ```
   Field  Type           Null  Key  Extra
   Id     int            NO    PRI  auto_increment
   Nome   varchar(100)   NO
   Preco  decimal(10,2)  NO
   ```
2. Três produtos cadastrados pela tela: `prints/a6-produtos-antes.jpg`.
3. App parado e rodado de novo: os três continuam lá (`prints/a6-produtos-depois-reiniciar.jpg`).
4. A validação continua funcionando (`prints/a6-validacao.jpg`). O botão Excluir foi testado com um 4º produto, que sumiu da tela e do banco.
5. Tabela `loja.produtos` aberta no MySQL Workbench com os mesmos registros: `prints/a6-workbench.png`.

| Id | Nome | Preco |
|---|---|---|
| 1 | Arroz 5kg | 27.90 |
| 2 | Feijão Carioca 1kg | 8.49 |
| 3 | Óleo de Soja 900ml | 6.99 |

---

## Atividade anterior: componentes reutilizáveis

A tela `/produtos` continua igual no navegador, mas agora é montada com três componentes (pasta `LojaBlazor/Components`):

| Componente | Ideia aplicada | O que faz |
|---|---|---|
| `Cartao.razor` | `ChildContent` | Moldura (card do Bootstrap). Recebe `Titulo` por parâmetro e mostra o que estiver entre `<Cartao>` e `</Cartao>`. |
| `TabelaProdutos.razor` | `[Parameter]` | Recebe a lista inteira em `Produtos` e só desenha a tabela; não sabe de onde a lista vem. |
| `FormProduto.razor` | `EventCallback<Produto>` | Formulário com validação. Quando o cadastro é válido, avisa a página por `AoCadastrar` e se limpa. |

`Pages/Produtos.razor` só usa as tags `Cartao`, `FormProduto` e `TabelaProdutos` (mais o aviso de lista vazia), e continua dona da lista e do método `Cadastrar`.

Prints em [`prints/`](prints/): a tela com produtos cadastrados e o formulário com mensagens de erro.

### Por que o FormProduto não adiciona o produto direto na lista?

Porque o formulário só deve cuidar de coletar e validar os dados. Se ele mexesse na lista, ficaria preso a esta página e não poderia ser reaproveitado.
Quem guarda o produto é a página `Produtos.razor`, que é a dona da lista.
Ela fica sabendo porque passa o método `Cadastrar` para o parâmetro `AoCadastrar` do formulário. Quando o cadastro é válido, o `FormProduto` chama `AoCadastrar.InvokeAsync(produto)`: esse é o aviso do filho para o pai por `EventCallback`.
Então a página adiciona o produto, e o Blazor redesenha a `TabelaProdutos` com a lista nova.

---

## Atividade anterior (Aula 2): validação de formulário

### 1) Produtos não aceita lixo

- As regras moram no modelo `Models/Produto.cs` (DataAnnotations):
  - `[Required]` → nome vazio é recusado;
  - `[Range(0.01, ...)]` → preço menor ou igual a zero é recusado.
- `EditForm` + `DataAnnotationsValidator` conferem as regras; `ValidationMessage` mostra o erro, em português, logo abaixo do campo.
- `OnValidSubmit` só é chamado quando tudo é válido: o produto entra na tabela e o formulário é recriado (`new Produto()`), voltando vazio.

### 2) Tela de Categorias (`/categorias`)

Mesmo padrão de Produtos: formulário, tabela e aviso quando a lista está vazia. Link no menu lateral.

| Campo | Regra |
|---|---|
| Nome | obrigatório, no mínimo 3 letras |
| Quantidade | entre 0 e 100000 |
| Nome (regra criada) | apenas letras (com acento) e espaços entre palavras |

**Regra criada:** o nome da categoria aceita somente letras e espaços entre palavras, porque categoria é um rótulo de agrupamento e números ou símbolos ("L1mpeza", "@@@") geram categorias inconsistentes que atrapalham busca e organização.

### Erro CS0117

No Blazor, o nome do arquivo `.razor` vira o nome da classe do componente. Se a página se chamar `Categoria.razor`, é gerada uma classe `Categoria` (o componente) que "esconde" o modelo `LojaBlazor.Models.Categoria`. Aí `new Categoria { Nome = ... }` passa a se referir ao componente, que não tem `Nome` nem `Quantidade` → **CS0117** ("'Categoria' não contém uma definição para 'Nome'").
Correção: nomear a página no plural (`Categorias.razor`, assim como `Produtos.razor`), mantendo o modelo no singular.
