using System.ComponentModel.DataAnnotations;

namespace NTComponents.Site.Documentation;

public sealed class DocumentationContactModel {
    [Required]
    public string Name { get; set; } = "Ada Lovelace";

    [Required, EmailAddress]
    public string Email { get; set; } = "ada@example.com";
}
