import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { switchMap } from 'rxjs';
import { User } from '../models/user';
import { UserService } from '../services/user.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, InputTextModule, ReactiveFormsModule, ButtonModule, TableModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  private formBuilder = inject(FormBuilder);
  private userService = inject(UserService);

  title = 'AzureCICDUI';
  form!: FormGroup;

  contentList$ = this.userService.getAll();

  ngOnInit(): void {
    this.form = this.formBuilder.group({
      content: ['', Validators.required]
    });
  }

  saveContent() {
    if (this.form.dirty) {
      const user: User = { ...this.form.value };

      this.contentList$ = this.userService.createPost(user).pipe(
        switchMap(() => this.userService.getAll())
      );
    }
  }
}
