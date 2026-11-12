using System.ComponentModel.DataAnnotations;

namespace Chronos.Api.Clients;

public record ClientDto(int Id, string Nom, int ProjetsEnCours);

public record CreerClientRequete([Required, StringLength(200)] string Nom);

public record ModifierClientRequete([Required, StringLength(200)] string Nom);
