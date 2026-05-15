export interface User {
  id: string;
  fullName: string;
  email: string;
  tenantId?: string;
  roles: string[];
  plan?: string;
}