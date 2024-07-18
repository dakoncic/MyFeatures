import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { MenuModule } from 'primeng/menu';
import { ToolbarModule } from 'primeng/toolbar';
import { LanguageService } from '../../services/language-service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    RouterModule,
    CommonModule,
    ToolbarModule,
    ButtonModule,
    DropdownModule,
    MenuModule,
    FormsModule
  ],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent implements OnInit {
  private languageService = inject(LanguageService);

  items!: MenuItem[];
  selectedLanguage!: MenuItem;

  ngOnInit() {
    this.items = [
      { label: 'HR', icon: 'pi pi-fw pi-flag', command: () => this.selectLanguage('hr_HR') },
      { label: 'EN', icon: 'pi pi-fw pi-flag', command: () => this.selectLanguage('en_US') }
    ];

    // Initialize language based on saved preference or default
    const activeLanguage = this.languageService.getActiveLanguage() ?? 'en_US';
    this.selectedLanguage = this.items.find(item => item.label === this.getLanguageLabel(activeLanguage))!;
  }

  selectLanguage(language: string) {
    this.selectedLanguage = this.items.find(item => item.label === this.getLanguageLabel(language))!;
    this.languageService.setLanguage(language);
  }

  getLanguageLabel(language: string): string {
    return language === 'hr_HR' ? 'HR' : 'EN';
  }

}
