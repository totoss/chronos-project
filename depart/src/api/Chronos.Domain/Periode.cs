// TODO [module 04] Objet valeur « période » : une structure record en lecture seule (readonly record struct), sans identité et immuable, avec deux bornes de
//    dates incluses (début, fin). Un constructeur qui refuse une période à l'envers par une ArgumentException (d'où l'écriture avec constructeur plutôt que
//    la forme positionnelle). Deux méthodes : « Contient » (un jour est-il dans la période, bornes comprises) et « Chevauche » (deux périodes ont-elles un
//    jour commun, bornes comprises). L'égalité par valeur vient du mot « record » : rien à écrire.
//    — cours/04-modeliser-le-domaine.md, Pas à pas, étape 9 « Un objet valeur : `Periode` »
// TODO [module 04] Ajouter « NombreDeJours » (les deux bornes comptent) et « Intersection » (la période commune, ou rien si les périodes sont disjointes :
//    choisir un type de retour qui dit « ou rien »). Écrire les tests d'abord.
//    — cours/04-modeliser-le-domaine.md, À vous, exercice 2 « Compléter `Periode`. »

namespace Chronos.Domain;

public readonly record struct Periode
{
}
