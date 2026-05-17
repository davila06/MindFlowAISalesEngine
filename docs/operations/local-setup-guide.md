# Local Setup Guide

This guide explains how to set up the development environment for NovaMind.

## Prerequisites

1. **Node.js**:
   - Install Node.js v16 or higher.

2. **.NET SDK**:
   - Install .NET SDK v6.0 or higher.

3. **Azure CLI**:
   - Install and configure Azure CLI.

## Steps

### 1. Clone the Repository
```bash
git clone <REPO_URL>
cd NovaMind - MindFlow AI sales engine
```

### 2. Frontend Setup
```bash
cd frontend
npm install
```

#### Environment Variables
- Copy `.env.example` to `.env.local`:
```bash
cp .env.example .env.local
```
- Update the following variables:
  - `NEXT_PUBLIC_API_URL`: URL of the backend API.

### 3. Backend Setup
```bash
cd backend/src/Api
```

#### Configure Secrets
- Use Azure Key Vault for secrets management:
```bash
az keyvault secret set --vault-name <KEY_VAULT_NAME> --name <SECRET_NAME> --value <SECRET_VALUE>
```
- Update `appsettings.json` to reference Key Vault secrets.

### 4. Run the Application

#### Backend
```bash
dotnet run
```

#### Frontend
```bash
npm run dev
```

### 5. Verify Setup
- Access the frontend at `http://localhost:3000`.
- Ensure the backend is running at `http://localhost:5165`.