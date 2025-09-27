**Operation Contract: CRUDShelfTenant

**Operation: 
-CanAdd(FirstName, LastName, Address, PostalCode, City, Email, PhoneNumber)

**Cross References: 
-UseCase002-AdministrateShelfTenant

**Preconditions: 
-No existing Tenant with the same Id in Db

**Postconditions: 
-Tenant-object is created and persisted



**Operation Contract: ReadShelfTenant

**Operation: 
-MVVM automatically shows content

**Cross References: 
-UseCase002-AdministrateShelfTenant

**Preconditions: 
-A tenant is selected

**Postconditions: 
-Tenant object is retrieved and its data is available for viewing



**Operation Contract: EditShelfTenant

**Operation: 
-CanSave(FirstName, LastName, Address, PostalCode, City, Email, PhoneNumber)

**Cross References: 
-UseCase002-AdministrateShelfTenant

**Preconditions: 
-A tenant is selected
-Fields are populated with data that follows certain criteria

**Postconditions: 
-Tenant object is updated with the new information



**Operation Contract: DeleteShelfTenant

**Operation: 
-CanDelete()

**Cross References: 
-UseCase002-AdministrateShelfTenant

**Preconditions: 
-A tenant is selected

**Postconditions: 
-Tenant object is deleted from the system
