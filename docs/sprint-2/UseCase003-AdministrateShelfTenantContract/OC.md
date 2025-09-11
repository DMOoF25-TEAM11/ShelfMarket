# Object Contracts for UseCase003 Interactions

## 1 Åbn kontrakt administration 
**Operation**: NavigerTil("Administrer hyreskontrakter")  
**Cross References**: SSD.mmd; UseCase-Brief.md trin 1  
**Preconditions**: 
  - Appen kører og brugeren er autentificeret.
  - Navigationsmenuen er tilgængelig.
**Postconditions**:  
  - Siden til administration af kontrakter er indlæst.
  - Kontraktlisten og en tom formular vises.

## 2 Vælg lejer
**Operation**: dropdownValg(lejer)  
**Cross References**: SSD.mmd; UseCase-Brief.md trin 2–3  
**Preconditions**:
  - Kontraktadministrationssiden er aktiv.
  - Listen over lejere er indlæst.
**Postconditions**:
  - Valgt lejer gemmes i nuværende kladde-/redigeringskontekst.
  - Afhængige felter (fx reoler, reoltyper) kan blive filtreret.

## 3 Tilføj kontrakt linje
**Operation**: UdfyldFormular(StartMåned, Slutmåned, Pris)
**Cross References**: SSD.mmd (Tilføj kontrakt linjer loop); UseCase-Brief.md trin 4–8
**Preconditions**:
  - Kontraktadministrationssiden er aktiv; lejer er valgt.
  - Linjeeditoren er synlig; valideringsregler er tilgængelige.
**Postconditions**:
  - Ordrelinje tilføjet/opdateret i en midlertidig liste (endnu ikke gemt).
  - UI-listen viser den nye/opdaterede linje; valideringsfeedback vises hvis ugyldig.

## 4 Vælg kontrakt 
**Operation**: vælgerKontrakt(kontrakt)
**Cross References**: SSD.mmd; UseCase-Brief.md trin 12–13
**Preconditions**:
  - Kontraktlisten er indlæst.
  - Den valgte kontrakt eksisterer og er tilgængelig.
**Postconditions**:
  - Formularen udfyldes med kontraktfelter.
  - Ordrelinjer indlæses i editoren; redigeringskontekst sættes til kontrakten.

## 5 Redigér kontrakt linje
**Operation**: OpdaterFormular(StartMåned, Slutmåned, Pris)
**Cross References**: SSD.mmd (Rediger kontrakt linjer loop); UseCase-Brief.md trin 14–16
**Preconditions**:
  - En kontrakt (ny eller eksisterende) er i redigeringskontekst.
  - Målordrelinje er valgt eller tilføj-tilstand er aktiv.
**Postconditions**:
  - Ordrelinjeværdier opdateret i hukommelsen; totaler genberegnes hvis relevant.
  - UI-listen opdateres; valideringsfeedback vises hvis ugyldig.

## 6 Slet kontrakt
**Operation**: klikSletKontrakt(kontrakt)
**Cross References**: SSD.mmd; UseCase-Brief.md trin 18–21
**Preconditions**:
  - Kontrakten eksisterer og kan vælges.
  - Brugeren har tilladelse til at slette.
**Postconditions**:
  - Sletteanmodning er igangsat; systemet bør bede om bekræftelse.
  - Ved bekræftelse (udenfor dette kald) fjernes kontrakten og listen opdateres.

## 7 Gå til anden side
**Operation**: klikkerMenu(side)
**Cross References**: SSD.mmd; UseCase-Brief.md trin 22
**Preconditions**:
  - Navigationsmenuen er synlig.
**Postconditions**:
  - Den ønskede side er indlæst.
  - Hvis der er ugemte ændringer, vises en bekræftelse/advarsel før navigation.