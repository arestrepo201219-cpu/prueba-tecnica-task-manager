export interface User {
  readonly id: number;
  readonly name: string;
  readonly email: string;
  readonly createdAt: string;
}

export interface CreateUserRequest {
  readonly name: string;
  readonly email: string;
}

export const USER_NAME_MAX_LENGTH = 100;
export const USER_EMAIL_MAX_LENGTH = 256;
