import { bootstrapApplication } from '@angular/platform-browser';

import { App } from './app/app';
import { appConfig } from './app/app.config';

bootstrapApplication(App, appConfig).catch((error: unknown) => {
  document.body.textContent = 'Não foi possível iniciar a aplicação.';
  throw error;
});
