# OC - Operations Contracts

## Indholdsfortegnelse
- [Operation: åbnMenu()](#operation-åbnmenu)
- [Operation: login(rolle, pinkode)](#operation-loginrolle-pinkode)
- [Operation: vælgMenupunkt(menupunktId)](#operation-vælgmenupunktmenupunktid)


## Operation: åbnMenu()
**Cross References**
- SSD: Rollebaseret menuvisning (trin 1)

**Preconditions**
- Ingen – bruger åbner systemet uden at være logget ind.

**Postconditions**
- Hvis bruger ikke er logget ind → systemet returnerer visMenu(gæst).
- Systemet viser kun gæstefunktioner.

## Operation: login(rolle, pinkode)
**Cross References**
- SSD: Rollebaseret menuvisning (trin 2)

**Preconditions**
- Bruger indtaster gyldigt login (rolle og pinkode).

**Postconditions**
- Hvis login er gyldigt:
  - Systemet identificerer brugerens rolle.
  - Systemet returnerer visMenu(rolle) med de menupunkter, rollen har adgang til.
- Hvis login er ugyldigt:
  - Systemet returnerer fejlbesked("ugyldigt login").
  - Ingen rolle tildeles.

## Operation: vælgMenupunkt(menupunktId)
**Cross References**
- SSD: Rollebaseret menuvisning (trin 3)

**Preconditions**
- Bruger er logget ind (eller gæst) og har fået vist en menu.

**Postconditions**
- Systemet udfører udførFunktion(menupunktId).
