import { inject, Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { LocalStorageService } from './local-storage.service';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly languageStorageKey = 'selectedLanguage';

  private translate = inject(TranslateService);
  private storage = inject(LocalStorageService);

  constructor() {
    this.initLanguage();
  }

  private initLanguage() {
    const savedLanguage = this.getActiveLanguage();
    if (savedLanguage) {
      this.translate.use(savedLanguage);
    } else {
      this.translate.setDefaultLang('en_US'); // Set default language
    }
  }

  setLanguage(lang: string) {
    this.storage.setItem(this.languageStorageKey, lang);
    this.translate.use(lang);

    window.location.reload();
  }

  getActiveLanguage() {
    return this.storage.getItem(this.languageStorageKey);
  }
}
