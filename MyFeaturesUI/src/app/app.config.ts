import { ApplicationConfig } from '@angular/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';

import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { ConfirmationService } from 'primeng/api';
import { apiConfigFactory } from '../config/api-configuration.factory';
import { Configuration } from '../infrastructure';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(withInterceptorsFromDi()),
    //moram registrirat korištenje konfiguracije koja definira environment
    //koji će se switchat ovisno o build konfiguraciji
    {
      provide: Configuration,
      useFactory: apiConfigFactory
    },
    //moram provide-at zato što ga koristim u app.component.html (null injector error)
    ConfirmationService
  ]
};
