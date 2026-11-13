// TODO [module 03] La requête de création d'un projet : code, libellé, nature, dates de début et de fin (facultative), budget en
//    jours. Au module 03, elle est déclarée dans ProjetDto.cs, pas ici.
//    — cours/03-premiere-api.md, Pas à pas, étape 6 « Décrire un projet : le DTO »
// TODO [module 06] Toujours dans ProjetDto.cs, elle gagne le client (facultatif) et le responsable, par leurs identifiants.
//    — cours/06-persister-avec-ef-core.md, Pas à pas, étape 9 « Servir `/api/projets` depuis la base »
// TODO [module 08] La déplacer dans ce fichier et l'annoter : code (obligatoire, 20 caractères au plus), libellé (obligatoire,
//    200 au plus), nature, date de début, budget en jours (obligatoire, jamais négatif) et responsable obligatoires ;
//    les types qui doivent pouvoir manquer deviennent nullables pour que l'absence soit détectée. RG-11 et RG-12 ne se
//    contrôlent pas ici : le domaine les refuse (422). Le fichier ne contient encore aucune déclaration : y écrire le type au moment de le déplacer.
//    — cours/08-une-api-robuste.md, À vous, exercice 1 « Les projets. »
// TODO [module 17] Donner à chaque annotation un message en français.
//    — cours/17-formulaires.md, Pas à pas, étape 6 « Les refus de l'API » (encadré « Compléter le contrat de l'API. »)
