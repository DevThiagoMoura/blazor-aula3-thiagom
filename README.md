# LojaBlazor – Componentes reutilizáveis em Blazor

Blazor Web App (.NET 10, render mode Interactive Server) com as telas **Produtos** e **Categorias**.

## Como executar

Abra `LojaBlazor.slnx` no Rider e rode a configuração `LojaBlazor: http`, ou pelo terminal:

```bash
dotnet run --project LojaBlazor
```

Acesse http://localhost:5151.

## Exercício: componentes reutilizáveis

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
