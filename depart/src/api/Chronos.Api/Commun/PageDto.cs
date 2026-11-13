// TODO [module 06] Le format de page commun à toute liste paginée de l'API, un record générique sur le type de l'élément :
//    les éléments (liste en lecture seule), le numéro de page, la taille de page et le total d'éléments.
//    La réponse JSON est « { elements, page, taille, total } ».
//    — cours/06-persister-avec-ef-core.md, Pas à pas, étape 9 « Servir `/api/projets` depuis la base »

namespace Chronos.Api.Commun;

public record PageDto<T>;
