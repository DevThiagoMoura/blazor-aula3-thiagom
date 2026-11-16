using System.ComponentModel.DataAnnotations;

namespace LojaBlazor.Models;

// As regras de validação moram no próprio modelo (DataAnnotations).
// O EditForm + DataAnnotationsValidator conferem essas regras e o
// ValidationMessage mostra o aviso logo abaixo de cada campo.
public class Produto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome do produto é obrigatório.")]
    public string Nome { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
    public decimal Preco { get; set; }
}
