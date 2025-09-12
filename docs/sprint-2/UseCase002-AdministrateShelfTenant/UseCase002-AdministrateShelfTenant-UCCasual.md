# Use case 002 causal

**Use case name:** Administrerer reol lejer
 
**Primary Actors:** Ejer  

Beskrivelse: Ejer �nsker at udf�re CRUD-operationer p� en reol lejer. Lejeren er registreret med
email, telefonnummer, navn og adresse som kan �ndres, samt et unikt-id. System validere input og
sender meddelselser om n�dvendigt.

**Main success scenario:** 

1. Ejer logger ind i systemet
2. Ejer navigerer til �Lejer-administration�
3. Ejer intaster ny adresse og trykker p� update
4. Systemet validerer input
5. Systemet gemmer den nye adresse
6. Systemet Viser den nye adresse

**Extensions (alternatives):** 

4a. Hvis input er ugyldigt, vises fejlmeddelelser og adressen �ndres ikke