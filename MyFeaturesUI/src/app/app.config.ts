import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';

import { HTTP_INTERCEPTORS, HttpClient, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { apiConfigFactory } from '../config/api-configuration.factory';
import { createHttpLoader } from '../config/app.translate.loader';
import { Configuration } from '../infrastructure';
import { routes } from './app.routes';
import { GlobalErrorInterceptor } from './interceptors/global.error.interceptor';

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
    ConfirmationService,

    { provide: HTTP_INTERCEPTORS, useClass: GlobalErrorInterceptor, multi: true },
    MessageService,

    importProvidersFrom(
      TranslateModule.forRoot(
        {
          loader:
          {
            provide: TranslateLoader,
            useFactory: createHttpLoader,
            deps: [HttpClient]
          },
          defaultLanguage: 'en_US'
        })
    )
  ]
};
