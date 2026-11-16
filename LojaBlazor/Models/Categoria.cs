using System.ComponentModel.DataAnnotations;

namespace LojaBlazor.Models;

public class Categoria
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
    [MinLength(3, ErrorMessage = "O nome deve ter no mínimo 3 letras.")]
    // Regra criada por mim: o nome aceita apenas letras (com acento) e espaços entre palavras.
    // Faz sentido porque categoria é um rótulo de agrupamento: números e símbolos geram
    // categorias inconsistentes ("L1mpeza", "@@@") que atrapalham busca e organização.
    [RegularExpression(@"^\p{L}+( \p{L}+)*$",
        ErrorMessage = "O nome deve conter apenas letras e espaços entre as palavras (sem números ou símbolos).")]
    public string Nome { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "A quantidade deve estar entre 0 e 100000.")]
    public int Quantidade { get; set; }
}
