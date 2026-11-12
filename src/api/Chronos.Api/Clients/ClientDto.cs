namespace Chronos.Api.Clients;

public record ClientDto(int Id, string Nom, int ProjetsEnCours);

public record CreerClientRequete(string Nom);
