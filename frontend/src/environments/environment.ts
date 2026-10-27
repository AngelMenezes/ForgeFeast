export interface Environment {
  readonly production: boolean;
  readonly apiBaseUrl: string;
}

export const environment: Environment = {
  production: true,
  apiBaseUrl: '/api',
};
