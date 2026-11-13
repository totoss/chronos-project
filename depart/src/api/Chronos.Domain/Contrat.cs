// TODO [module 04] Entité contrat : identifiant, identifiant du collaborateur, type (TypeContrat), quotité, date de début et date de fin (absente tant que le contrat est
//    en cours), à accesseurs d'écriture privés. La quotité est une fraction décimale (0,80 pour 80 %) et RG-03 la veut comprise entre 10 % et 100 %,
//    bornes comprises : le constructeur (type, quotité, date de début) lève RegleMetierException avec le code de la règle sinon. Une méthode dit si le contrat est
//    actif un jour donné (de son début à sa fin comprise). La clôture d'un contrat n'est pas publique : seul le collaborateur, qui est dans le même projet, la
//    déclenche. Tester RG-03 aux quatre limites par une théorie.
//    — cours/04-modeliser-le-domaine.md, À vous, exercice 5 « `Contrat` et RG-03. »

namespace Chronos.Domain;

public class Contrat
{
}
