# Security Guide: Secrets and Permissions

This document outlines best practices for managing secrets and permissions in the NovaMind project.

## Secrets Management

### 1. Azure Key Vault

1. **Purpose**:
   - Securely store and manage sensitive information such as API keys, connection strings, and certificates.

2. **Setup**:
   - Create a Key Vault in Azure.
   - Add secrets using the Azure Portal or CLI.

3. **Access**:
   - Use Managed Identity to access Key Vault from the backend.

4. **Example**:
   ```csharp
   var secretClient = new SecretClient(new Uri("https://<keyvault-name>.vault.azure.net"), new DefaultAzureCredential());
   var secret = await secretClient.GetSecretAsync("DatabaseConnectionString");
   ```

### 2. Local Development

1. **Environment Variables**:
   - Use `.env` files for local development.
   - Example:
     ```env
     DATABASE_CONNECTION_STRING=Server=localhost;Database=MindFlow;User Id=sa;Password=your_password;
     ```

2. **Do Not Commit Secrets**:
   - Add `.env` to `.gitignore`.

## Permissions Management

### 1. Azure AD Roles

1. **Purpose**:
   - Manage user and application access to Azure resources.

2. **Setup**:
   - Assign roles using the Azure Portal or CLI.

3. **Example**:
   - Assign `Reader` role to a user:
     ```bash
     az role assignment create --assignee user@example.com --role Reader --scope /subscriptions/<subscription-id>
     ```

### 2. Backend Permissions

1. **Principle of Least Privilege**:
   - Grant only the permissions required for a task.

2. **Example**:
   - Use role-based access control (RBAC) for API endpoints.
   ```csharp
   [Authorize(Roles = "Admin")]
   public IActionResult GetSensitiveData()
   {
       // ...
   }
   ```

## Best Practices

1. **Rotate Secrets Regularly**:
   - Use Azure Key Vault to automate secret rotation.

2. **Audit Permissions**:
   - Regularly review and audit permissions using Azure Monitor.

3. **Monitor Access**:
   - Enable logging for Key Vault and Azure AD access.

4. **Secure Local Development**:
   - Use tools like `dotenv` to manage environment variables securely.