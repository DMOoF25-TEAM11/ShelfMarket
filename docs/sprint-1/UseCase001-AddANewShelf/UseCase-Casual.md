# Use Case 001 causal

**Use case name:** Administrere reoler  
**Scope:** Systemet er under udvikling  
**Level:** User goal  
**Primary Actors:** Ejer  
**Stakeholders and Interests:** 
- Ejer ønsker at administrere reoler i systemet
- Personale ønsker at se opdaterede reoloplysninger
- Kunder (lejere) ønsker at tilpasse deres lejeaftaler baseret på tilgængelige reoler
- Kunder (lejere) ønsker at ændre reoltype og placering

**Preconditions:** Ejer er logget ind og har åbnet Windows-applikationen  
**Postconditions:**  
- En ny reol er tilføjet til systemet  
- En reol slettes fra systemet  
- En reol er redigeret i listen  
- Reollisten er opdateret  

**Main success scenario:**  
1. Ejer åbner programmet og navigerer til administrations -> reol
2. Systemet viser en formular med felter for placering og type
3. Ejeren indtaster oplysninger og klikker “Tilføj”
4. Systemet validerer input (tjekker at type er valgt, placering er udfyldt og unik)
5. Systemet opretter reolen og gemmer den i databasen
6. Systemet viser en bekræftelse og opdaterer reollisten
7. Ejer ser den nye reol i listen
8. Ejer vælger en reol fra listen og klikker på ikonet "papirkurv"
9. Systemet fjerner reolen fra databasen og opdaterer listen
10. Systemet viser en bekræftelse på sletningen
11. Ejer vælger en reol fra listen og formularen udfyldes med reolens oplysninger
12. Ejer redigerer oplysningerne og klikker "Gem"
13. Systemet validerer input
14. Systemet opdaterer reolen i databasen
15. Systemet viser en bekræftelse og opdaterer reollisten
16. Ejer ser de opdaterede oplysninger i listen
17. Ejer går tilbage til forsiden ved at klikke på "Forside" knappen

**Extensions (alternatives):**  
- 4a. Placering er ikke unik:  
  - Systemet viser en fejlmeddelelse og beder ejeren vælge en anden placering.  
- 5a/14a. Systemfejl ved oprettelse eller opdatering (f.eks. databasefejl):  
  - Systemet viser en fejlmeddelelse og beder ejeren prøve igen senere.  
  - Fejlen logges til teknisk support.  
- 9a. Systemfejl ved sletning:  
  - Systemet viser en fejlmeddelelse og beder ejeren prøve igen senere.  
  - Fejlen logges til teknisk support.  