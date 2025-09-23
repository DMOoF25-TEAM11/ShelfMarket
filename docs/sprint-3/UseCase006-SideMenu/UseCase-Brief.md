# Use Case Brief: Rollebaseret menuvisning

**Use case name:** 
Rollebaseret menuvisning

**Scope:** 
Genbrugsmarkedets IT-system (menu- og adgangsstyring)

**Primary Actors:** 
Gæst, Bruger, Admin

**Goal:** 
At vise forskellige menupunkter baseret på brugerens rolle, så hver aktør kun får adgang til relevante funktioner.

**Stakeholders and Interests:**
- Lejere: Skal kunne se egne reoler og udskrive stregkoder.
- Personalet: Skal kunne udføre salg og se ledige reoler.
- Ejer: Skal kunne administrere økonomi, reoler, lejere og have fuld adgang til systemet.
- Ejer: Skal sikre, at systemet fungerer korrekt, sikkert og begrænser adgang til personlige oplysninger.

**Preconditions:** 
- Bruger skal være logget ind for at se relevante menupunkter.
- Systemet har registreret brugerens rolle.

**Postconditions:**
- Menuen viser kun de menupunkter, der er relevante for brugerens rolle.

**Main success scenario:**
1. Bruger logger ind eller identificeres som gæst.
2. Systemet validerer kode og rolle.
3. Systemet genererer menuen baseret på brugerens rolle.
1. Bruger får vist de menupunkter, der matcher rollen.
1. Bruger vælger et menupunkt og navigerer til den ønskede funktion.
1. Systemet udfører den valgte funktion. 
