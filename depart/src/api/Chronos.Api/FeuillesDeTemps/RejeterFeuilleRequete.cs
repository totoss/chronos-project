// TODO [module 10] La requête de rejet d'une feuille : l'identifiant du valideur (obligatoire) et le motif (obligatoire, 500 caractères au plus).
//    Le cours crée aussi, au même endroit et pour la même étape, une requête de validation (identifiant du valideur seul) :
//    elle est provisoire (ValiderFeuilleRequete.cs, créée ici puis supprimée au module 12) et n'est pas présente dans ce dossier.
//    — cours/10-temps-et-absences.md, Pas à pas, étape 4 « Les endpoints de la feuille »
// TODO [module 12] Le valideur est l'utilisateur connecté : retirer l'identifiant du valideur (un identifiant fourni par le client serait un
//    moyen d'agir au nom d'un autre) ; il ne reste que le motif, avec un message en français.
//    — cours/12-securite-et-tracabilite.md, Pas à pas, étape 8 « RG-36 : valider la feuille d'un autre »

namespace Chronos.Api.FeuillesDeTemps;

public record RejeterFeuilleRequete;
