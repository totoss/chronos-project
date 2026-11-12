# Cahier des charges fonctionnel — Chronos

Ce document décrit **ce que** l'application doit faire, indépendamment de la technique. C'est la référence métier du cours : chaque module renvoie aux règles de gestion (`RG-xx`) qu'il met en œuvre.

## 1. Contexte

Une société de conseil emploie des consultants et des salariés internes. Elle a besoin de savoir, à tout moment :

- **qui travaille sur quoi**, et pour combien de temps ;
- **qui est disponible**, surchargé ou sans mission ;
- **combien de temps a réellement été passé** sur chaque projet ;
- **qui est absent** et quand.

Chronos remplace les tableurs utilisés jusqu'ici pour ce suivi.

## 2. Acteurs

| Acteur | Ce qu'il fait dans Chronos |
| --- | --- |
| **Collaborateur** | Consulte son planning, saisit ses temps, pose ses absences. |
| **Manager** | Affecte les membres de son équipe aux projets, valide leurs temps et leurs absences, suit leur charge. |
| **Gestionnaire RH** | Gère les fiches des collaborateurs, les contrats, les types d'absence et le calendrier. |
| **Administrateur** | Gère les comptes, les rôles et le paramétrage. |

Une même personne peut cumuler plusieurs rôles (un manager est aussi un collaborateur).

## 3. Vocabulaire métier

| Terme | Définition |
| --- | --- |
| **Collaborateur** | Personne employée par la société, consultant ou salarié interne. |
| **Quotité** | Part de temps de travail du collaborateur : 100 % pour un temps plein, 80 % pour un 4/5ᵉ. |
| **Client** | Société pour laquelle un projet est réalisé. |
| **Projet** | Ensemble de travaux identifié par un code. Il est *facturable* (pour un client) ou *interne* (formation, avant-vente, structure). |
| **Affectation** | Rattachement d'un collaborateur à un projet, sur une période, avec un taux d'allocation. |
| **Taux d'allocation** | Part de la capacité du collaborateur réservée à un projet (ex. 50 %). |
| **Capacité** | Nombre de jours qu'un collaborateur peut travailler sur une période. |
| **Charge planifiée** | Somme des jours réservés par les affectations sur une période. |
| **Taux de charge** | Charge planifiée ÷ capacité. |
| **Intercontrat** | Situation d'un consultant sans affectation facturable. |
| **Saisie de temps** | Temps déclaré par un collaborateur, un jour donné, sur un projet. |
| **Feuille de temps** | Ensemble des saisies d'un collaborateur sur une semaine, soumis à validation. |
| **Taux d'occupation** | Jours facturables réellement saisis ÷ capacité. |
| **Absence** | Période non travaillée : congés payés, RTT, maladie, etc. |

## 4. Périmètre fonctionnel

### 4.1 Collaborateurs (informations RH)

- Créer, modifier, consulter et archiver une fiche collaborateur : matricule, nom, prénom, adresse électronique professionnelle, date d'entrée, date de sortie éventuelle, type de contrat, quotité, poste, manager.
- Conserver l'historique des contrats (un changement de quotité ne doit pas fausser le passé).
- Rechercher et filtrer les collaborateurs (nom, équipe, statut actif ou sorti).

### 4.2 Clients et projets

- Gérer les clients.
- Créer et suivre un projet : code, libellé, client éventuel, nature (facturable ou interne), dates de début et de fin, budget en jours, responsable, statut (*en préparation*, *en cours*, *terminé*, *annulé*).

### 4.3 Affectations et charge

- Affecter un collaborateur à un projet sur une période, avec un taux d'allocation.
- Visualiser le plan de charge d'une équipe : par collaborateur et par semaine ou par mois, avec le taux de charge.
- Signaler les surcharges, les sous-charges et les intercontrats.

### 4.4 Temps passés

- Saisir ses temps par jour et par projet, à la semaine.
- Soumettre sa feuille de temps ; le manager la valide ou la rejette avec un motif.
- Comparer le temps consommé au budget du projet.

### 4.5 Absences

- Déposer une demande d'absence (type, date de début, date de fin, demi-journées).
- Le manager approuve ou refuse la demande.
- Les absences approuvées réduisent la capacité du collaborateur.

### 4.6 Calendrier

- Gérer les jours fériés. Les samedis, dimanches et jours fériés ne sont pas travaillés.

### 4.7 Hors périmètre

Paie, recrutement, notes de frais, facturation, entretiens annuels. Chronos pourra fournir des données à ces outils, mais ne les remplace pas.

## 5. Règles de gestion

### Collaborateurs

- **RG-01** — Le matricule et l'adresse électronique d'un collaborateur sont uniques.
- **RG-02** — La date de sortie, si elle est renseignée, est postérieure ou égale à la date d'entrée.
- **RG-03** — La quotité est comprise entre 10 % et 100 %.
- **RG-04** — Un collaborateur ne peut pas être son propre manager, ni directement ni par une chaîne de managers.
- **RG-05** — Un collaborateur n'est jamais supprimé physiquement s'il possède des saisies, des affectations ou des absences : il est archivé.

### Projets

- **RG-10** — Le code d'un projet est unique.
- **RG-11** — Un projet facturable a obligatoirement un client ; un projet interne n'en a pas.
- **RG-12** — La date de fin d'un projet, si elle est renseignée, est postérieure ou égale à sa date de début.
- **RG-13** — On ne peut ni affecter un collaborateur ni saisir du temps sur un projet *terminé* ou *annulé*.

### Affectations et charge

- **RG-20** — Une affectation est comprise dans la période du projet et dans la période de présence du collaborateur.
- **RG-21** — Le taux d'allocation d'une affectation est compris entre 5 % et 100 %.
- **RG-22** — Capacité d'un collaborateur sur une période = (jours ouvrés − jours d'absence approuvée) × quotité.
- **RG-23** — Charge planifiée sur une période = somme, pour chaque affectation, des jours ouvrés couverts × taux d'allocation.
- **RG-24** — Un taux de charge supérieur à 100 % est une **surcharge**. Elle est autorisée mais signalée : le manager doit la confirmer.
- **RG-25** — Un consultant dont le taux de charge facturable est nul sur une semaine complète est en **intercontrat**.

### Temps passés

- **RG-30** — Le temps se saisit par quarts de journée : 0,25 ; 0,5 ; 0,75 ; 1.
- **RG-31** — Le total des saisies d'un collaborateur sur une journée ne dépasse pas 1 jour.
- **RG-32** — Aucune saisie n'est possible un jour non travaillé (week-end, jour férié) ni sur une demi-journée d'absence approuvée.
- **RG-33** — Un collaborateur ne saisit du temps que sur les projets auxquels il est affecté à la date concernée.
- **RG-34** — Une feuille de temps suit le cycle *brouillon → soumise → validée* ou *rejetée*. Une feuille rejetée revient à l'état de brouillon avec le motif du rejet.
- **RG-35** — Une feuille soumise ou validée n'est plus modifiable par le collaborateur.
- **RG-36** — Seul le manager du collaborateur (ou un gestionnaire RH) peut valider ou rejeter sa feuille. Nul ne valide sa propre feuille.

### Absences

- **RG-40** — Une demande d'absence suit le cycle *demandée → approuvée* ou *refusée*. Elle peut être *annulée* par le demandeur tant qu'elle n'a pas commencé.
- **RG-41** — Deux absences d'un même collaborateur ne se chevauchent pas.
- **RG-42** — La durée d'une absence se compte en jours ouvrés, par demi-journées.
- **RG-43** — Une absence ne peut pas être approuvée si du temps a déjà été validé sur la période.

### Droits d'accès

- **RG-50** — Un collaborateur ne voit que ses propres données (fiche, temps, absences, affectations).
- **RG-51** — Un manager voit en plus les données de son équipe, directe et indirecte.
- **RG-52** — Les informations contractuelles (type de contrat, quotité, dates) ne sont modifiables que par un gestionnaire RH.
- **RG-53** — Toute création, modification ou validation est tracée : qui, quand.

## 6. Exigences non fonctionnelles

- **Données personnelles** — Chronos manipule des données RH. On ne collecte que le nécessaire (pas de motif médical pour un arrêt maladie, par exemple), l'accès est restreint par rôle et les actions sont tracées (RGPD).
- **Fiabilité des calculs** — Les calculs de capacité et de charge sont couverts par des tests automatisés : c'est le cœur de valeur de l'outil.
- **Dates** — Les dates métier (jour de saisie, début d'absence) sont des dates calendaires sans heure ni fuseau. Les horodatages techniques (traçabilité) sont enregistrés en UTC.
- **Langue** — Interface en français ; les dates et les nombres suivent les conventions françaises.
- **Accessibilité** — L'interface est utilisable au clavier et avec un lecteur d'écran.

## 7. Exemple chiffré

Semaine du lundi 4 au vendredi 8 mai 2026. Le vendredi 8 mai est férié : la semaine compte **4 jours ouvrés**.

Camille est à 80 % et a posé une journée de congé le lundi 4.

- Capacité (RG-22) = (4 − 1) × 0,8 = **2,4 jours**.

Camille est affectée à 50 % au projet A et à 50 % au projet B sur toute la semaine. Les allocations portent sur les 4 jours ouvrés de la semaine.

- Charge planifiée (RG-23) = 4 × 0,5 + 4 × 0,5 = **4 jours**.
- Taux de charge = 4 ÷ 2,4 ≈ **167 %** → surcharge (RG-24).

Cet exemple montre pourquoi la règle mérite d'être discutée : faut-il que les affectations tiennent compte de la quotité et des absences ? Ce point sera tranché, tests à l'appui, dans le module consacré au calcul de charge.

## 8. Précisions apportées en cours de projet

Un cahier des charges vit : quand le développement révèle une ambiguïté, la décision prise est consignée ici, sans réécrire le texte d'origine. Les sections 5 et 7 restent donc telles qu'elles ont été rédigées — les modules 02 et 09 du cours s'appuient sur leur formulation initiale.

> Mentoré : ne lisez cette section qu'après avoir terminé le [module 09](../cours/09-affectations-et-calcul-de-charge.md).

| Règle | Précision | Décidée au |
| --- | --- | --- |
| **RG-23** | Le taux d'allocation est une part de la capacité **du collaborateur** (§ 3). La charge planifiée tient donc compte de la quotité : *charge planifiée = somme, pour chaque affectation, des jours ouvrés couverts × quotité × taux d'allocation*. Elle ne tient pas compte des absences, qui ne réduisent que la capacité (RG-22) : c'est ce qui rend visible la surcharge créée par une absence. | Module 09 |
| **RG-24** | La confirmation est demandée au moment où le manager crée ou modifie une affectation qui met le collaborateur en surcharge : sans confirmation explicite, l'affectation est refusée. Une surcharge qui apparaît plus tard — parce qu'une absence est approuvée, par exemple — est signalée dans le plan de charge, sans nouvelle confirmation. | Module 09 |
| **RG-25** | La règle parle d'une semaine complète. Sur une période plus longue (plan de charge par mois), un consultant est signalé en intercontrat quand sa charge facturable est nulle sur **toute** la période alors que sa capacité est positive. La vue par semaine reste la référence. | Module 09 |
| **§ 7** | Avec RG-23 précisée, la charge planifiée de Camille est 4 × 0,8 × 0,5 + 4 × 0,8 × 0,5 = **3,2 jours**, pour une capacité inchangée de 2,4 jours : taux de charge ≈ **133 %**. Camille reste en surcharge, à cause de son congé et non de son temps partiel. | Module 09 |
